using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Define una carta individual con todos sus atributos.
/// Es un ScriptableObject para que sea editable en Inspector y reutilizable.
/// </summary>
[CreateAssetMenu(fileName = "Nueva Carta", menuName = "Juego de Mesa/Carta Definición")]
public class CartaDefinicion : ScriptableObject
{
    public enum TipoCarta { Ventaja, Desventaja }
    public enum Alcance { Jugador, DesdeJugador, Global, Casilla }
    public enum TipoEfecto 
    { 
        Movimiento,
        Turno,
        Defensa,
        Energía,
        Estado,
        Carta,
        Intercambio
    }

    [Header("Identidad")]
    public string nombreCarta;
    public string descripcion;
    public Sprite icono;
    
    [Header("Costos")]
    public int costoEnergia = 1;
    
    [Header("Tipo")]
    public TipoCarta tipo;
    
    [Header("Alcance")]
    public Alcance alcance;
    
    [Header("Efecto")]
    public TipoEfecto tipoEfecto;
    
    [Header("Parámetros del Efecto")]
    public int valor1 = 0;
    public int valor2 = 0;
    public float duracion = 0f;
    
    [Header("Validación")]
    public bool puedeUsarseEnMano = true;
    public bool puedeUsarseEnCasilla = true;
    
    // ────────────────────────────────────────
    // MÉTODOS DE UTILIDAD
    // ────────────────────────────────────────
    
    public bool EsVentaja() => tipo == TipoCarta.Ventaja;
    public bool EsDesventaja() => tipo == TipoCarta.Desventaja;
    
    public bool PuedeCambiarseObjetivo() => alcance == Alcance.DesdeJugador;
    public bool EsGlobal() => alcance == Alcance.Global;
    
    public string ObtenerNombreTipoEfecto() => tipoEfecto.ToString();
}