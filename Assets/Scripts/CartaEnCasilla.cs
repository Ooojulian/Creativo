using UnityEngine;

public class CartaEnCasilla : MonoBehaviour
{
    [Header("Modo de carta")]
    public bool aleatoria = true;

    [Tooltip("Si aleatoria=false, se usa esta carta fija")]
    public CardSO cartaFija;

    [Header("Pool de cartas (si es aleatoria)")]
    public CardSO[] poolDeCartas;

    public CardSO ObtenerCarta()
    {
        if (!aleatoria) return cartaFija;

        if (poolDeCartas == null || poolDeCartas.Length == 0) return null;

        return poolDeCartas[Random.Range(0, poolDeCartas.Length)];
    }
}