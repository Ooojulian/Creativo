using UnityEngine;
using System.Collections;

// Ficha que va de meta -> inicio (dirección inversa al MovimientoFicha normal)
public class FichaInversa : MonoBehaviour
{
    public GestorDeRuta ruta;
    public int indiceActual;
    public float velocidad = 150f;
    public GameManager gm;

    public MovimientoFicha fichaPrincipal;

    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;

    void Awake() { camaraDirectora = FindAnyObjectByType<CamaraDirectora>(); }
    void Start() { Inicializar(); }
    void OnEnable() { Inicializar(); }

    public void Inicializar()
    {
        if (ruta != null && ruta.casillas.Count > 0)
        {
            indiceActual = ruta.casillas.Count - 1;
            transform.position = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;
        }
    }

    public void Avanzar(int pasos)
    {
        if (enMovimiento) return;

        int casillasRestantes = indiceActual;

        if (pasos > casillasRestantes)
        {
            Debug.Log($"[FichaInversa] {name}: necesita {casillasRestantes} o menos, sacó {pasos}. Turno perdido.");
            bool soyAutoridadAvanza = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
            if (soyAutoridadAvanza && gm != null) gm.SiguienteTurno();
            return;
        }

        StartCoroutine(MoverHaciaInicio(pasos));
    }

    IEnumerator MoverHaciaInicio(int pasos)
    {
        enMovimiento = true;

        if (gm != null && gm.dado != null) gm.dado.gameObject.SetActive(false);
        if (camaraDirectora != null) camaraDirectora.SeguirJugador(transform);

        int metaFinal = indiceActual - pasos;
        if (metaFinal < 0) metaFinal = 0;

        while (indiceActual > metaFinal)
        {
            indiceActual--;

            Vector3 destino = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;

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
        Debug.Log($"[FichaInversa] {name} llegó a casilla {indiceActual}");

        // REVELAR -> AÑADIR A MANO (Usando el inventario de la ficha principal)
        yield return StartCoroutine(RevelarYAñadirCarta());

        // Detectar colisión → batalla PPS
        if (Photon.Pun.PhotonNetwork.IsMasterClient && BatallaPPS.Instance != null && gm != null)
        {
            if (gm.DetectarColision(null, this, out int idxDef, out bool esBDef, out int actorDef))
            {
                int idxAtk = -1;
                for (int k = 0; k < gm.todosLosJugadores.Count; k++)
                    if (gm.todosLosJugadores[k].fichaB == this) { idxAtk = k; break; }
                int actorAtk = idxAtk >= 0 && idxAtk < Photon.Pun.PhotonNetwork.PlayerList.Length
                    ? Photon.Pun.PhotonNetwork.PlayerList[idxAtk].ActorNumber : -1;
                BatallaPPS.Instance.IniciarBatalla(actorAtk, actorDef, idxAtk, true, idxDef, esBDef);
                yield break;
            }
        }

        bool llegóAlInicio = indiceActual <= 0;
        if (llegóAlInicio)
            Debug.Log($"[FichaInversa] {name} llegó al inicio.");
        else
        {
            // Solo host avanza turno en red. Clientes solo animaron.
            bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
            if (soyAutoridad && gm != null) gm.SiguienteTurno();
        }
    }

    IEnumerator RevelarYAñadirCarta()
    {
        if (ruta == null || ruta.casillas == null || ruta.casillas.Count == 0) yield break;
        if (indiceActual < 0 || indiceActual >= ruta.casillas.Count) yield break;

        Transform casilla = ruta.casillas[indiceActual];
        if (casilla == null) yield break;

        CartaEnCasilla comp = casilla.GetComponent<CartaEnCasilla>();
        if (comp == null) yield break;

        CardSO card = comp.ObtenerCarta();
        if (card == null) yield break;

        // 1) Mostrar revelación SOLO SI ES MI FICHA (Privado)
        bool esMia = true;
        var pv = fichaPrincipal != null ? fichaPrincipal.GetComponent<Photon.Pun.PhotonView>() : GetComponent<Photon.Pun.PhotonView>();
        if (pv != null) esMia = pv.IsMine;

        if (esMia && gm != null && gm.uiCartas != null)
            gm.uiCartas.MostrarRevelacion(card);

        yield return new WaitForSeconds(fichaPrincipal != null ? fichaPrincipal.tiempoRevelacion : 2f);

        // 2) Añadir a mano del jugador principal
        if (fichaPrincipal != null && fichaPrincipal.inventario != null)
        {
            fichaPrincipal.inventario.AddToHand(card);
            if (CardTriggerSystem.Instance != null)
                CardTriggerSystem.Instance.CheckCardDrawn(fichaPrincipal, card);
        }

        // 3) Limpiar UI
        if (gm != null && gm.uiCartas != null)
            yield return StartCoroutine(gm.uiCartas.FadeOutYLimpiar(fichaPrincipal != null ? fichaPrincipal.tiempoResultado : 1f));
    }
}
