using UnityEngine;

public enum TipoCarta
{
    Ninguna,

    // Ventajas
    AvanceRapido,   // +2
    Escudo,
    DobleTiro,

    // Desventajas
    Retroceso,      // -2
    PierdeTurno,
    Intercambio
}

public class CartaEnCasilla : MonoBehaviour
{
    [Header("Modo de carta")]
    public bool aleatoria = true;

    [Tooltip("Si aleatoria=false, se usa esta carta fija")]
    public TipoCarta cartaFija = TipoCarta.Ninguna;

    public TipoCarta ObtenerCarta()
    {
        if (!aleatoria) return cartaFija;

        // Todas las cartas menos Ninguna
        TipoCarta[] posibles = new TipoCarta[]
        {
            TipoCarta.AvanceRapido,
            TipoCarta.Escudo,
            TipoCarta.DobleTiro,
            TipoCarta.Retroceso,
            TipoCarta.PierdeTurno,
            TipoCarta.Intercambio
        };

        return posibles[Random.Range(0, posibles.Length)];
    }
}