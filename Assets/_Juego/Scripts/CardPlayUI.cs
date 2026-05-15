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

    private CartaDefinicion cartaSeleccionada;
    private MovimientoFicha jugadorActual;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Mostrar(CartaDefinicion carta, MovimientoFicha jugador)
    {
        cartaSeleccionada = carta;
        jugadorActual = jugador;
        textoCarta.text = $"¿Qué deseas hacer con {carta.nombreCarta}?";
        
        panel.SetActive(true);
    }

    public void OnClickUsar()
    {
        if (jugadorActual == null) return;
        
        panel.SetActive(false);
        var inventario = jugadorActual.ObtenerInventarioCartas();
        if (inventario != null)
        {
            inventario.UsarCarta(cartaSeleccionada);
        }
    }

    public void OnClickGuardar()
    {
        panel.SetActive(false);
        // TODO: Implementar sistema de reserva si lo necesitas
    }

    public void OnClickCerrar()
    {
        panel.SetActive(false);
    }
}