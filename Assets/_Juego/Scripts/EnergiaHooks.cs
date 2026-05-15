using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Conecta los eventos del juego (Turnos, Batallas, Metas) con el sistema de Energía.
/// [DefaultExecutionOrder(100)] garantiza que este script corre DESPUÉS de GameManager (orden 0),
/// para que GameManager.Instance ya exista cuando Awake se llame aquí.
/// </summary>
[DefaultExecutionOrder(100)]
public class EnergiaHooks : MonoBehaviour
{
    public static EnergiaHooks Instance;

    [Header("Configuración Premios")]
    public int energiaPorTurno = 1;
    public int energiaPorBatallaGanada = 2;
    public int energiaPorMetaIntermedia = 3;

    private Dictionary<MovimientoFicha, EnergiaController> controllers = new Dictionary<MovimientoFicha, EnergiaController>();
    private MovimientoFicha ultimoGanadorBatalla = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Energia] Múltiples EnergiaHooks detectados. Destruyendo el duplicado.");
            Destroy(this);
            return;
        }
        Instance = this;

        // Suscribirse aquí, en Awake, garantizado que GameManager ya corrió su Awake
        // gracias a [DefaultExecutionOrder(100)]
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted += HandleTurnStarted;
        }

        if (BatallaPPS.Instance != null)
        {
            BatallaPPS.Instance.OnBattleResult += HandleBattleResult;
        }

        // Crear los EnergiaController en cada jugador ya en Awake
        InicializarControladores();
    }

    void Start()
    {
        // Ya no damos energía manualmente aquí.
        // Como nos suscribimos en Awake() y tenemos [DefaultExecutionOrder(100)],
        // estamos garantizados de escuchar el OnTurnStarted que dispara GameManager.Start().
        InicializarControladores(); // Re-correr por si los jugadores se activan en Start
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted -= HandleTurnStarted;

        if (BatallaPPS.Instance != null)
            BatallaPPS.Instance.OnBattleResult -= HandleBattleResult;
    }


    private void InicializarControladores()
    {
        if (GameManager.Instance == null) return;

        foreach (var j in GameManager.Instance.todosLosJugadores)
        {
            if (j == null) continue;
            var ctrl = j.GetComponent<EnergiaController>();
            if (ctrl == null) ctrl = j.gameObject.AddComponent<EnergiaController>();
            
            if (!controllers.ContainsKey(j))
                controllers[j] = ctrl;
        }
    }


    private void HandleTurnStarted(MovimientoFicha jugador)
    {
        if (jugador == null) return;

        if (!controllers.ContainsKey(jugador))
        {
            var c = jugador.GetComponent<EnergiaController>();
            if (c == null) c = jugador.gameObject.AddComponent<EnergiaController>();
            controllers[jugador] = c;
        }

        var ctrl = controllers[jugador];
        ctrl.GanarEnergia(energiaPorTurno);

        // 2. Ganar +2 si ganó batalla previa
        if (ultimoGanadorBatalla == jugador)
        {
            ctrl.GanarEnergia(energiaPorBatallaGanada);
            ultimoGanadorBatalla = null; // Resetear
        }

        // 3. Ganar +3 si está en meta intermedia
        // Definimos "meta intermedia" como cualquier casilla tipo NodeType.Finish que NO sea la última de la ruta
        // O si simplemente está en una casilla específica. Aquí usaremos el BoardNode.nodeType.
        if (jugador.ruta != null && jugador.indiceActual > 0 && jugador.indiceActual < jugador.ruta.casillas.Count - 1)
        {
            var node = jugador.ruta.casillas[jugador.indiceActual].GetComponent<BoardNode>();
            if (node != null && node.nodeType == BoardNode.NodeType.Finish)
            {
                ctrl.GanarEnergia(energiaPorMetaIntermedia);
            }
        }
    }

    private void HandleBattleResult(int ganador, int idxAtk, int idxDef)
    {
        if (ganador == 0) return; // Empate

        MovimientoFicha fichaGanadora = null;
        if (ganador == 1) fichaGanadora = GameManager.Instance.todosLosJugadores[idxAtk];
        else if (ganador == 2) fichaGanadora = GameManager.Instance.todosLosJugadores[idxDef];

        if (fichaGanadora != null)
        {
            // Si queremos que la energía se gane INSTANTÁNEAMENTE:
            if (controllers.TryGetValue(fichaGanadora, out var ctrl))
            {
                ctrl.GanarEnergia(energiaPorBatallaGanada);
            }
            // O si queremos que se gane al INICIO del siguiente turno del jugador:
            // ultimoGanadorBatalla = fichaGanadora; 
        }
    }

    // Métodos de ayuda para la UI u otros sistemas
    public EnergiaController GetController(MovimientoFicha jugador)
    {
        if (controllers.TryGetValue(jugador, out var ctrl)) return ctrl;
        return null;
    }
}
