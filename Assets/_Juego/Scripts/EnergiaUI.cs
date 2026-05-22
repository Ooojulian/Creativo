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
        if (textoEnergia != null)
            textoEnergia.gameObject.SetActive(false);
    }

    void Start()
    {
        SuscribirSiPosible();
    }

    void Update()
    {
        // Reintento por si GameManager llegó tarde
        if (currentController == null)
            SuscribirSiPosible();
    }

    private bool _suscrito = false;
    private void SuscribirSiPosible()
    {
        if (_suscrito || GameManager.Instance == null) return;
        _suscrito = true;
        GameManager.Instance.OnTurnStarted += ActualizarSuscripcion;
        if (GameManager.Instance.JugadorActual != null)
            ActualizarSuscripcion(GameManager.Instance.JugadorActual);
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
        if (jugador == null) return;

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
