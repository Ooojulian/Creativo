using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Interfaz")]
    public GameObject panelMenu;

    [Header("HUD en juego")]
    public GameObject panelHUD;
    public TextMeshProUGUI textoTurno;

    [Header("Cámaras")]
    public Camera camaraMenu;
    public Camera camaraJuego;

    [Header("Elementos del Juego")]
    public DadoLogico dado;
    public List<MovimientoFicha> todosLosJugadores;
    public GestorDeRuta ruta;

    [Header("Pantalla de Fin")]
    public GameObject panelFin;
    public TextMeshProUGUI textoFin;
    public TextMeshProUGUI textoGanadoresHUD;

    // Separación entre fichas en la casilla de inicio
    private static readonly Vector3[] offsetsInicio = {
        new Vector3(-20f, 0f,  20f),
        new Vector3( 20f, 0f,  20f),
        new Vector3(-20f, 0f, -20f),
        new Vector3( 20f, 0f, -20f),
    };

    private List<MovimientoFicha> jugadoresActivos = new List<MovimientoFicha>();
    private List<MovimientoFicha> ganadores = new List<MovimientoFicha>();
    private int turnoActual = 0;
    private int umbralVictoria = 1;

    void Start()
    {
        // Fallback: buscar ruta automáticamente si no está asignada
        if (ruta == null)
            ruta = FindAnyObjectByType<GestorDeRuta>();

        panelMenu.SetActive(true);
        dado.gameObject.SetActive(false);

        if (panelHUD != null)    panelHUD.SetActive(false);
        if (panelFin != null)    panelFin.SetActive(false);
        if (camaraJuego != null) camaraJuego.gameObject.SetActive(false);
        if (camaraMenu != null)  camaraMenu.gameObject.SetActive(true);

        foreach (var jugador in todosLosJugadores)
            jugador.gameObject.SetActive(false);
    }

    public void IniciarPartida(int cantidad)
    {
        panelMenu.SetActive(false);
        dado.gameObject.SetActive(true);
        jugadoresActivos.Clear();
        ganadores.Clear();
        umbralVictoria = Mathf.CeilToInt(cantidad / 2f);

        if (panelFin != null) panelFin.SetActive(false);
        if (textoGanadoresHUD != null) textoGanadoresHUD.text = "";

        if (camaraMenu != null)  camaraMenu.gameObject.SetActive(false);
        if (camaraJuego != null) camaraJuego.gameObject.SetActive(true);
        if (panelHUD != null)    panelHUD.SetActive(true);

        // Posicionar jugadores en la casilla Start
        Vector3 posInicio;
        if (ruta != null && ruta.casillas.Count > 0)
            posInicio = ruta.casillas[0].position + Vector3.up * 0.5f;
        else
            posInicio = Vector3.zero;

        for (int i = 0; i < cantidad; i++)
        {
            MovimientoFicha jugador = todosLosJugadores[i];
            jugador.indiceActual = 0;
            jugador.transform.position = posInicio + offsetsInicio[i];
            jugador.gameObject.SetActive(true);
            jugadoresActivos.Add(jugador);
        }

        Debug.Log($"Partida iniciada con {cantidad} jugadores. Posición inicio: {posInicio}");

        turnoActual = 0;
        PrepararTurno();
    }

    public void SiguienteTurno()
    {
        turnoActual++;
        if (turnoActual >= jugadoresActivos.Count)
            turnoActual = 0;
        PrepararTurno();
    }

    private void PrepararTurno()
    {
        // Saltar jugadores que ya llegaron a la meta
        int intentos = 0;
        while (ganadores.Contains(jugadoresActivos[turnoActual]) && intentos < jugadoresActivos.Count)
        {
            turnoActual = (turnoActual + 1) % jugadoresActivos.Count;
            intentos++;
        }

        Debug.Log($"Turno del Jugador {turnoActual + 1}");
        dado.jugador = jugadoresActivos[turnoActual];

        if (!dado.gameObject.activeSelf)
            dado.gameObject.SetActive(true);

        if (textoTurno != null)
            textoTurno.text = $"Turno: Jugador {turnoActual + 1}";
    }

    public void LlegarAMeta(MovimientoFicha jugador)
    {
        if (ganadores.Contains(jugador)) return;
        ganadores.Add(jugador);

        int numJugador = jugadoresActivos.IndexOf(jugador) + 1;
        Debug.Log($"[GameManager] Jugador {numJugador} llegó a la meta! ({ganadores.Count}/{umbralVictoria})");

        // Actualizar indicador visual en el HUD
        if (textoGanadoresHUD != null)
        {
            string lista = "En meta: ";
            foreach (var g in ganadores)
                lista += $"J{jugadoresActivos.IndexOf(g) + 1}  ";
            textoGanadoresHUD.text = lista.TrimEnd();
        }

        if (ganadores.Count >= umbralVictoria)
            MostrarPantallaFin();
        else
        {
            turnoActual = (turnoActual + 1) % jugadoresActivos.Count;
            PrepararTurno();
        }
    }

    private void MostrarPantallaFin()
    {
        if (dado != null) dado.gameObject.SetActive(false);
        if (panelHUD != null) panelHUD.SetActive(false);

        if (panelFin != null)
        {
            panelFin.SetActive(true);

            if (textoFin != null)
            {
                string texto = "¡Fin del juego!\n\nGanadores:\n";
                foreach (var g in ganadores)
                {
                    int num = jugadoresActivos.IndexOf(g) + 1;
                    texto += $"Jugador {num}\n";
                }
                textoFin.text = texto;
            }
        }
    }

    public void VolverAlMenu()
    {
        ganadores.Clear();
        jugadoresActivos.Clear();
        turnoActual = 0;

        foreach (var jugador in todosLosJugadores)
            jugador.gameObject.SetActive(false);

        if (panelFin != null)    panelFin.SetActive(false);
        if (panelHUD != null)    panelHUD.SetActive(false);
        if (camaraJuego != null) camaraJuego.gameObject.SetActive(false);
        if (camaraMenu != null)  camaraMenu.gameObject.SetActive(true);
        if (panelMenu != null)   panelMenu.SetActive(true);
    }
}
