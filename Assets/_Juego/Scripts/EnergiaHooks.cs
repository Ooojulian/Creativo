using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Conecta los eventos del juego (Turnos, Batallas, Metas) con el sistema de Energía.
/// </summary>
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
        Instance = this;
    }

    void Start()
    {
        // Suscribirse a eventos del core (añadidos previamente)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted += HandleTurnStarted;
            
            // Inicializar controladores
            foreach (var j in GameManager.Instance.todosLosJugadores)
            {
                var ctrl = j.GetComponent<EnergiaController>();
                if (ctrl == null) ctrl = j.gameObject.AddComponent<EnergiaController>();
                controllers[j] = ctrl;
            }
        }

        if (BatallaPPS.Instance != null)
        {
            BatallaPPS.Instance.OnBattleResult += HandleBattleResult;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted -= HandleTurnStarted;

        if (BatallaPPS.Instance != null)
            BatallaPPS.Instance.OnBattleResult -= HandleBattleResult;
    }

    private void HandleTurnStarted(MovimientoFicha jugador)
    {
        if (jugador == null) return;
        
        // Asegurar que el jugador tenga controlador si no lo tenía en el Start
        if (!controllers.ContainsKey(jugador))
        {
            var c = jugador.GetComponent<EnergiaController>();
            if (c == null) c = jugador.gameObject.AddComponent<EnergiaController>();
            controllers[jugador] = c;
        }

        var ctrl = controllers[jugador];
        Debug.Log($"[Energía] Inicio de turno para {jugador.name}. Procesando premios...");

        // 1. Ganar +1 automático
        ctrl.GanarEnergia(energiaPorTurno);
        Debug.Log($"[Energía] {jugador.name} recibe +{energiaPorTurno} de energía por inicio de turno.");

        // 2. Ganar +2 si ganó batalla previa
        if (ultimoGanadorBatalla == jugador)
        {
            ctrl.GanarEnergia(energiaPorBatallaGanada);
            Debug.Log($"[Energía] {jugador.name} recibe +{energiaPorBatallaGanada} por victoria en batalla previa.");
            ultimoGanadorBatalla = null; // Resetear
        }

        // 3. Ganar +3 si está en meta intermedia
        if (jugador.ruta != null && jugador.indiceActual > 0 && jugador.indiceActual < jugador.ruta.casillas.Count - 1)
        {
            var node = jugador.ruta.casillas[jugador.indiceActual].GetComponent<BoardNode>();
            if (node != null && node.nodeType == BoardNode.NodeType.Finish)
            {
                ctrl.GanarEnergia(energiaPorMetaIntermedia);
                Debug.Log($"[Energía] {jugador.name} recibe +{energiaPorMetaIntermedia} por estar en meta intermedia.");
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
