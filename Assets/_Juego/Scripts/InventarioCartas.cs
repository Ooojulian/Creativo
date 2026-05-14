using UnityEngine;
using System.Collections.Generic;
using System;

// Esto especifica que Random = UnityEngine.Random por defecto
using Random = UnityEngine.Random;
/// <summary>
/// Gestiona las cartas en mano del jugador.
/// - Agregar cartas (de casillas o batallas)
/// - Usar cartas (con validación de energía)
/// - Descartar cartas
/// - Limitar a X cartas máximo
/// </summary>
public class InventarioCartas : MonoBehaviour
{
    [Header("Límites")]
    [SerializeField] private int cartasMaximasEnMano = 7;
    
    private List<CartaDefinicion> cartasEnMano = new List<CartaDefinicion>();
    
    // Eventos para UI
    public event Action<List<CartaDefinicion>> OnManoActualizada;
    public event Action<CartaDefinicion> OnCartaAgregada;
    public event Action<CartaDefinicion> OnCartaUsada;
    public event Action<CartaDefinicion> OnCartaDescartada;
    public event Action OnManoLlena;
    
    // ────────────────────────────────────────
    // AGREGAR CARTAS
    // ────────────────────────────────────────
    
    public bool AgregarCarta(CartaDefinicion carta)
    {
        if (carta == null) return false;
        
        // Si la mano está llena, no agregar
        if (cartasEnMano.Count >= cartasMaximasEnMano)
        {
            Debug.LogWarning($"[Inventario] Mano llena ({cartasMaximasEnMano} cartas). No se puede agregar {carta.nombreCarta}");
            OnManoLlena?.Invoke();
            return false;
        }
        
        cartasEnMano.Add(carta);
        OnCartaAgregada?.Invoke(carta);
        OnManoActualizada?.Invoke(new List<CartaDefinicion>(cartasEnMano));
        
        Debug.Log($"[Inventario] +{carta.nombreCarta}. Mano: {cartasEnMano.Count}/{cartasMaximasEnMano}");
        return true;
    }
    
    public void AgregarMultiples(List<CartaDefinicion> cartas)
    {
        foreach (var carta in cartas)
            AgregarCarta(carta);
    }
    
    // ────────────────────────────────────────
    // USAR CARTAS
    // ────────────────────────────────────────
    
    public bool UsarCarta(CartaDefinicion carta)
    {
        if (!cartasEnMano.Contains(carta))
        {
            Debug.LogWarning($"[Inventario] {carta.nombreCarta} no está en mano");
            return false;
        }
        
        cartasEnMano.Remove(carta);
        OnCartaUsada?.Invoke(carta);
        OnManoActualizada?.Invoke(new List<CartaDefinicion>(cartasEnMano));
        
        Debug.Log($"[Inventario] -{carta.nombreCarta}. Mano: {cartasEnMano.Count}/{cartasMaximasEnMano}");
        return true;
    }
    
    // ────────────────────────────────────────
    // DESCARTAR CARTAS
    // ────────────────────────────────────────
    
    public bool DescartarCarta(CartaDefinicion carta)
    {
        if (!cartasEnMano.Contains(carta)) return false;
        
        cartasEnMano.Remove(carta);
        OnCartaDescartada?.Invoke(carta);
        OnManoActualizada?.Invoke(new List<CartaDefinicion>(cartasEnMano));
        
        Debug.Log($"[Inventario] Descartada: {carta.nombreCarta}");
        return true;
    }
    
    /// <summary>
    /// Descarta N cartas al azar (para cartas como "Ruptura").
    /// </summary>
    public List<CartaDefinicion> DescartarCartasAlAzar(int cantidad)
    {
        List<CartaDefinicion> descartadas = new List<CartaDefinicion>();
        cantidad = Mathf.Min(cantidad, cartasEnMano.Count);
        
        for (int i = 0; i < cantidad; i++)
        {
            int idx = Random.Range(0, cartasEnMano.Count);
            CartaDefinicion carta = cartasEnMano[idx];
            descartadas.Add(carta);
            DescartarCarta(carta);
        }
        
        return descartadas;
    }
    
    // ────────────────────────────────────────
    // GETTERS
    // ────────────────────────────────────────
    
    public List<CartaDefinicion> ObtenerMano() 
        => new List<CartaDefinicion>(cartasEnMano);
    
    public int ObtenerCantidadCartas() => cartasEnMano.Count;
    public bool EsManoLlena() => cartasEnMano.Count >= cartasMaximasEnMano;
    public bool TieneCarta(CartaDefinicion carta) => cartasEnMano.Contains(carta);
    
    public void LimpiarMano()
    {
        cartasEnMano.Clear();
        OnManoActualizada?.Invoke(new List<CartaDefinicion>(cartasEnMano));
    }
}