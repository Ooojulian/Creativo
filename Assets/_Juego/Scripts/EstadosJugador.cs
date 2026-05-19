using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Gestiona estados activos del jugador (escudo, maldición, silencio, etc.)
/// Cada estado tiene duración medida en turnos.
/// </summary>
public class EstadosJugador : MonoBehaviour
{
    [System.Serializable]
    public class Estado
    {
        public string nombre;
        public int turnosRestantes;
        
        public Estado(string nombre, int turnos)
        {
            this.nombre = nombre;
            this.turnosRestantes = turnos;
        }
        
        public void DecrementarTurno()
        {
            turnosRestantes = Mathf.Max(0, turnosRestantes - 1);
        }
        
        public bool Activo() => turnosRestantes > 0;
    }
    
    private Dictionary<string, Estado> estadosActivos = new Dictionary<string, Estado>();
    
    // Constantes de nombres
    public const string ESCUDO = "Escudo";
    public const string MALDICION = "Maldición";
    public const string SILENCIO = "Silencio";
    public const string DOBLE_TURNO = "DobleTurno";
    public const string CONGELADO = "Congelado";
    
    // Eventos
    public event Action<string, int> OnEstadoAplicado;  // (nombre, duración)
    public event Action<string> OnEstadoExpired;        // (nombre)
    public event Action OnEstadosCambiados;
    
    // ────────────────────────────────────────
    // APLICAR/MODIFICAR ESTADOS
    // ────────────────────────────────────────
    
    /// <summary>
    /// Aplica un estado al jugador por X turnos.
    /// Si ya existe, lo renueva.
    /// </summary>
    public void AplicarEstado(string nombre, int turnos)
    {
        if (turnos <= 0)
        {
            Debug.LogWarning($"[Estados] Intentando aplicar {nombre} con {turnos} turnos");
            return;
        }
        
        if (estadosActivos.ContainsKey(nombre))
        {
            estadosActivos[nombre].turnosRestantes = Mathf.Max(
                estadosActivos[nombre].turnosRestantes, 
                turnos
            );
            Debug.Log($"[Estados] {nombre} renovado por {turnos} turno(s)");
        }
        else
        {
            estadosActivos[nombre] = new Estado(nombre, turnos);
            Debug.Log($"[Estados] {nombre} aplicado por {turnos} turno(s)");
        }
        
        OnEstadoAplicado?.Invoke(nombre, turnos);
        OnEstadosCambiados?.Invoke();
    }
    
    public void AumentarDuracion(string nombre, int turnos)
    {
        if (estadosActivos.ContainsKey(nombre))
            estadosActivos[nombre].turnosRestantes += turnos;
    }
    
    // ────────────────────────────────────────
    // REMOVER ESTADOS
    // ────────────────────────────────────────
    
    public void RemoverEstado(string nombre)
    {
        if (estadosActivos.Remove(nombre))
        {
            Debug.Log($"[Estados] {nombre} removido");
            OnEstadoExpired?.Invoke(nombre);
            OnEstadosCambiados?.Invoke();
        }
    }
    
    /// <summary>
    /// Llama esto al final del turno para decrementar duraciones.
    /// </summary>
    public void DecrementarTodosLosEstados()
    {
        List<string> paraRemover = new List<string>();
        
        foreach (var kvp in estadosActivos)
        {
            kvp.Value.DecrementarTurno();
            if (!kvp.Value.Activo())
            {
                paraRemover.Add(kvp.Key);
            }
        }
        
        foreach (var nombre in paraRemover)
        {
            Debug.Log($"[Estados] {nombre} expiró");
            OnEstadoExpired?.Invoke(nombre);
            estadosActivos.Remove(nombre);
        }
        
        if (paraRemover.Count > 0)
            OnEstadosCambiados?.Invoke();
    }
    
    // ────────────────────────────────────────
    // VERIFICAR ESTADOS
    // ────────────────────────────────────────
    
    public bool TieneEstado(string nombre) 
        => estadosActivos.ContainsKey(nombre) && estadosActivos[nombre].Activo();
    
    public int ObtenerDuracionEstado(string nombre)
    {
        if (estadosActivos.ContainsKey(nombre))
            return estadosActivos[nombre].turnosRestantes;
        return 0;
    }
    
    public Dictionary<string, Estado> ObtenerTodosLosEstados()
        => new Dictionary<string, Estado>(estadosActivos);
    
    public bool EstaSilenciado() => TieneEstado(SILENCIO);
    public bool TieneEscudo() => TieneEstado(ESCUDO);
    public bool EstaCongelado() => TieneEstado(CONGELADO);
    public bool TieneDobleTurno() => TieneEstado(DOBLE_TURNO);
    
    public void LimpiarTodosLosEstados()
    {
        estadosActivos.Clear();
        OnEstadosCambiados?.Invoke();
    }
}