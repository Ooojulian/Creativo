using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Solo maneja el MOVIMIENTO. La lógica de cartas está en EfectoCarta y GestorTurnos.
/// Mucho más simple y desacoplado.
/// </summary>
public class MovimientoFicha : MonoBehaviour
{
    [Header("Referencias")]
    private GameManager gameManager;
    [SerializeField] private float velocidad = 150f;
    
    // Índice actual en la ruta
    [HideInInspector] public int indiceActual = 0;
    public FichaInversa fichaB; // Referencia a la ficha inversa
    public bool moverFichaB = false; // Variable que indica si mover ficha B
    private SistemaEnergia sistemaEnergia;
    private InventarioCartas inventarioCartas;

    public InventarioCartas ObtenerInventarioCartas() => inventarioCartas;
    private EstadosJugador estadosJugador;
    
    // Control de movimiento
    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;
    
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
        
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
    }
    
    void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
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
            // La ficha B maneja su propio movimiento
        }
        else
        {
            Debug.Log($"[MovimientoFicha] Elegida Ficha A");
        }
    }
    
    public void MoverFichaBPasos(int pasos)
    {
        if (fichaB != null)
            fichaB.Avanzar(pasos);
    }

    public List<Transform> ruta => GameObject.FindGameObjectWithTag("GameManager")?.GetComponent<GameManager>()?.casillas;

    public InventarioCartas inventario => ObtenerInventario();
    
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
        
        if (camaraDirectora != null)
            camaraDirectora.SeguirJugador(transform);
        
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
        Debug.Log($"[MovimientoFicha] {name} llegó a casilla {indiceActual}");
        
        if (camaraDirectora != null)
            camaraDirectora.VolverAlTablero();
        
        // Notificar a GestorTurnos que terminó el movimiento
        GestorTurnos gestorTurnos = FindAnyObjectByType<GestorTurnos>();
        if (gestorTurnos != null)
            gestorTurnos.OnMovimientoCompletado(this);
        
        // Verificar si llegó a la meta
        if (gameManager != null && indiceActual >= gameManager.casillas.Count - 1)
            gameManager.LlegarAMeta(this);
        
        OnMovimientoCompletado?.Invoke(indiceActual);
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
}