using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centraliza todas las definiciones de cartas como una base de datos.
/// Se configura en Inspector con las 12 cartas.
/// </summary>
[CreateAssetMenu(fileName = "SistemaCartas", menuName = "Juego de Mesa/Sistema de Cartas")]
public class SistemaCartas : ScriptableObject
{
    [SerializeField]
    private List<CartaDefinicion> todasLasCartas = new List<CartaDefinicion>();
    
    public List<CartaDefinicion> ObtenerTodasLasCartas() => new List<CartaDefinicion>(todasLasCartas);
    
    public CartaDefinicion ObtenerCartaPorNombre(string nombre) 
        => todasLasCartas.FirstOrDefault(c => c.nombreCarta == nombre);
    
    /// <summary>
    /// Obtiene cartas aleatorias de un tipo específico.
    /// Útil para casillas que generan cartas al azar.
    /// </summary>
    public CartaDefinicion ObtenerCartaAleatoria(CartaDefinicion.TipoCarta tipo)
    {
        var filtradas = todasLasCartas.Where(c => c.tipo == tipo).ToList();
        if (filtradas.Count == 0) return null;
        return filtradas[Random.Range(0, filtradas.Count)];
    }
    
    public CartaDefinicion ObtenerCartaAleatoria()
    {
        if (todasLasCartas.Count == 0) return null;
        return todasLasCartas[Random.Range(0, todasLasCartas.Count)];
    }
    
    /// <summary>
    /// Obtiene cartas que pueden usarse desde la mano del jugador.
    /// </summary>
    public List<CartaDefinicion> ObtenerCartasUsablesEnMano()
        => todasLasCartas.Where(c => c.puedeUsarseEnMano).ToList();
    
    public int ObtenerCantidadCartas() => todasLasCartas.Count;
}