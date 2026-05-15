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
        try
        {
            cartaSeleccionada = card;
            jugadorActual = jugador;
            
            var energia = jugador.GetComponent<EnergiaController>();
            int energiaActual = energia != null ? energia.EnergiaActual : 0;
            
            // Deshabilitar todo si está silenciado
            if (jugador.silencioActivo)
            {
                textoCarta.text = $"<color=red>¡ESTÁS SILENCIADO!</color>\nNo puedes usar cartas este turno.";
                botonUsar.interactable = false;
                botonGuardar.interactable = false;
            }
            else
            {
                bool tieneEnergia = energia != null && energia.TieneEnergia(card.costoEnergia);
                
                textoCarta.text = $"¿Qué deseas hacer con {card.cardName}?\n" +
                                  $"<size=80%>Costo: {card.costoEnergia} Energía (Tienes: {energiaActual})</size>";
                
                if (!tieneEnergia)
                {
                    textoCarta.text += "\n<color=red>¡No tienes suficiente energía!</color>";
                }
                
                // Habilitar usar solo si hay energía
                botonUsar.interactable = tieneEnergia;
                
                // Deshabilitar guardar si reserva llena
                if (jugador.inventario == null) Debug.LogError("¡jugador.inventario es null en Mostrar!");
                else botonGuardar.interactable = jugador.inventario.reserve.Count < jugador.inventario.maxReserveSize;
            }

            panel.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CardPlayUI] Error en Mostrar: {e.Message}\n{e.StackTrace}");
            panel.SetActive(false); // Evitar que se quede trabado si hay error
        }
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
