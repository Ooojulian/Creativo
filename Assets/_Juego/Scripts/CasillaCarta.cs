using UnityEngine;

/// <summary>
/// Representa una casilla del tablero que puede contener una carta (ventaja/desventaja/ninguna)
/// </summary>
public class CasillaCarta : MonoBehaviour
{
    [SerializeField] private CartaDefinicion cartaAsociada;
    [SerializeField] private bool tieneCarta = false;
    [SerializeField] private bool esVentaja = false; // true=ventaja, false=desventaja
    
    private int numeroCasilla; // 0-39 aprox
    private Renderer rendererCasilla;
    
    void Awake()
    {
        rendererCasilla = GetComponent<Renderer>();
    }
    
    public CartaDefinicion ObtenerCarta() => cartaAsociada;
    public bool TieneCarta() => tieneCarta;
    public bool EsVentaja() => esVentaja;
    public int ObtenerNumeroCasilla() => numeroCasilla;
    
    /// <summary>
    /// Asigna una carta a esta casilla
    /// </summary>
    public void AsignarCarta(CartaDefinicion carta, bool ventaja)
    {
        if (carta == null)
        {
            LimpiarCarta();
            return;
        }
        
        cartaAsociada = carta;
        tieneCarta = true;
        esVentaja = ventaja;
        
        ActualizarVisual();
        Debug.Log($"[Casilla {numeroCasilla}] Carta asignada: {carta.nombreCarta} ({(ventaja ? "Ventaja" : "Desventaja")})");
    }
    
    /// <summary>
    /// Deja la casilla sin carta
    /// </summary>
    public void LimpiarCarta()
    {
        cartaAsociada = null;
        tieneCarta = false;
        esVentaja = false;
        
        ActualizarVisual();
        Debug.Log($"[Casilla {numeroCasilla}] Casilla limpiada");
    }
    
    /// <summary>
    /// Actualiza el visual de la casilla según su estado
    /// </summary>
    private void ActualizarVisual()
    {
        if (rendererCasilla == null) return;
        
        if (!tieneCarta)
        {
            // Sin carta = gris neutro
            rendererCasilla.material.color = Color.gray;
        }
        else if (esVentaja)
        {
            // Ventaja = verde
            rendererCasilla.material.color = Color.green;
        }
        else
        {
            // Desventaja = rojo
            rendererCasilla.material.color = Color.red;
        }
    }
    
    public void SetNumeroCasilla(int numero) => numeroCasilla = numero;
}