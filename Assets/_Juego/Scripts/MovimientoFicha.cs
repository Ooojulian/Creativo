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

    [Header("Nuevo Sistema de Cartas")]
    public PlayerInventory inventario;
    public int cartasAlEmpezarTurno = 0;

    void Awake()
    {
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
        if (inventario == null) inventario = GetComponent<PlayerInventory>();
    }

    public void Avanzar(int cantidadPasos)
    {
        if (enMovimiento) return;

        int casillasRestantes = ruta.casillas.Count - 1 - indiceActual;
        
        // CARTA SPRINT (Reserva): Verificar cercanía a meta antes de mover
        if (CardTriggerSystem.Instance != null)
            CardTriggerSystem.Instance.CheckNearGoal(this);

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
            int indiceAnterior = indiceActual;
            indiceActual++;

            if (ruta.casillas[indiceActual] == null)
            {
                Debug.LogWarning($"[MovimientoFicha] casilla[{indiceActual}] es null, saltando.");
                continue;
            }

            // CHECK: Overtake (Pisotón)
            if (CardTriggerSystem.Instance != null)
            {
                foreach (var otro in gm.todosLosJugadores)
                {
                    if (otro != this && otro.indiceActual == indiceActual)
                    {
                        // Si yo paso a alguien que estaba en esta casilla
                        CardTriggerSystem.Instance.CheckOvertake(otro, this);
                    }
                }
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

        // REVELAR -> AÑADIR A MANO
        yield return StartCoroutine(RevelarYAñadirCarta());

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

    IEnumerator RevelarYAñadirCarta()
    {
        if (ruta == null || ruta.casillas == null || ruta.casillas.Count == 0) yield break;
        if (indiceActual <= 0 || indiceActual >= ruta.casillas.Count) yield break;

        Transform casilla = ruta.casillas[indiceActual];
        if (casilla == null) yield break;

        CartaEnCasilla comp = casilla.GetComponent<CartaEnCasilla>();
        if (comp == null)
        {
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        CardSO card = comp.ObtenerCarta();

        if (card == null)
        {
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        // 1) Mostrar revelación
        if (gm != null && gm.uiCartas != null)
            gm.uiCartas.MostrarRevelacion(card);

        yield return new WaitForSeconds(tiempoRevelacion);

        // 2) Añadir a mano
        if (inventario != null)
        {
            inventario.AddToHand(card);
            if (CardTriggerSystem.Instance != null)
                CardTriggerSystem.Instance.CheckCardDrawn(this, card);
        }

        // 3) Limpiar UI
        if (gm != null && gm.uiCartas != null)
            yield return StartCoroutine(gm.uiCartas.FadeOutYLimpiar(tiempoResultado));
    }
}