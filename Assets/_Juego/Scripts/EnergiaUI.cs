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
        if (textoEnergia != null) 
        {
            textoEnergia.gameObject.SetActive(false);
            Debug.Log("[EnergíaUI] Texto inicializado y oculto.");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted += ActualizarSuscripcion;
            
            // Si el juego ya inició (por ejemplo, al recargar escena o script), forzar actualización
            if (GameManager.Instance.JugadorActual != null)
            {
                Debug.Log($"[EnergíaUI] Detectado jugador actual {GameManager.Instance.JugadorActual.name} al inicio.");
                ActualizarSuscripcion(GameManager.Instance.JugadorActual);
            }
        }
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
        if (jugador == null) return;

        // Desvincular anterior
        if (currentController != null)
            currentController.OnEnergiaCambiada.RemoveListener(RefrescarTexto);

        // Vincular nuevo
        currentController = jugador.GetComponent<EnergiaController>();
        if (currentController != null)
        {
            currentController.OnEnergiaCambiada.AddListener(RefrescarTexto);
            if (textoEnergia != null) 
            {
                textoEnergia.gameObject.SetActive(true);
                RefrescarTexto(currentController.EnergiaActual);
                Debug.Log($"[EnergíaUI] Mostrando energía para {jugador.name}: {currentController.EnergiaActual}");
            }
        }
        else
        {
            Debug.LogWarning($"[EnergíaUI] {jugador.name} no tiene EnergiaController.");
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
