using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event System.Action<MovimientoFicha> OnTurnStarted;
    public event System.Action<MovimientoFicha> OnTurnEnded;
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
    public GestorDeRutas ruta;

    public List<Transform> casillas => ruta != null ? ruta.casillas : null;

    [Header("Pantalla de Fin")]
    public GameObject panelFin;
    public TextMeshProUGUI textoFin;
    public TextMeshProUGUI textoGanadoresHUD;

    [Header("Selección de Ficha")]
    public SeleccionFichaUI seleccionFichaUI;

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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ruta == null)
            ruta = FindAnyObjectByType<GestorDeRutas>();


        dado.gameObject.SetActive(false);

        if (panelHUD != null)    panelHUD.SetActive(false);
        if (panelFin != null)    panelFin.SetActive(false);
        if (panelMenu != null)   panelMenu.SetActive(false);

        if (textoTurno != null)            textoTurno.text = "";
        if (textoResultadoDadoHUD != null) textoResultadoDadoHUD.text = "";
        if (textoGanadoresHUD != null)     textoGanadoresHUD.text = "";

        foreach (var jugador in todosLosJugadores)
            jugador.gameObject.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            // Viene del Lobby con sala activa
            int cantidad = PhotonNetwork.CurrentRoom.PlayerCount;
            if (camaraMenu != null)  camaraMenu.gameObject.SetActive(false);
            if (camaraJuego != null) camaraJuego.gameObject.SetActive(true);
            IniciarPartida(cantidad);
        }
        else
        {
            // Modo local: arrancar directamente con todos los jugadores asignados
            if (camaraMenu != null)  camaraMenu.gameObject.SetActive(false);
            if (camaraJuego != null) camaraJuego.gameObject.SetActive(true);
            IniciarPartida(todosLosJugadores.Count);
        }
    }

    public void IniciarPartida(int cantidad)
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        jugadoresActivos.Clear();
        ganadores.Clear();
        umbralVictoria = Mathf.CeilToInt(cantidad / 2f);

        if (panelFin != null) panelFin.SetActive(false);
        if (textoGanadoresHUD != null) textoGanadoresHUD.text = "";

        if (camaraMenu != null)  camaraMenu.gameObject.SetActive(false);
        if (camaraJuego != null) camaraJuego.gameObject.SetActive(true);
        if (panelHUD != null)    panelHUD.SetActive(true);

        // Teletransportar la cámara inmediatamente al tablero para evitar el suavizado desde la posición del menú
        CamaraDirectora cam = FindAnyObjectByType<CamaraDirectora>();
        if (cam != null) cam.SnapAlTablero();

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

            // Activar e inicializar ficha B (va de meta a inicio)
            if (jugador.fichaB != null)
            {
                jugador.fichaB.gameObject.SetActive(true);
                jugador.fichaB.Inicializar();
                if (ruta != null && ruta.casillas != null && ruta.casillas.Count > 0)
                {
                    Vector3 posMeta = ruta.casillas[ruta.casillas.Count - 1].position + Vector3.up * 0.5f;
                    jugador.fichaB.transform.position = posMeta + offsetsInicio[i];
                }
            }
        }

        turnoActual = 0;
        PrepararTurno();
    }

    public void SiguienteTurno()
    {
        if (turnoActual < jugadoresActivos.Count)
        {
            MovimientoFicha j = jugadoresActivos[turnoActual];
            // Decrementar estados del jugador que acaba de jugar
            j.GetComponent<EstadosJugador>()?.DecrementarTodosLosEstados();
            // Trigger "Inspiración" al final del turno
            CardTriggerSystem.Instance?.CheckTurnEnd(j, j.cartasAlEmpezarTurno);
        }

        if (OnTurnEnded != null)
            OnTurnEnded.Invoke(jugadoresActivos[turnoActual]);

        turnoActual++;
        if (turnoActual >= jugadoresActivos.Count)
            turnoActual = 0;
        PrepararTurno();
    }

    public void PrepararTurno()
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

        // NUEVO: Guardar cantidad de cartas para trigger "Inspiración"
        if (j.inventario != null)
            j.cartasAlEmpezarTurno = j.inventario.hand.Count;

        // NUEVO: Trigger "Desvío" al inicio del turno
        if (CardTriggerSystem.Instance != null)
            CardTriggerSystem.Instance.CheckTurnStart(j);

        // Silencio se limpia al empezar el turno (EstadosJugador lo decrementa al final del anterior)
        j.silencioActivo = false;

        // CARTA: PierdeTurno (saltar turno)
        if (j.pierdeSiguienteTurno)
        {
            j.pierdeSiguienteTurno = false;
            Debug.Log($"[Cartas] Jugador {turnoActual + 1} pierde el turno.");
            SiguienteTurno();
            return;
        }

        dado.jugador = j;

        if (dado.gameObject.activeSelf)
            dado.gameObject.SetActive(false);

        if (textoTurno != null)
            textoTurno.text = $"Turno: Jugador {turnoActual + 1}";

        // Sin red: activar dado directo
        if (GameSync.Instance == null || !PhotonNetwork.InRoom)
        {
            dado.gameObject.SetActive(true);
            DispararOnTurnStarted(j);
            return;
        }

        // Con red: host avisa quién juega, incluye lógica de panel selección ficha
        if (PhotonNetwork.IsMasterClient)
            GameSync.Instance.AnunciarTurnoConPanel(turnoActual);
    }

    public void DispararOnTurnStarted(MovimientoFicha j)
    {
        if (OnTurnStarted != null)
            OnTurnStarted.Invoke(j);
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

    // =========================
    // CARTA: INTERCAMBIO
    // =========================
    public void IntercambiarConOtroJugador(MovimientoFicha jugador, int randRivalIdx = -1)
    {
        // Elegir otro jugador activo (distinto al actual)
        MovimientoFicha otro = null;
        if (randRivalIdx >= 0 && randRivalIdx < todosLosJugadores.Count)
        {
            otro = todosLosJugadores[randRivalIdx];
        }
        else
        {
            var candidatos = new List<MovimientoFicha>(jugadoresActivos);
            candidatos.Remove(jugador);
            if (candidatos.Count == 0) return;
            otro = candidatos[Random.Range(0, candidatos.Count)];
        }

        if (otro == null) return;

        if (otro.escudoActivo)
        {
            Debug.Log($"[Escudo] {otro.name} bloqueó el intercambio.");
            otro.escudoActivo = false;
            return;
        }

        // Intercambiar índices
        int a = jugador.indiceActual;
        int b = otro.indiceActual;

        jugador.indiceActual = b;
        otro.indiceActual = a;

        // Reposicionar en el tablero
        if (ruta != null && ruta.casillas != null && ruta.casillas.Count > 0)
        {
            jugador.transform.position = ruta.casillas[jugador.indiceActual].position + Vector3.up * 0.5f;
            otro.transform.position = ruta.casillas[otro.indiceActual].position + Vector3.up * 0.5f;

            if (GameSync.Instance != null)
            {
                int idxJugador = todosLosJugadores.IndexOf(jugador);
                int idxOtro = todosLosJugadores.IndexOf(otro);
                GameSync.Instance.SincronizarPosicionFicha(idxJugador, jugador.indiceActual);
                GameSync.Instance.SincronizarPosicionFicha(idxOtro, otro.indiceActual);
            }
        }

        Debug.Log($"[Cartas] Intercambio: {jugador.name} <-> {otro.name}");
    }

    // Detecta si hay ficha enemiga en la misma casilla. Devuelve datos para batalla.
    // Solo el host debe llamarlo y luego invocar BatallaPPS.IniciarBatalla.
    public bool DetectarColision(MovimientoFicha atacanteA, FichaInversa atacanteB,
        out int idxFichaDef, out bool esFichaBDef, out int actorDef)
    {
        idxFichaDef = -1;
        esFichaBDef = false;
        actorDef = -1;

        int casillaAtk;
        int idxJugadorAtk;
        if (atacanteA != null)
        {
            casillaAtk = atacanteA.indiceActual;
            idxJugadorAtk = todosLosJugadores.IndexOf(atacanteA);
        }
        else
        {
            casillaAtk = atacanteB.indiceActual;
            idxJugadorAtk = -1;
            for (int k = 0; k < todosLosJugadores.Count; k++)
                if (todosLosJugadores[k].fichaB == atacanteB) { idxJugadorAtk = k; break; }
        }

        for (int i = 0; i < jugadoresActivos.Count; i++)
        {
            var otro = jugadoresActivos[i];
            int idxOtro = todosLosJugadores.IndexOf(otro);
            if (idxOtro == idxJugadorAtk) continue;

            if (otro.indiceActual == casillaAtk && otro.gameObject.activeSelf)
            {
                idxFichaDef = idxOtro;
                esFichaBDef = false;
                actorDef = idxOtro < Photon.Pun.PhotonNetwork.PlayerList.Length
                    ? Photon.Pun.PhotonNetwork.PlayerList[idxOtro].ActorNumber : -1;
                return true;
            }
            if (otro.fichaB != null && otro.fichaB.indiceActual == casillaAtk && otro.fichaB.gameObject.activeSelf)
            {
                idxFichaDef = idxOtro;
                esFichaBDef = true;
                actorDef = idxOtro < Photon.Pun.PhotonNetwork.PlayerList.Length
                    ? Photon.Pun.PhotonNetwork.PlayerList[idxOtro].ActorNumber : -1;
                return true;
            }
        }
        return false;
    }

    public void VolverAlMenu()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        SceneManager.LoadScene("MenuInicio");
    }

    // ── Accesores para sistemas externos ──────────────────────────────────────

    public CartasUIVisual ObtenerUICartas() => uiCartas;

    public List<MovimientoFicha> ObtenerJugadoresActivos() => new List<MovimientoFicha>(jugadoresActivos);
}