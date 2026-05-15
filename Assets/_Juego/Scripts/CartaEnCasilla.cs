using UnityEngine;

public enum ModoCasilla { Ninguna, AleatoriaCualquiera, SoloVentaja, SoloDesventaja, CartaFija }

public class CartaEnCasilla : MonoBehaviour
{
    [Header("Configuración de la Casilla")]
    public ModoCasilla modo = ModoCasilla.AleatoriaCualquiera;

    [Tooltip("Solo se usa si el modo es CartaFija")]
    public CardSO cartaEspecifica;

    public CardSO ObtenerCarta()
    {
        if (CardManager.Instance == null) return null;

        switch (modo)
        {
            case ModoCasilla.Ninguna:
                return null;

            case ModoCasilla.AleatoriaCualquiera:
                return CardManager.Instance.ObtenerCartaAleatoria();

            case ModoCasilla.SoloVentaja:
                return CardManager.Instance.ObtenerCartaVentaja();

            case ModoCasilla.SoloDesventaja:
                return CardManager.Instance.ObtenerCartaDesventaja();

            case ModoCasilla.CartaFija:
                return cartaEspecifica;

            default:
                return null;
        }
    }
}