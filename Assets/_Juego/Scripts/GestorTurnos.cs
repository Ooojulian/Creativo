using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class GestorTurnos : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DadoLogico dado;
    [SerializeField] private EfectoCarta efectoCarta;
    [Header("UI")]
    [SerializeField] private UIPanelCartas uiPanelCartas;
    
    private List<MovimientoFicha> jugadores = new List<MovimientoFicha>();
    private int indiceJugadorActual = 0;
    private MovimientoFicha jugadorActual;
    
    private enum EstadoTurno
    {
        Esperando,
        SeleccionandoCartas,
        TirandoDado,
        Moviendo,
        AplicandoCartas,
        Finalizando,
        Completado
    }
    public void ContinuarDespuesDeSeleccion()
    {
        MostrarSeleccionCartas();
    }
    private EstadoTurno estadoActual = EstadoTurno.Esperando;
    private int turnoActual = 0;
    
    public event Action<MovimientoFicha> OnTurnoIniciado;
    public event Action<MovimientoFicha> OnTurnoFinalizado;
    
    void Start()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (dado == null) dado = FindAnyObjectByType<DadoLogico>();
        if (efectoCarta == null) efectoCarta = FindAnyObjectByType<EfectoCarta>();
        if (uiPanelCartas == null) uiPanelCartas = FindAnyObjectByType<UIPanelCartas>();
        
        Debug.Log($"[GestorTurnos] GameManager: {(gameManager != null ? "OK" : "FALTA")}");
        Debug.Log($"[GestorTurnos] Dado: {(dado != null ? "OK" : "FALTA")}");
        Debug.Log($"[GestorTurnos] EfectoCarta: {(efectoCarta != null ? "OK" : "FALTA")}");
        Debug.Log($"[GestorTurnos] UIPanelCartas: {(uiPanelCartas != null ? "OK" : "FALTA")}");
    }
    
    public void AsignarJugadores(List<MovimientoFicha> nuevosJugadores)
    {
        jugadores = new List<MovimientoFicha>(nuevosJugadores);
        indiceJugadorActual = 0;
        if (jugadores.Count > 0)
            jugadorActual = jugadores[0];
        
        Debug.Log($"[GestorTurnos] {jugadores.Count} jugadores asignados");
    }
    
    public void IniciarTurno()
    {
        if (jugadores.Count == 0)
        {
            Debug.LogError("[GestorTurnos] No hay jugadores asignados");
            return;
        }
        
        jugadorActual = jugadores[indiceJugadorActual];
        
        if (estadoActual != EstadoTurno.Esperando && estadoActual != EstadoTurno.Completado)
        {
            Debug.LogWarning("[GestorTurnos] Intento de iniciar turno mientras hay uno activo");
            return;
        }
        
        estadoActual = EstadoTurno.SeleccionandoCartas;
        turnoActual++;
        
        Debug.Log($"[GestorTurnos] Turno {turnoActual} iniciado para {jugadorActual.name}");
        OnTurnoIniciado?.Invoke(jugadorActual);
        
        // ✅ MOSTRAR SELECCIÓN DE FICHA PRIMERO
        SeleccionFichaUI seleccionUI = FindAnyObjectByType<SeleccionFichaUI>();
        if (seleccionUI != null)
        {
            seleccionUI.MostrarSeleccion(jugadorActual);
        }
        else
        {
            // Si no hay panel, continuar directo a cartas
            MostrarSeleccionCartas();
        }
    }
    
    private void MostrarSeleccionCartas()
    {
        if (uiPanelCartas != null)
        {
            uiPanelCartas.MostrarPanelCartas(
                jugadorActual,
                OnCartasSeleccionadas,
                OnOmitirCartas
            );
        }
        else
        {
            OnOmitirCartas();
        }
    }
    
    private void OnCartasSeleccionadas(List<CartaDefinicion> cartasUsadas)
    {
        Debug.Log($"[GestorTurnos] Jugador usó {cartasUsadas.Count} cartas");
        PasarATirarDado();
    }
    
    private void OnOmitirCartas()
    {
        Debug.Log($"[GestorTurnos] Jugador omitió seleccionar cartas");
        PasarATirarDado();
    }
    
    private void PasarATirarDado()
    {
        estadoActual = EstadoTurno.TirandoDado;
        
        if (dado != null)
        {
            dado.gameObject.SetActive(true);
            dado.PrepararParaLanzar(jugadorActual, OnDadoLanzado);
        }
    }
    
    private void OnDadoLanzado(int resultado)
    {
        estadoActual = EstadoTurno.Moviendo;
        
        if (jugadorActual != null)
        {
            // ✅ VERIFICAR CUÁL FICHA MOVER
            if (jugadorActual.moverFichaB && jugadorActual.fichaB != null)
            {
                jugadorActual.fichaB.Avanzar(resultado);
            }
            else
            {
                jugadorActual.Avanzar(resultado);
            }
        }
    }
    
    public void OnMovimientoCompletado(MovimientoFicha jugador)
    {
        if (jugador != jugadorActual) return;
        
        estadoActual = EstadoTurno.AplicandoCartas;
        StartCoroutine(AplicarCartasDeCasilla());
    }
    
    private IEnumerator AplicarCartasDeCasilla()
    {
        if (gameManager == null || gameManager.casillas == null || gameManager.casillas.Count == 0) 
        {
            FinalizarTurno();
            yield break;
        }
        
        if (jugadorActual.indiceActual < 0 || jugadorActual.indiceActual >= gameManager.casillas.Count) 
        {
            FinalizarTurno();
            yield break;
        }
        
        Transform casilla = gameManager.casillas[jugadorActual.indiceActual];
        
        SistemaCartas sistemaCartas = gameManager.ObtenerSistemaCartas();
        CartaDefinicion carta = sistemaCartas.ObtenerCartaAleatoria();
        
        if (carta == null) 
        {
            FinalizarTurno();
            yield break;
        }
        
        CartasUIVisual uiCartas = gameManager.ObtenerUICartas();
        if (uiCartas != null)
            uiCartas.MostrarRevelacion(carta);
        
        yield return new WaitForSeconds(2f);
        
        bool esDesventaja = carta.tipo == CartaDefinicion.TipoCarta.Desventaja;
        bool tieneEscudo = jugadorActual.ObtenerEstados().TieneEscudo();
        
        if (esDesventaja && tieneEscudo)
        {
            jugadorActual.ObtenerEstados().RemoverEstado(EstadosJugador.ESCUDO);
            Debug.Log($"[Cartas] {jugadorActual.name} bloqueó {carta.nombreCarta} con Escudo");
            yield return new WaitForSeconds(1f);
        }
        else
        {
            if (efectoCarta != null)
                efectoCarta.AplicarCartaA(carta, jugadorActual);
            yield return new WaitForSeconds(1f);
        }
        
        if (uiCartas != null)
            yield return StartCoroutine(uiCartas.FadeOutYLimpiar());
        
        FinalizarTurno();
    }

    private void FinalizarTurno()
    {
        SistemaEnergia energia = jugadorActual.ObtenerEnergia();
        energia.GanarEnergiaAlFinalizarTurno();
        
        EstadosJugador estados = jugadorActual.ObtenerEstados();
        estados.DecrementarTodosLosEstados();
        
        Debug.Log($"[GestorTurnos] Turno finalizado para {jugadorActual.name}");
        OnTurnoFinalizado?.Invoke(jugadorActual);
        
        estadoActual = EstadoTurno.Completado;
        
        CambiarAlSiguienteJugador();
    }
    
    private void CambiarAlSiguienteJugador()
    {
        indiceJugadorActual = (indiceJugadorActual + 1) % jugadores.Count;
        Debug.Log($"[GestorTurnos] Siguiente turno: Jugador índice {indiceJugadorActual}");
        
        StartCoroutine(EsperarYContinuar());
    }
    
    private IEnumerator EsperarYContinuar()
    {
        yield return new WaitForSeconds(1f);
        IniciarTurno();
    }
    
    public void PasarTurno()
    {
        Debug.Log($"[GestorTurnos] {jugadorActual.name} pasó turno");
        FinalizarTurno();
    }
}