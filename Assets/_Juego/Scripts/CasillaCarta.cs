using UnityEngine;

/// <summary>
/// Representa una casilla que puede contener una carta (ventaja/desventaja/ninguna)
/// </summary>
public class CasillaCarta : MonoBehaviour
{
    [SerializeField] private CartaDefinicion cartaAsociada;
    [SerializeField] private bool tieneCarta = false;
    [SerializeField] private bool esVentaja = false; // true=ventaja, false=desventaja
    
    private int numeroCasilla; // 1-40 aprox
    
    public CartaDefinicion ObtenerCarta() => cartaAsociada;
    public bool TieneCarta() => tieneCarta;
    public bool EsVentaja() => esVentaja;
    
    public void AsignarCarta(CartaDefinicion carta, bool ventaja)
    {
        cartaAsociada = carta;
        tieneCarta = true;
        esVentaja = ventaja;
        
        // Aquí puedes hacer visual: cambiar color, mostrar icono, etc
        ActualizarVisual();
    }
    
    public void LimpiarCarta()
    {
        cartaAsociada = null;
        tieneCarta = false;
    }
    
    private void ActualizarVisual()
    {
        // TODO: Cambiar color/icono según tipo de carta
        if (tieneCarta)
        {
            if (esVentaja)
                GetComponent<Renderer>().material.color = Color.green;
            else
                GetComponent<Renderer>().material.color = Color.red;
        }
    }
    
    public void SetNumeroCasilla(int numero) => numeroCasilla = numero;
    public int ObtenerNumeroCasilla() => numeroCasilla;
}