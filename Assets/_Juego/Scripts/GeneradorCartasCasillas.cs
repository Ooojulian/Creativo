using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Genera cartas aleatorias en casillas al inicio del juego
/// </summary>
public class GeneradorCartasCasillas : MonoBehaviour
{
    [SerializeField] private SistemaCartas sistemaCartas; // referencia a todas las cartas
    [SerializeField] private CasillaCarta[] todasLasCasillas; // todas las casillas del tablero
    
    [Header("Configuración de Probabilidad")]
    [SerializeField] [Range(0f, 1f)] private float probabilidadCarta = 0.4f; // 40% de casillas tienen carta
    [SerializeField] [Range(0f, 1f)] private float probabilidadVentaja = 0.5f; // 50% ventaja, 50% desventaja
    
    void Start()
    {
        GenerarCartasEnCasillas();
    }
    
    public void GenerarCartasEnCasillas()
    {
        if (sistemaCartas == null)
        {
            Debug.LogError("SistemaCartas no asignado en GeneradorCartasCasillas");
            return;
        }
        
        if (todasLasCasillas == null || todasLasCasillas.Length == 0)
        {
            Debug.LogError("No hay casillas asignadas");
            return;
        }
        
        foreach (CasillaCarta casilla in todasLasCasillas)
        {
            // Decidir si esta casilla tiene carta
            if (Random.value < probabilidadCarta)
            {
                // Decidir si es ventaja o desventaja
                bool esVentaja = Random.value < probabilidadVentaja;
                
                // Elegir carta aleatoria
                CartaDefinicion cartaAleatoria = ObtenerCartaAleatoria(esVentaja);
                
                if (cartaAleatoria != null)
                {
                    casilla.AsignarCarta(cartaAleatoria, esVentaja);
                    Debug.Log($"Casilla asignada: {cartaAleatoria.nombreCarta} ({(esVentaja ? "Ventaja" : "Desventaja")})");
                }
            }
        }
    }
    
    private CartaDefinicion ObtenerCartaAleatoria(bool ventaja)
    {
        if (sistemaCartas.ObtenerTodasLasCartas() == null)
            return null;
        
        List<CartaDefinicion> cartasDelTipo = new List<CartaDefinicion>();
        
        foreach (CartaDefinicion carta in sistemaCartas.ObtenerTodasLasCartas())
        {
            if (ventaja && carta.EsVentaja())
                cartasDelTipo.Add(carta);
            else if (!ventaja && carta.EsDesventaja())
                cartasDelTipo.Add(carta);
        }
        
        if (cartasDelTipo.Count == 0)
            return null;
        
        return cartasDelTipo[Random.Range(0, cartasDelTipo.Count)];
    }
}