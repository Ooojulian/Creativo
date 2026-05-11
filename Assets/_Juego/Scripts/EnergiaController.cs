using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de economía de Energía modular.
/// Debe adjuntarse a cada GameObject de Jugador (Ficha).
/// </summary>
public class EnergiaController : MonoBehaviour
{
    [Header("Estado")]
    [SerializeField] private int energiaActual = 0;

    [Header("Eventos")]
    public UnityEvent<int> OnEnergiaCambiada;
    public UnityEvent<int> OnEnergiaInsuficiente;

    // Métodos públicos de consulta y modificación
    public int EnergiaActual => energiaActual;

    public void GanarEnergia(int cantidad)
    {
        if (cantidad <= 0) return;
        energiaActual += cantidad;
        OnEnergiaCambiada?.Invoke(energiaActual);
        Debug.Log($"[Energia] {name} ganó {cantidad}. Total: {energiaActual}");
    }

    public bool GastarEnergia(int costo)
    {
        if (TieneEnergia(costo))
        {
            energiaActual -= costo;
            OnEnergiaCambiada?.Invoke(energiaActual);
            Debug.Log($"[Energia] {name} gastó {costo}. Restante: {energiaActual}");
            return true;
        }
        
        OnEnergiaInsuficiente?.Invoke(costo);
        Debug.Log($"[Energia] {name} no tiene suficiente energía para costo {costo}");
        return false;
    }

    public bool TieneEnergia(int costo)
    {
        return energiaActual >= costo;
    }
}
