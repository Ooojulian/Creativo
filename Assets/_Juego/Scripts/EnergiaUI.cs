using UnityEngine;
using TMPro;

/// <summary>
/// Muestra la energía del jugador actual en la UI.
/// [DefaultExecutionOrder(101)] garantiza que corre después de EnergiaHooks (100).
/// </summary>
[DefaultExecutionOrder(101)]
public class EnergiaUI : MonoBehaviour
{
    public TextMeshProUGUI textoEnergia;
    private EnergiaController currentController;

    void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted += ActualizarSuscripcion;
        }
    }

    void Start()
    {
        // Ya no conectamos manualmente aquí. Al suscribirnos en Awake,
        // capturaremos automáticamente el OnTurnStarted inicial de GameManager.Start().
    }

    void OnDestroy()
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
        
        // Si aún no tiene el componente, intentamos forzar a EnergiaHooks a crearlo
        if (currentController == null && EnergiaHooks.Instance != null)
        {
            // Forzar inicialización en el hook
            currentController = jugador.gameObject.AddComponent<EnergiaController>();
        }

        if (currentController != null)
        {
            currentController.OnEnergiaCambiada.AddListener(RefrescarTexto);
            if (textoEnergia != null) textoEnergia.gameObject.SetActive(true);
            RefrescarTexto(currentController.EnergiaActual);
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
