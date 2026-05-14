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

    [Header("Cartas UI")]
    public CartasUIVisual uiCartas;

    [Header("Cámaras")]
    public Camera camaraMenu;
    public Camera camaraJuego;

    [Header("Elementos del Juego")]
    public DadoLogico dado;
    public List<MovimientoFicha> todosLosJugadores;
    public List<Transform> casillas; // Reemplaza GestorDeRuta

    [Header("Sistemas")]
    public SistemaCartas sistemaCartas;
    public GestorTurnos gestorTurnos;

    [Header("Pantalla de Fin")]
    public GameObject panelFin;
    public TextMeshProUGUI textoFin;
    public TextMeshProUGUI textoGanadoresHUD;

    [Header("HUD persistente")]
    public TextMeshProUGUI textoResultadoDadoHUD;

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

    public MovimientoFicha JugadorActual => jugadoresActivos.Count > 0 ? jugadoresActivos[turnoActual] : null;

    void Start()
    {
        if (gestorTurnos == null)
            gestorTurnos = FindAnyObjectByType<GestorTurnos>();

        if (sistemaCartas == null)
            sistemaCartas = Resources.Load<SistemaCartas>("SistemaCartas");

        panelMenu.SetActive(true);
        dado.gameObject.SetActive(false);

        if (panelHUD != null)    panelHUD.SetActive(false);
        if (panelFin != null)    panelFin.SetActive(false);
        if (camaraJuego != null) camaraJuego.gameObject.SetActive(false);
        if (camaraMenu != null)  camaraMenu.gameObject.SetActive(true);

        if (textoTurno != null)            textoTurno.text = "";
        if (textoResultadoDadoHUD != null) textoResultadoDadoHUD.text = "";
        if (textoGanadoresHUD != null)     textoGanadoresHUD.text = "";

        foreach (var jugador in todosLosJugadores)
            jugador.gameObject.SetActive(false);
    }

    public void IniciarPartida(int cantidad)
    {
        Debug.Log($"[GameManager] IniciarPartida llamado con {cantidad} jugadores"); // ✅ LOG INICIAL
        
        panelMenu.SetActive(false);
        jugadoresActivos.Clear();
        ganadores.Clear();
        umbralVictoria = Mathf.CeilToInt(cantidad / 2f);

        if (panelFin != null) panelFin.SetActive(false);
        if (textoGanadoresHUD != null) textoGanadoresHUD.text = "";

        if (camaraMenu != null)  camaraMenu.gameObject.SetActive(false);
        if (camaraJuego != null) camaraJuego.gameObject.SetActive(true);
        if (panelHUD != null)    panelHUD.SetActive(true);

        // Teletransportar la cámara inmediatamente al tablero
        CamaraDirectora cam = FindAnyObjectByType<CamaraDirectora>();
        if (cam != null) cam.SnapAlTablero();

        Debug.Log($"[GameManager] casillas count: {(casillas != null ? casillas.Count : "NULL")}");
        Debug.Log($"[GameManager] todosLosJugadores count: {(todosLosJugadores != null ? todosLosJugadores.Count : "NULL")}");

        // Posicionar jugadores en la casilla Start
        Vector3 posInicio;
        if (casillas != null && casillas.Count > 0)
            posInicio = casillas[0].position + Vector3.up * 0.5f;
        else
        {
            Debug.LogError("[GameManager] NO HAY CASILLAS EN LA ESCENA!");
            posInicio = Vector3.zero;
        }

        if (todosLosJugadores == null || todosLosJugadores.Count == 0)
        {
            Debug.LogError("[GameManager] NO HAY JUGADORES EN todosLosJugadores!");
            return;
        }

        for (int i = 0; i < cantidad; i++)
        {
            if (i >= todosLosJugadores.Count)
            {
                Debug.LogError($"[GameManager] Índice {i} fuera de rango en todosLosJugadores");
                break;
            }

            MovimientoFicha jugador = todosLosJugadores[i];
            jugador.indiceActual = 0;
            jugador.transform.position = posInicio + offsetsInicio[i];
            jugador.gameObject.SetActive(true);
            jugadoresActivos.Add(jugador);
            Debug.Log($"[GameManager] Jugador {i+1} posicionado en {jugador.transform.position}");
        }

        if (gestorTurnos == null)
        {
            gestorTurnos = FindAnyObjectByType<GestorTurnos>();
            if (gestorTurnos == null)
            {
                Debug.LogError("[GameManager] GestorTurnos no encontrado en la escena!");
                return;
            }
        }   
        Debug.Log($"[GameManager] Partida iniciada con {cantidad} jugadores. Posición inicio: {posInicio}");

        // ✅ ASIGNAR JUGADORES A GESTOR TURNOS
        if (gestorTurnos != null)
            gestorTurnos.AsignarJugadores(jugadoresActivos);

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

        // Jugador actual
        MovimientoFicha j = jugadoresActivos[turnoActual];

        Debug.Log($"Turno del Jugador {turnoActual + 1}");

        if (textoTurno != null)
            textoTurno.text = $"Turno: Jugador {turnoActual + 1}";
        
        // Usar GestorTurnos para manejar el turno
        if (gestorTurnos != null)
        {
            gestorTurnos.IniciarTurno();
        }
        else
        {
            Debug.LogError("[GameManager] GestorTurnos no asignado");
        }
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

    // ────────────────────────────────────────
    // MÉTODOS PÚBLICOS
    // ────────────────────────────────────────

    public SistemaCartas ObtenerSistemaCartas() => sistemaCartas;
    
    public CartasUIVisual ObtenerUICartas() => uiCartas;
    
    public List<MovimientoFicha> ObtenerJugadoresActivos() => new List<MovimientoFicha>(jugadoresActivos);

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