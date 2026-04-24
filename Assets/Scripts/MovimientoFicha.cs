using UnityEngine;
using System.Collections;

public class MovimientoFicha : MonoBehaviour
{
    public GestorDeRuta ruta;
    public int indiceActual = 0;
    public float velocidad = 150f;
    public GameManager gm;

    [Header("Estado de cartas")]
    public bool escudoActivo = false;
    public bool pierdeSiguienteTurno = false;
    public bool dobleTiroPendiente = false;

    [Header("Cartas - UI timing")]
    public float tiempoRevelacion = 2f; // tiempo mostrando la carta antes de aplicar
    public float tiempoResultado = 1f;  // tiempo mostrando el efecto antes del fade out

    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;

    void Awake()
    {
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
    }

    public void Avanzar(int cantidadPasos)
    {
        if (enMovimiento) return;

        int casillasRestantes = ruta.casillas.Count - 1 - indiceActual;
        if (cantidadPasos > casillasRestantes)
        {
            Debug.Log($"[MovimientoFicha] {name}: necesita {casillasRestantes} o menos para avanzar, sacó {cantidadPasos}. Turno perdido.");
            if (gm != null) gm.SiguienteTurno();
            return;
        }

        StartCoroutine(MoverPorLasCasillas(cantidadPasos));
    }

    IEnumerator MoverPorLasCasillas(int pasos)
    {
        enMovimiento = true;

        // Ocultar dado y enfocar al jugador
        if (gm != null && gm.dado != null)
            gm.dado.gameObject.SetActive(false);

        if (camaraDirectora != null) camaraDirectora.SeguirJugador(transform);

        // Validar referencias
        if (ruta == null || ruta.casillas == null || ruta.casillas.Count == 0)
        {
            Debug.LogError($"[MovimientoFicha] {name}: ruta no asignada o sin casillas.");
            enMovimiento = false;
            if (gm != null) gm.SiguienteTurno();
            yield break;
        }

        int metaFinal = indiceActual + pasos;
        if (metaFinal >= ruta.casillas.Count)
            metaFinal = ruta.casillas.Count - 1;

        while (indiceActual < metaFinal)
        {
            indiceActual++;

            if (ruta.casillas[indiceActual] == null)
            {
                Debug.LogWarning($"[MovimientoFicha] casilla[{indiceActual}] es null, saltando.");
                continue;
            }

            Vector3 destino = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;

            while (Vector3.Distance(transform.position, destino) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, destino, velocidad * Time.deltaTime);
                yield return null;
            }

            transform.position = destino;
            yield return new WaitForSeconds(0.08f);
        }

        enMovimiento = false;
        Debug.Log($"[MovimientoFicha] {name} llegó a casilla {indiceActual}");

        // REVELAR -> ESPERAR -> APLICAR CARTA -> FADE OUT
        yield return StartCoroutine(RevelarYAplicarCarta());

        if (camaraDirectora != null) camaraDirectora.VolverAlTablero();

        bool llegóAMeta = indiceActual >= ruta.casillas.Count - 1;

        if (gm != null)
        {
            if (llegóAMeta)
            {
                gm.LlegarAMeta(this);
            }
            else
            {
                if (dobleTiroPendiente)
                {
                    dobleTiroPendiente = false;

                    if (gm.dado != null)
                        gm.dado.gameObject.SetActive(true);

                    Debug.Log($"[Cartas] {name} repite turno por DobleTiro.");
                }
                else
                {
                    gm.SiguienteTurno();
                }
            }
        }
    }

    IEnumerator RevelarYAplicarCarta()
    {
        if (ruta == null || ruta.casillas == null || ruta.casillas.Count == 0) yield break;
        if (indiceActual < 0 || indiceActual >= ruta.casillas.Count) yield break;

        Transform casilla = ruta.casillas[indiceActual];
        if (casilla == null) yield break;

        CartaEnCasilla comp = casilla.GetComponent<CartaEnCasilla>();
        if (comp == null)
        {
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        TipoCarta carta = comp.ObtenerCarta();

        if (carta == TipoCarta.Ninguna)
        {
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        // 1) Mostrar revelación con fade in
        if (gm != null && gm.uiCartas != null)
            gm.uiCartas.MostrarRevelacion(carta);

        yield return new WaitForSeconds(tiempoRevelacion);

        bool esDesventaja =
            carta == TipoCarta.Retroceso ||
            carta == TipoCarta.PierdeTurno ||
            carta == TipoCarta.Intercambio;

        // 2) Escudo bloquea desventajas
        if (esDesventaja && escudoActivo)
        {
            escudoActivo = false;

            if (gm != null && gm.uiCartas != null)
                gm.uiCartas.MostrarResultado(carta, true);

            Debug.Log($"[Cartas] {name} bloqueó {carta} con Escudo.");

            // Esperar tiempoResultado y luego hacer fade out y limpiar
            if (gm != null && gm.uiCartas != null)
                yield return StartCoroutine(gm.uiCartas.FadeOutYLimpiar(tiempoResultado));
            else
                yield return new WaitForSeconds(tiempoResultado);

            yield break;
        }

        // 3) Mostrar resultado
        if (gm != null && gm.uiCartas != null)
            gm.uiCartas.MostrarResultado(carta, false);

        // 4) Aplicar el efecto
        switch (carta)
        {
            case TipoCarta.AvanceRapido:
            {
                int nuevo = Mathf.Min(ruta.casillas.Count - 1, indiceActual + 2);
                indiceActual = nuevo;
                transform.position = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;
                Debug.Log($"[Cartas] {name} AvanceRapido -> casilla {indiceActual}");
                break;
            }

            case TipoCarta.Escudo:
                escudoActivo = true;
                Debug.Log($"[Cartas] {name} obtuvo Escudo.");
                break;

            case TipoCarta.DobleTiro:
                dobleTiroPendiente = true;
                Debug.Log($"[Cartas] {name} obtuvo DobleTiro.");
                break;

            case TipoCarta.Retroceso:
            {
                int nuevo = Mathf.Max(0, indiceActual - 2);
                indiceActual = nuevo;
                transform.position = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;
                Debug.Log($"[Cartas] {name} Retroceso -> casilla {indiceActual}");
                break;
            }

            case TipoCarta.PierdeTurno:
                pierdeSiguienteTurno = true;
                Debug.Log($"[Cartas] {name} PierdeTurno (siguiente turno).");
                break;

            case TipoCarta.Intercambio:
                if (gm != null)
                {
                    gm.IntercambiarConOtroJugador(this);
                    Debug.Log($"[Cartas] {name} Intercambio.");
                }
                break;
        }

        // 5) Esperar tiempoResultado, luego fade out y limpiar la UI automáticamente
        if (gm != null && gm.uiCartas != null)
            yield return StartCoroutine(gm.uiCartas.FadeOutYLimpiar(tiempoResultado));
        else
            yield return new WaitForSeconds(tiempoResultado);
    }
}