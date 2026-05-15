using UnityEngine;
using System.Collections;

/// <summary>
/// Aplica los efectos de las cartas a los jugadores.
/// Desacoplado de la lógica de selección y UI.
/// </summary>
public class EfectoCarta : MonoBehaviour
{
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }
    
    // ────────────────────────────────────────
    // API PÚBLICA
    // ────────────────────────────────────────
    
    /// <summary>
    /// Aplica una carta a un jugador objetivo.
    /// </summary>
    public void AplicarCartaA(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        if (carta == null || objetivo == null)
        {
            Debug.LogWarning("[EfectoCarta] Carta u objetivo null");
            return;
        }
        
        Debug.Log($"[EfectoCarta] Aplicando {carta.nombreCarta} a {objetivo.name}");
        
        switch (carta.tipoEfecto)
        {
            case CartaDefinicion.TipoEfecto.Movimiento:
                AplicarMovimiento(carta, objetivo);
                break;
                
            case CartaDefinicion.TipoEfecto.Turno:
                AplicarTurno(carta, objetivo);
                break;
                
            case CartaDefinicion.TipoEfecto.Defensa:
                AplicarDefensa(carta, objetivo);
                break;
                
            case CartaDefinicion.TipoEfecto.Estado:
                AplicarEstado(carta, objetivo);
                break;
                
            case CartaDefinicion.TipoEfecto.Energía:
                AplicarEnergia(carta, objetivo);
                break;
                
            case CartaDefinicion.TipoEfecto.Carta:
                AplicarEfectoCarta(carta, objetivo);
                break;
                
            case CartaDefinicion.TipoEfecto.Intercambio:
                AplicarIntercambio(carta, objetivo);
                break;
                
            default:
                Debug.LogWarning($"[EfectoCarta] Tipo de efecto desconocido: {carta.tipoEfecto}");
                break;
        }
    }
    
    // ────────────────────────────────────────
    // EFECTOS ESPECÍFICOS
    // ────────────────────────────────────────
    
    private void AplicarMovimiento(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        if (gameManager == null || gameManager.casillas == null) return;
        
        // valor1 = +/-X casillas
        int casillasNuevas = Mathf.Clamp(
            objetivo.indiceActual + carta.valor1,
            0,
            gameManager.casillas.Count - 1
        );
        
        objetivo.indiceActual = casillasNuevas;
        objetivo.transform.position = gameManager.casillas[casillasNuevas].position + Vector3.up * 0.5f;
        
        string direccion = carta.valor1 > 0 ? "avanza" : "retrocede";
        Debug.Log($"[EfectoCarta] {objetivo.name} {direccion} a casilla {casillasNuevas}");
    }
    
    private void AplicarTurno(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        // valor1 = turnos a perder, o -1 para doble turno
        if (carta.valor1 == -1)
        {
            objetivo.ObtenerEstados().AplicarEstado(EstadosJugador.DOBLE_TURNO, 1);
            Debug.Log($"[EfectoCarta] {objetivo.name} obtiene Doble Turno");
        }
        else if (carta.valor1 > 0)
        {
            objetivo.ObtenerEstados().AplicarEstado(EstadosJugador.SILENCIO, carta.valor1);
            Debug.Log($"[EfectoCarta] {objetivo.name} pierde {carta.valor1} turno(s)");
        }
    }
    
    private void AplicarDefensa(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        // valor1 = duración del escudo
        int duracion = carta.valor1 > 0 ? carta.valor1 : 1;
        objetivo.ObtenerEstados().AplicarEstado(EstadosJugador.ESCUDO, duracion);
        Debug.Log($"[EfectoCarta] {objetivo.name} obtiene Escudo por {duracion} turno(s)");
    }
    
    private void AplicarEstado(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        // Para maldición, silencio, congelado, etc.
        string nombreEstado = carta.nombreCarta;
        int duracion = (int)carta.duracion > 0 ? (int)carta.duracion : 1;
        
        objetivo.ObtenerEstados().AplicarEstado(nombreEstado, duracion);
        Debug.Log($"[EfectoCarta] {objetivo.name} obtiene {nombreEstado} por {duracion} turno(s)");
    }
    
    private void AplicarEnergia(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        // valor1 = +/-X energía
        SistemaEnergia energia = objetivo.ObtenerEnergia();
        
        if (carta.valor1 > 0)
            energia.GanarEnergia(carta.valor1);
        else if (carta.valor1 < 0)
            energia.GastarEnergia(Mathf.Abs(carta.valor1));
            
        Debug.Log($"[EfectoCarta] {objetivo.name} energía cambia por {carta.valor1}");
    }
    
    private void AplicarEfectoCarta(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        InventarioCartas inv = objetivo.ObtenerInventario();
        
        // valor1 = cantidad de cartas a robar/descartar
        if (carta.nombreCarta.Contains("Robo"))
        {
            // Robar cartas
            int cantidad = carta.valor1 > 0 ? carta.valor1 : 1;
            for (int i = 0; i < cantidad; i++)
            {
                CartaDefinicion nuevaCarta = gameManager.ObtenerSistemaCartas()
                    .ObtenerCartaAleatoria();
                inv.AgregarCarta(nuevaCarta);
            }
            Debug.Log($"[EfectoCarta] {objetivo.name} roba {cantidad} carta(s)");
        }
        else if (carta.nombreCarta.Contains("Ruptura"))
        {
            // Descartar cartas
            int cantidad = carta.valor1 > 0 ? carta.valor1 : 2;
            var descartadas = inv.DescartarCartasAlAzar(cantidad);
            Debug.Log($"[EfectoCarta] {objetivo.name} descarta {descartadas.Count} carta(s)");
        }
    }
    
    private void AplicarIntercambio(CartaDefinicion carta, MovimientoFicha objetivo1, MovimientoFicha objetivo2 = null)
    {
        if (gameManager == null || gameManager.casillas == null) return;
        
        if (objetivo2 == null)
        {
            // Intercambiar con jugador aleatorio
            var candidatos = gameManager.ObtenerJugadoresActivos();
            candidatos.RemoveAll(j => j == objetivo1);
            
            if (candidatos.Count == 0) return;
            
            objetivo2 = candidatos[Random.Range(0, candidatos.Count)];
        }
        
        // Intercambiar posiciones
        int temp = objetivo1.indiceActual;
        objetivo1.indiceActual = objetivo2.indiceActual;
        objetivo2.indiceActual = temp;
        
        objetivo1.transform.position = gameManager.casillas[objetivo1.indiceActual].position + Vector3.up * 0.5f;
        objetivo2.transform.position = gameManager.casillas[objetivo2.indiceActual].position + Vector3.up * 0.5f;
        
        Debug.Log($"[EfectoCarta] Intercambio: {objetivo1.name} <-> {objetivo2.name}");
    }
}
