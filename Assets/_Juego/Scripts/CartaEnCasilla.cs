using UnityEngine;

public enum TipoCartaAleatoria { Cualquiera, Ventaja, Desventaja }

public class CartaEnCasilla : MonoBehaviour
{
    [Header("Modo de carta")]
    public bool aleatoria = true;
    public TipoCartaAleatoria tipoAleatoria = TipoCartaAleatoria.Cualquiera;

    [Tooltip("Si aleatoria=false, se usa esta carta fija")]
    public CardSO cartaFija;

    [Header("Pool de cartas (si es Cualquiera)")]
    public CardSO[] poolDeCartas;

    public CardSO ObtenerCarta()
    {
        if (!aleatoria) return cartaFija;

        if (tipoAleatoria == TipoCartaAleatoria.Ventaja && CardManager.Instance != null)
        {
            return CardManager.Instance.ObtenerCartaVentaja();
        }
        else if (tipoAleatoria == TipoCartaAleatoria.Desventaja && CardManager.Instance != null)
        {
            return CardManager.Instance.ObtenerCartaDesventaja();
        }

        if (poolDeCartas != null && poolDeCartas.Length > 0)
        {
            return poolDeCartas[Random.Range(0, poolDeCartas.Length)];
        }

        if (CardManager.Instance != null)
        {
            return CardManager.Instance.ObtenerCartaAleatoria();
        }

        return null;
    }
}