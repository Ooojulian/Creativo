using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Interfaz")]
    public GameObject panelMenu;

    [Header("Elementos del Juego")]
    public DadoLogico dado;
    public List<MovimientoFicha> todosLosJugadores;

    private List<MovimientoFicha> jugadoresActivos = new List<MovimientoFicha>();
    private int turnoActual = 0;

    void Start()
    {
        panelMenu.SetActive(true);
        dado.gameObject.SetActive(false);

        foreach (var jugador in todosLosJugadores)
        {
            jugador.gameObject.SetActive(false);
        }
    }

    public void IniciarPartida(int cantidad)
    {
        panelMenu.SetActive(false);
        dado.gameObject.SetActive(true);

        for (int i = 0; i < cantidad; i++)
        {
            todosLosJugadores[i].gameObject.SetActive(true);
            jugadoresActivos.Add(todosLosJugadores[i]);
        }

        Debug.Log("Partida iniciada con " + cantidad + " jugadores.");

        turnoActual = 0;
        PrepararTurno();
    }

    public void SiguienteTurno()
    {
        turnoActual++;

        if (turnoActual >= jugadoresActivos.Count)
        {
            turnoActual = 0;
        }

        PrepararTurno();
    }

    private void PrepararTurno()
    {
        Debug.Log("Es el turno del Jugador: " + (turnoActual + 1));
        dado.jugador = jugadoresActivos[turnoActual];
    }
}
