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

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (botonFichaA != null) botonFichaA.onClick.AddListener(() => ElegirFicha(false));
        if (botonFichaB != null) botonFichaB.onClick.AddListener(() => ElegirFicha(true));
    }

    public void MostrarSeleccion(MovimientoFicha jugador)
    {
        if (panel != null) panel.SetActive(true);

        // Mostrar info de cada ficha
        if (textoFichaA != null)
            textoFichaA.text = $"Ficha A\nCasilla: {jugador.indiceActual}";

        if (textoFichaB != null && jugador.fichaB != null)
            textoFichaB.text = $"Ficha B\nCasilla: {jugador.fichaB.indiceActual}";

        // Guardar referencia para cuando elija
        jugadorActual = jugador;
    }

    public void OcultarPanel()
    {
        if (panel != null) panel.SetActive(false);
    }

    private MovimientoFicha jugadorActual;

    private void ElegirFicha(bool esB)
    {
        if (jugadorActual == null) return;
        jugadorActual.ElegirFicha(esB);

        if (panel != null) panel.SetActive(false);

        // Ejecutar movimiento con resultado del dado ya tirado
        if (gameManager != null && gameManager.dado != null)
            gameManager.dado.EjecutarMovimiento();
    }
}
