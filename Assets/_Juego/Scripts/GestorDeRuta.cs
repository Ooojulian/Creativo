using UnityEngine;
using System.Collections.Generic;
using System;

public class GestorDeRutas : MonoBehaviour
{
    [Header("Ruta del Tablero")]
    public List<Transform> casillas = new List<Transform>();

    private List<MovimientoFicha> jugadores = new List<MovimientoFicha>();
    private int turnoActual = 0;
    
    private GameManager gameManager;
    private DadoLogico dado;
    private CartasUIVisual uiCartas;

    void Start()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
    }

    public void AsignarJugadores(List<MovimientoFicha> nuevosJugadores)
    {
        jugadores = new List<MovimientoFicha>(nuevosJugadores);
        turnoActual = 0;
    }

    public void IniciarTurno(MovimientoFicha jugador = null, DadoLogico dadoLogico = null, CartasUIVisual ui = null)
    {
        if (dadoLogico != null) dado = dadoLogico;
        if (ui != null) uiCartas = ui;
        
        if (dado == null)
            dado = FindAnyObjectByType<DadoLogico>();

        // ✅ Verificar que jugador NO sea null
        if (dado != null && jugador != null)
        {
            dado.gameObject.SetActive(true);
            dado.PrepararParaLanzar(jugador, OnDadoLanzado);
        }
        else if (jugador == null && jugadores.Count > 0)
        {
            // Si jugador es null, usar el actual de la lista
            if (dado != null)
            {
                dado.gameObject.SetActive(true);
                dado.PrepararParaLanzar(jugadores[turnoActual], OnDadoLanzado);
            }
        }
    }

    private void OnDadoLanzado(int resultado)
    {
        if (jugadores.Count > 0)
        {
            jugadores[turnoActual].Avanzar(resultado);
        }
    }

    public void OnMovimientoCompletado(MovimientoFicha jugador)
    {
        turnoActual = (turnoActual + 1) % jugadores.Count;
    }
}