using UnityEngine;

/// <summary>
/// Aplica los efectos de las cartas (movimiento, defensa, turno, energía, etc)
/// </summary>
public class ControladorEfectosCartas : MonoBehaviour
{
    public static ControladorEfectosCartas instancia;
    
    void Awake()
    {
        if (instancia == null)
            instancia = this;
        else
            Destroy(gameObject);
    }
    
    /// <summary>
    /// Aplica el efecto de una carta a un jugador objetivo
    /// </summary>
    public void AplicarEfectoCarta(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        if (carta == null || objetivo == null)
        {
            Debug.LogWarning("Carta u objetivo nulo");
            return;
        }
        
        Debug.Log($"Aplicando {carta.nombreCarta} a {objetivo.name}");
        
        switch (carta.tipoEfecto)
        {
            case CartaDefinicion.TipoEfecto.Movimiento:
                AplicarMovimiento(objetivo, carta.valor1);
                break;
                
            case CartaDefinicion.TipoEfecto.Defensa:
                AplicarDefensa(objetivo, carta.valor1);
                break;
                
            case CartaDefinicion.TipoEfecto.Turno:
                AplicarTurno(objetivo, carta.valor1);
                break;
                
            case CartaDefinicion.TipoEfecto.Energía:
                AplicarEnergia(objetivo, carta.valor1);
                break;
                
            case CartaDefinicion.TipoEfecto.Carta:
                AplicarCartaEfecto(objetivo, carta.valor1);
                break;
                
            case CartaDefinicion.TipoEfecto.Intercambio:
                Debug.Log("Intercambio - implementar lógica especial");
                break;
                
            default:
                Debug.LogWarning($"Tipo de efecto no implementado: {carta.tipoEfecto}");
                break;
        }
    }
    
    private void AplicarMovimiento(MovimientoFicha ficha, int casillas)
    {
        ficha.Avanzar(casillas);
    }
    
    private void AplicarDefensa(MovimientoFicha ficha, int cantidad)
    {
        // TODO: implementar sistema de defensa/escudo
        Debug.Log($"{ficha.name} recibe {cantidad} de defensa");
    }
    
    private void AplicarTurno(MovimientoFicha ficha, int turnos)
    {
        // TODO: implementar pérdida de turno
        Debug.Log($"{ficha.name} pierde {turnos} turno(s)");
    }
    
    private void AplicarEnergia(MovimientoFicha ficha, int cantidad)
    {
        // TODO: implementar sistema de energía
        Debug.Log($"{ficha.name} cambia energía en {cantidad}");
    }
    
    private void AplicarCartaEfecto(MovimientoFicha ficha, int cantidad)
    {
        // TODO: implementar robo/descarte de cartas
        Debug.Log($"{ficha.name} roba/descarta {cantidad} carta(s)");
    }
}