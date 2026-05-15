using UnityEngine;
using System;

/// <summary>
/// Gestiona la energía de un jugador.
/// - Ganar energía al ganar batallas (piedra/papel/tijeras)
/// - Ganar 1 energía al finalizar turno
/// - Gastar energía al usar cartas
/// </summary>
public class SistemaEnergia : MonoBehaviour
{
    [Header("Energía")]
    [SerializeField] private int energiaActual = 0;
    [SerializeField] private int energiaMaxima = 10;
    
    [Header("Valores")]
    [SerializeField] private int energiaAlGanarBatalla = 2;
    [SerializeField] private int energiaAlFinalizarTurno = 1;
    
    // Eventos para UI
    public event Action<int, int> OnEnergiaChanged; // (actual, máxima)
    public event Action OnEnergiaInsuficiente;
    
    void Start()
    {
        // Iniciar con algo de energía
        energiaActual = Mathf.Min(3, energiaMaxima);
        OnEnergiaChanged?.Invoke(energiaActual, energiaMaxima);
    }
    
    // ────────────────────────────────────────
    // GANAR ENERGÍA
    // ────────────────────────────────────────
    
    public void GanarEnergiaAlGanarBatalla()
    {
        GanarEnergia(energiaAlGanarBatalla);
        Debug.Log($"[Energía] +{energiaAlGanarBatalla} por ganar batalla. Total: {energiaActual}/{energiaMaxima}");
    }
    
    public void GanarEnergiaAlFinalizarTurno()
    {
        GanarEnergia(energiaAlFinalizarTurno);
        Debug.Log($"[Energía] +{energiaAlFinalizarTurno} por finalizar turno. Total: {energiaActual}/{energiaMaxima}");
    }
    
    public void GanarEnergia(int cantidad)
    {
        energiaActual = Mathf.Min(energiaActual + cantidad, energiaMaxima);
        OnEnergiaChanged?.Invoke(energiaActual, energiaMaxima);
    }
    
    // ────────────────────────────────────────
    // GASTAR ENERGÍA
    // ────────────────────────────────────────
    
    public bool PuedeGastar(int cantidad)
    {
        return energiaActual >= cantidad;
    }
    
    public bool GastarEnergia(int cantidad)
    {
        if (!PuedeGastar(cantidad))
        {
            Debug.LogWarning($"[Energía] Insuficiente. Tiene {energiaActual}, necesita {cantidad}.");
            OnEnergiaInsuficiente?.Invoke();
            return false;
        }
        
        energiaActual -= cantidad;
        OnEnergiaChanged?.Invoke(energiaActual, energiaMaxima);
        Debug.Log($"[Energía] -{cantidad}. Total: {energiaActual}/{energiaMaxima}");
        return true;
    }
    
    // ────────────────────────────────────────
    // GETTERS
    // ────────────────────────────────────────
    
    public int ObtenerEnergiaActual() => energiaActual;
    public int ObtenerEnergiaMaxima() => energiaMaxima;
    public float ObtenerPorcentajeEnergia() => (float)energiaActual / energiaMaxima;
}