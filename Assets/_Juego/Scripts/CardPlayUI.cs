using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardPlayUI : MonoBehaviour
{
    public static CardPlayUI Instance;

    public GameObject panel;
    public TextMeshProUGUI textoCarta;
    public Button botonUsar;
    public Button botonGuardar;

    private CardSO cartaSeleccionada;
    private MovimientoFicha jugadorActual;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Mostrar(CardSO card, MovimientoFicha jugador)
    {
        cartaSeleccionada = card;
        jugadorActual = jugador;
        textoCarta.text = $"¿Qué deseas hacer con {card.cardName}?";
        
        // Deshabilitar guardar si reserva llena
        botonGuardar.interactable = jugador.inventario.reserve.Count < jugador.inventario.maxReserveSize;

        panel.SetActive(true);
    }

    public void OnClickUsar()
    {
        panel.SetActive(false);
        jugadorActual.inventario.RemoveFromHand(cartaSeleccionada);
        CardManager.Instance.EjecutarEfectoInmediato(cartaSeleccionada, jugadorActual);
    }

    public void OnClickGuardar()
    {
        panel.SetActive(false);
        jugadorActual.inventario.SaveToReserve(cartaSeleccionada);
    }

    public void OnClickCerrar()
    {
        panel.SetActive(false);
    }
}
