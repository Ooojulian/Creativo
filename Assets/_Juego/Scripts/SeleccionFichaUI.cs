using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Panel con dos botones: "Mover Ficha A" y "Mover Ficha B"
// Aparece al inicio del turno, antes de tirar el dado
public class SeleccionFichaUI : MonoBehaviour
{
    public GameObject panel;
    public Button botonFichaA;
    public Button botonFichaB;
    public TextMeshProUGUI textoFichaA;
    public TextMeshProUGUI textoFichaB;

    public GameManager gameManager;

    private MovimientoFicha jugadorActual;
    private int pasosActuales;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (botonFichaA != null) botonFichaA.onClick.AddListener(() => OnElegirFicha(false));
        if (botonFichaB != null) botonFichaB.onClick.AddListener(() => OnElegirFicha(true));
    }

    public void MostrarSeleccion(MovimientoFicha jugador)
    {
        MostrarSeleccionConPasos(jugador, 0);
    }

    public void MostrarSeleccionConPasos(MovimientoFicha jugador, int pasos)
    {
        jugadorActual = jugador;
        pasosActuales = pasos;

        if (textoFichaA != null)
            textoFichaA.text = $"Ficha A\nCasilla: {jugador.indiceActual}";
        if (textoFichaB != null && jugador.fichaB != null)
            textoFichaB.text = $"Ficha B\nCasilla: {jugador.fichaB.indiceActual}";

        if (panel != null) panel.SetActive(true);
    }

    public void OcultarPanel()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnElegirFicha(bool esB)
    {
        if (jugadorActual == null) return;
        if (panel != null) panel.SetActive(false);

        // Mover con los pasos del dado via RPC a todos los clientes
        DadoLogico.MoverViaRPC(jugadorActual, pasosActuales, esB);
    }
}