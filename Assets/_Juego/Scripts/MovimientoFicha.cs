using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Solo maneja el MOVIMIENTO. La lógica de cartas está en EfectoCarta y GestorTurnos.
/// Mucho más simple y desacoplado.
/// </summary>
public class MovimientoFicha : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GestorDeRuta ruta;
    [SerializeField] private float velocidad = 150f;
    
    // Índice actual en la ruta
    [HideInInspector] public int indiceActual = 0;
    
    // Componentes del sistema de juego (se cargan en Awake)
    private SistemaEnergia sistemaEnergia;
    private InventarioCartas inventarioCartas;
    private EstadosJugador estadosJugador;
    
    // Control de movimiento
    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;
<<<<<<< Updated upstream:Assets/_Juego/Scripts/MovimientoFicha.cs

    [Header("Nuevo Sistema de Cartas")]
    public PlayerInventory inventario;
    public int cartasAlEmpezarTurno = 0;

    [Header("Segunda Ficha")]
    public FichaInversa fichaB;
    public bool moverFichaB = false; // true = mover FichaB, false = mover FichaA (esta)

=======
    
    // Eventos
    public event Action<int> OnMovimientoCompletado; // (índiceActual)
    
>>>>>>> Stashed changes:Assets/Scripts/MovimientoFicha.cs
    void Awake()
    {
        // Crear/obtener componentes del jugador
        if (sistemaEnergia == null)
            sistemaEnergia = GetComponent<SistemaEnergia>() ?? gameObject.AddComponent<SistemaEnergia>();
            
        if (inventarioCartas == null)
            inventarioCartas = GetComponent<InventarioCartas>() ?? gameObject.AddComponent<InventarioCartas>();
            
        if (estadosJugador == null)
            estadosJugador = GetComponent<EstadosJugador>() ?? gameObject.AddComponent<EstadosJugador>();
        
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
        if (inventario == null) inventario = GetComponent<PlayerInventory>();
    }
    
    void Start()
    {
        if (ruta == null)
            ruta = FindAnyObjectByType<GestorDeRuta>();
    }
    
    // ────────────────────────────────────────
    // ACCESO A COMPONENTES
    // ────────────────────────────────────────
    
    public SistemaEnergia ObtenerEnergia() => sistemaEnergia;
    public InventarioCartas ObtenerInventario() => inventarioCartas;
    public EstadosJugador ObtenerEstados() => estadosJugador;
    
    // ────────────────────────────────────────
    // MOVIMIENTO
    // ────────────────────────────────────────
    
    public void Avanzar(int cantidadPasos)
    {
        if (enMovimiento) return;
<<<<<<< Updated upstream:Assets/_Juego/Scripts/MovimientoFicha.cs

        // Si eligió mover la ficha B, delegar
        if (moverFichaB && fichaB != null)
        {
            fichaB.Avanzar(cantidadPasos);
            return;
        }

=======
        
        // Validar que no se pase la meta
>>>>>>> Stashed changes:Assets/Scripts/MovimientoFicha.cs
        int casillasRestantes = ruta.casillas.Count - 1 - indiceActual;

        if (CardTriggerSystem.Instance != null)
            CardTriggerSystem.Instance.CheckNearGoal(this);

        if (cantidadPasos > casillasRestantes)
        {
<<<<<<< Updated upstream:Assets/_Juego/Scripts/MovimientoFicha.cs
            Debug.Log($"[MovimientoFicha] {name}: necesita {casillasRestantes} o menos para avanzar, sacó {cantidadPasos}. Turno perdido.");
            if (Photon.Pun.PhotonNetwork.IsMasterClient && gm != null) gm.SiguienteTurno();
            else if (GameSync.Instance == null && gm != null) gm.SiguienteTurno();
=======
            Debug.Log($"[MovimientoFicha] {name}: no puede avanzar {cantidadPasos}, solo hay {casillasRestantes}");
            OnMovimientoCompletado?.Invoke(indiceActual);
>>>>>>> Stashed changes:Assets/Scripts/MovimientoFicha.cs
            return;
        }
        
        StartCoroutine(MoverPorLasCasillas(cantidadPasos));
    }
<<<<<<< Updated upstream:Assets/_Juego/Scripts/MovimientoFicha.cs

    // Llamado por la UI antes de tirar el dado
    public void ElegirFicha(bool usarFichaB)
    {
        moverFichaB = usarFichaB;
        Debug.Log($"[{name}] Ficha elegida: {(usarFichaB ? "B (inversa)" : "A (normal)")}");
    }

=======
    
>>>>>>> Stashed changes:Assets/Scripts/MovimientoFicha.cs
    IEnumerator MoverPorLasCasillas(int pasos)
{
    enMovimiento = true;
    
    if (camaraDirectora != null)
        camaraDirectora.SeguirJugador(transform);
    
    int metaFinal = Mathf.Min(indiceActual + pasos, ruta.casillas.Count - 1);
    
    while (indiceActual < metaFinal)
    {
        indiceActual++;
        
        if (ruta.casillas[indiceActual] == null)
        {
            Debug.LogWarning($"[MovimientoFicha] casilla[{indiceActual}] es null");
            continue;
        }
        
        Vector3 destino = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;
        
        while (Vector3.Distance(transform.position, destino) > 0.05f)
        {
<<<<<<< Updated upstream:Assets/_Juego/Scripts/MovimientoFicha.cs
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

        // Detectar colisión con ficha enemiga → batalla PPS
        if (Photon.Pun.PhotonNetwork.IsMasterClient && BatallaPPS.Instance != null && gm != null)
        {
            if (gm.DetectarColision(this, null, out int idxDef, out bool esBDef, out int actorDef))
            {
                int idxAtk = gm.todosLosJugadores.IndexOf(this);
                int actorAtk = idxAtk < Photon.Pun.PhotonNetwork.PlayerList.Length
                    ? Photon.Pun.PhotonNetwork.PlayerList[idxAtk].ActorNumber : -1;
                BatallaPPS.Instance.IniciarBatalla(actorAtk, actorDef, idxAtk, false, idxDef, esBDef);
                yield break;
            }
        }

        bool llegóAMeta = indiceActual >= ruta.casillas.Count - 1;

        // En red: solo host decide avance de turno y meta. Otros clientes solo animaron.
        bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
        if (!soyAutoridad)
        {
            Debug.Log($"[MovimientoFicha] {name} cliente termino animacion. Espera turno de host.");
            yield break;
        }

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
=======
            transform.position = Vector3.MoveTowards(
                transform.position,
                destino,
                velocidad * Time.deltaTime);
            yield return null;
>>>>>>> Stashed changes:Assets/Scripts/MovimientoFicha.cs
        }
        
        transform.position = destino;
        yield return new WaitForSeconds(0.08f);
    }
<<<<<<< Updated upstream:Assets/_Juego/Scripts/MovimientoFicha.cs

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
=======
    
    enMovimiento = false;
    
    // ← AGREGAR:
    if (camaraDirectora != null)
        camaraDirectora.VolverAlTablero();
    
    // Notificar a GestorTurnos que terminó el movimiento
    GestorTurnos gestorTurnos = FindAnyObjectByType<GestorTurnos>();
    if (gestorTurnos != null)
        gestorTurnos.OnMovimientoCompletado(this);
    
    // Verificar si llegó a la meta
    GameManager gm = FindAnyObjectByType<GameManager>();
    if (indiceActual >= ruta.casillas.Count - 1 && gm != null)
        gm.LlegarAMeta(this);
    
    OnMovimientoCompletado?.Invoke(indiceActual);
    }
    
    // ────────────────────────────────────────
    // UTILIDADES
    // ────────────────────────────────────────
    
    public Vector3 ObtenerPosicionCasilla(int indice)
    {
        if (indice < 0 || indice >= ruta.casillas.Count) return transform.position;
        return ruta.casillas[indice].position + Vector3.up * 0.5f;
>>>>>>> Stashed changes:Assets/Scripts/MovimientoFicha.cs
    }
}