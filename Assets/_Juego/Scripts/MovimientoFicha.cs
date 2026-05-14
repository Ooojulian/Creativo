using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Maneja el MOVIMIENTO de fichas. Integrado con cartas, animaciones, Photon y batallas.
/// </summary>
public class MovimientoFicha : MonoBehaviour
{
    [Header("Referencias")]
    private GameManager gameManager;
    [SerializeField] private float velocidad = 150f;
    
    // Índice actual en la ruta
    [HideInInspector] public int indiceActual = 0;
    public FichaInversa fichaB; // Referencia a la ficha inversa
    public bool moverFichaB = false; // true = mover FichaB, false = mover FichaA (esta)
    
    private SistemaEnergia sistemaEnergia;
    private InventarioCartas inventarioCartas;
    private EstadosJugador estadosJugador;
    
    // Animaciones y UI
    private PersonajeAnimador _animador;
    public PlayerInventory inventario;
    public int cartasAlEmpezarTurno = 0;
    
    // Control de movimiento
    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;
    private GameManager gm;
    private bool dobleTiroPendiente = false;
    
    // Eventos
    public event Action<int> OnMovimientoCompletado; // (índiceActual)

    void Awake()
    {
        // Crear/obtener componentes del jugador
        if (sistemaEnergia == null)
            sistemaEnergia = GetComponent<SistemaEnergia>() ?? gameObject.AddComponent<SistemaEnergia>();
            
        if (inventarioCartas == null)
            inventarioCartas = GetComponent<InventarioCartas>() ?? gameObject.AddComponent<InventarioCartas>();
            
        if (estadosJugador == null)
            estadosJugador = GetComponent<EstadosJugador>() ?? gameObject.AddComponent<EstadosJugador>();
        
        if (inventario == null) 
            inventario = GetComponent<PlayerInventory>();
            
        _animador = GetComponentInChildren<PersonajeAnimador>();
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
        gm = FindAnyObjectByType<GameManager>();
    }
    
    void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
        if (gm == null)
            gm = gameManager;
    }
    
    // ────────────────────────────────────────
    // ACCESO A COMPONENTES
    // ────────────────────────────────────────
    
    public SistemaEnergia ObtenerEnergia() => sistemaEnergia;
    public InventarioCartas ObtenerInventario() => inventarioCartas;
    public EstadosJugador ObtenerEstados() => estadosJugador;
    
    public void ElegirFicha(bool esB)
    {
        if (esB && fichaB != null)
        {
            Debug.Log($"[MovimientoFicha] Elegida Ficha B");
            moverFichaB = true;
        }
        else
        {
            Debug.Log($"[MovimientoFicha] Elegida Ficha A");
            moverFichaB = false;
        }
    }
    
    public void MoverFichaBPasos(int pasos)
    {
        if (fichaB != null)
            fichaB.Avanzar(pasos);
    }

    public List<Transform> ruta => GameObject.FindGameObjectWithTag("GameManager")?.GetComponent<GameManager>()?.casillas;
    
    // ────────────────────────────────────────
    // MOVIMIENTO
    // ────────────────────────────────────────
    
    public void Avanzar(int cantidadPasos)
    {
        if (enMovimiento) return;
        
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
        
        if (gameManager == null || gameManager.casillas == null || gameManager.casillas.Count == 0)
        {
            Debug.LogError("[MovimientoFicha] GameManager o casillas no están disponibles");
            OnMovimientoCompletado?.Invoke(indiceActual);
            return;
        }
        
        // Validar que no se pase la meta
        int casillasRestantes = gameManager.casillas.Count - 1 - indiceActual;

        if (cantidadPasos > casillasRestantes)
        {
            Debug.Log($"[MovimientoFicha] {name}: no puede avanzar {cantidadPasos}, solo hay {casillasRestantes}");
            OnMovimientoCompletado?.Invoke(indiceActual);
            return;
        }
        
        StartCoroutine(MoverPorLasCasillas(cantidadPasos));
    }
    
    IEnumerator MoverPorLasCasillas(int pasos)
    {
        enMovimiento = true;
        _animador?.SetMoviendo(true);

        // Ocultar dado y enfocar al jugador
        if (gm != null && gm.dado != null)
            gm.dado.gameObject.SetActive(false);

        if (camaraDirectora != null) 
            camaraDirectora.SeguirJugador(transform);

        // Validar referencias
        if (gameManager == null || gameManager.casillas == null || gameManager.casillas.Count == 0)
        {
            Debug.LogError($"[MovimientoFicha] {name}: ruta no asignada o sin casillas.");
            enMovimiento = false;
            if (gm != null) gm.SiguienteTurno();
            yield break;
        }

        int metaFinal = Mathf.Min(indiceActual + pasos, gameManager.casillas.Count - 1);
        
        while (indiceActual < metaFinal)
        {
            indiceActual++;
            
            if (gameManager.casillas[indiceActual] == null)
            {
                Debug.LogWarning($"[MovimientoFicha] casilla[{indiceActual}] es null");
                continue;
            }
            
            Vector3 destino = gameManager.casillas[indiceActual].position + Vector3.up * 0.5f;
            
            while (Vector3.Distance(transform.position, destino) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destino,
                    velocidad * Time.deltaTime);
                yield return null;
            }
            
            transform.position = destino;
            yield return new WaitForSeconds(0.08f);
        }
        
        enMovimiento = false;
        _animador?.SetMoviendo(false);
        Debug.Log($"[MovimientoFicha] {name} llegó a casilla {indiceActual}");

        // REVELAR -> AÑADIR A MANO
        yield return StartCoroutine(RevelarYAñadirCarta());

        if (camaraDirectora != null) 
            camaraDirectora.VolverAlTablero();

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

        bool llegóAMeta = indiceActual >= gameManager.casillas.Count - 1;

        // En red: solo host decide avance de turno y meta
        bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
        if (!soyAutoridad)
        {
            Debug.Log($"[MovimientoFicha] {name} cliente terminó animación. Espera turno de host.");
            yield break;
        }

        if (gm != null)
        {
            if (llegóAMeta)
            {
                _animador?.SetVictoria();
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

        // Notificar evento
        OnMovimientoCompletado?.Invoke(indiceActual);
    }

    IEnumerator RevelarYAñadirCarta()
    {
        // Placeholder para revelar y añadir cartas
        // Implementar según tu sistema de cartas
        yield return null;
    }
    
    // ────────────────────────────────────────
    // UTILIDADES
    // ────────────────────────────────────────
    
    public Vector3 ObtenerPosicionCasilla(int indice)
    {
        if (gameManager == null || gameManager.casillas == null) return transform.position;
        if (indice < 0 || indice >= gameManager.casillas.Count) return transform.position;
        return gameManager.casillas[indice].position + Vector3.up * 0.5f;
    }

    public void SetDobleTiroPendiente(bool valor)
    {
        dobleTiroPendiente = valor;
    }
}
