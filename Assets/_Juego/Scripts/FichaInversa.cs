using UnityEngine;
using System.Collections;

// Ficha que va de meta -> inicio (dirección inversa al MovimientoFicha normal)
public class FichaInversa : MonoBehaviour
{
    private GameManager gameManager;
    public int indiceActual;
    public float velocidad = 150f;

    public MovimientoFicha fichaPrincipal;

    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;

    void Awake()
    {
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
        gameManager = FindAnyObjectByType<GameManager>();
    }

    void Start()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        Inicializar();
    }

    void OnEnable()
    {
        if (gameManager != null) Inicializar();
    }

    public void Inicializar()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null && gameManager.casillas != null && gameManager.casillas.Count > 0)
        {
            indiceActual = gameManager.casillas.Count - 1;
            transform.position = gameManager.casillas[indiceActual].position + Vector3.up * 0.5f;
        }
    }

    public void Avanzar(int pasos)
    {
        if (enMovimiento) return;

        int casillasRestantes = indiceActual;

        if (pasos > casillasRestantes)
        {
            bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
            if (soyAutoridad && gameManager != null) gameManager.SiguienteTurno();
            return;
        }

        StartCoroutine(MoverHaciaInicio(pasos));
    }

    // Llamado por CardManager cuando una carta de retroceso afecta la FichaB
    public void Retroceder(int pasos)
    {
        if (enMovimiento) return;
        var casillas = gameManager?.casillas;
        if (casillas == null || casillas.Count == 0) return;

        int nuevoIndice = Mathf.Min(casillas.Count - 1, indiceActual + pasos);
        if (nuevoIndice == indiceActual) return;

        StartCoroutine(MoverHaciaFinal(pasos));
    }

    IEnumerator MoverHaciaFinal(int pasos)
    {
        enMovimiento = true;

        if (camaraDirectora != null) camaraDirectora.SeguirJugador(transform);

        var casillas = gameManager?.casillas;
        if (casillas == null) { enMovimiento = false; yield break; }

        int metaFinal = Mathf.Min(casillas.Count - 1, indiceActual + pasos);

        while (indiceActual < metaFinal)
        {
            indiceActual++;
            Vector3 destino = casillas[indiceActual].position + Vector3.up * 0.5f;
            while (Vector3.Distance(transform.position, destino) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
                yield return null;
            }
            transform.position = destino;
            yield return new WaitForSeconds(0.08f);
        }

        enMovimiento = false;
        if (camaraDirectora != null) camaraDirectora.VolverAlTablero();
    }

    IEnumerator MoverHaciaInicio(int pasos)
    {
        enMovimiento = true;

        if (gameManager != null && gameManager.dado != null) gameManager.dado.gameObject.SetActive(false);
        if (camaraDirectora != null) camaraDirectora.SeguirJugador(transform);

        var casillas = gameManager?.casillas;
        if (casillas == null) { enMovimiento = false; yield break; }

        int metaFinal = Mathf.Max(0, indiceActual - pasos);

        while (indiceActual > metaFinal)
        {
            indiceActual--;
            Vector3 destino = casillas[indiceActual].position + Vector3.up * 0.5f;
            while (Vector3.Distance(transform.position, destino) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
                yield return null;
            }
            transform.position = destino;
            yield return new WaitForSeconds(0.08f);
        }

        enMovimiento = false;
        if (camaraDirectora != null) camaraDirectora.VolverAlTablero();

        yield return StartCoroutine(RevelarYAñadirCarta());

        // Detectar colisión → batalla PPS
        if (Photon.Pun.PhotonNetwork.IsMasterClient && BatallaPPS.Instance != null && gameManager != null)
        {
            if (gameManager.DetectarColision(null, this, out int idxDef, out bool esBDef, out int actorDef))
            {
                int idxAtk = -1;
                for (int k = 0; k < gameManager.todosLosJugadores.Count; k++)
                    if (gameManager.todosLosJugadores[k].fichaB == this) { idxAtk = k; break; }
                int actorAtk = idxAtk >= 0 && idxAtk < Photon.Pun.PhotonNetwork.PlayerList.Length
                    ? Photon.Pun.PhotonNetwork.PlayerList[idxAtk].ActorNumber : -1;
                BatallaPPS.Instance.IniciarBatalla(actorAtk, actorDef, idxAtk, true, idxDef, esBDef);
                yield break;
            }
        }

        bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
        if (soyAutoridad && gameManager != null) gameManager.SiguienteTurno();
    }

    IEnumerator RevelarYAñadirCarta()
    {
        var casillas = gameManager?.casillas;
        if (casillas == null || casillas.Count == 0) yield break;
        if (indiceActual < 0 || indiceActual >= casillas.Count) yield break;

        Transform casilla = casillas[indiceActual];
        if (casilla == null) yield break;

        CartaEnCasilla comp = casilla.GetComponent<CartaEnCasilla>();
        if (comp == null) yield break;

        CardSO card = comp.ObtenerCarta();
        if (card == null) yield break;

        // GameSync.EsMiTurno es la fuente correcta (fichas de escena, no instanciadas por red)
        bool esMia = true;
        if (Photon.Pun.PhotonNetwork.InRoom)
            esMia = GameSync.Instance != null && GameSync.Instance.EsMiTurno;

        if (esMia && gameManager != null && gameManager.uiCartas != null)
            gameManager.uiCartas.MostrarRevelacion(card);

        yield return new WaitForSeconds(fichaPrincipal != null ? fichaPrincipal.tiempoRevelacion : 2f);

        if (fichaPrincipal != null && fichaPrincipal.inventario != null)
        {
            fichaPrincipal.inventario.AddToHand(card);
            if (CardTriggerSystem.Instance != null)
                CardTriggerSystem.Instance.CheckCardDrawn(fichaPrincipal, card);
        }

        if (esMia && CardPlayUI.Instance != null)
        {
            CardPlayUI.Instance.Mostrar(card, fichaPrincipal);
            while (CardPlayUI.Instance.panel.activeSelf)
                yield return null;
        }

        if (esMia && gameManager != null && gameManager.uiCartas != null)
            yield return StartCoroutine(gameManager.uiCartas.FadeOutYLimpiar(fichaPrincipal != null ? fichaPrincipal.tiempoResultado : 1f));
    }
}
