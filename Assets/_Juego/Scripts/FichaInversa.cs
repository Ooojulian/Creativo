using UnityEngine;
using System.Collections;

// Ficha que va de meta -> inicio (dirección inversa al MovimientoFicha normal)
public class FichaInversa : MonoBehaviour
{
    public GestorDeRuta ruta;
    public int indiceActual;
    public float velocidad = 150f;
    public GameManager gm;

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
}
