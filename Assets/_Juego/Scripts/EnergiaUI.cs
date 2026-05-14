using UnityEngine;
using TMPro;

/// <summary>
/// Muestra la energía del jugador actual en la UI.
/// </summary>
public class EnergiaUI : MonoBehaviour
{
    public TextMeshProUGUI textoEnergia;
    private EnergiaController currentController;

    void Start()
    {
        if (textoEnergia != null) textoEnergia.gameObject.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted += ActualizarSuscripcion;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted -= ActualizarSuscripcion;
        
        if (currentController != null)
            currentController.OnEnergiaCambiada.RemoveListener(RefrescarTexto);
    }

    private void ActualizarSuscripcion(MovimientoFicha jugador)
    {
        // Desvincular anterior
        if (currentController != null)
            currentController.OnEnergiaCambiada.RemoveListener(RefrescarTexto);

        // Vincular nuevo
        currentController = jugador.GetComponent<EnergiaController>();
        if (currentController != null)
        {
            currentController.OnEnergiaCambiada.AddListener(RefrescarTexto);
            if (textoEnergia != null) textoEnergia.gameObject.SetActive(true);
            RefrescarTexto(currentController.EnergiaActual);
        }
        else
        {
            if (textoEnergia != null) textoEnergia.gameObject.SetActive(false);
        }
    }

    private void RefrescarTexto(int energia)
    {
        if (textoEnergia != null)
        {
            textoEnergia.text = $"Energía: {energia}";
        }
    }
}
