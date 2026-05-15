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
        
        var energia = jugador.GetComponent<EnergiaController>();
        int energiaActual = energia != null ? energia.EnergiaActual : 0;
        
        textoCarta.text = $"¿Qué deseas hacer con {card.cardName}?\n" +
                          $"<size=80%>Costo: {card.costoEnergia} Energía (Tienes: {energiaActual})</size>";
        
        // Deshabilitar usar si no hay energía
        botonUsar.interactable = energia != null && energia.TieneEnergia(card.costoEnergia);
        
        // Deshabilitar guardar si reserva llena
        botonGuardar.interactable = jugador.inventario.reserve.Count < jugador.inventario.maxReserveSize;

        panel.SetActive(true);
    }

    public void OnClickUsar()
    {
        var energia = jugadorActual.GetComponent<EnergiaController>();
        if (energia != null && energia.GastarEnergia(cartaSeleccionada.costoEnergia))
        {
            panel.SetActive(false);
            jugadorActual.inventario.RemoveFromHand(cartaSeleccionada);
            CardManager.Instance.EjecutarEfectoInmediato(cartaSeleccionada, jugadorActual);
        }
        else
        {
            Debug.Log("No tienes suficiente energía.");
            // Opcionalmente cerrar el panel o mostrar feedback
            panel.SetActive(false);
        }
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
