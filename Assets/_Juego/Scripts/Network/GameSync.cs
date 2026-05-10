using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Sincroniza turnos y movimiento de fichas entre clientes.
// El Host es la fuente de verdad: decide el turno y envia RPCs a todos.
public class GameSync : MonoBehaviourPunCallbacks
{
    public static GameSync Instance;

    [Header("Referencias")]
    public GameManager gameManager;

    // Actor number del jugador cuyo turno es ahora (asignado por el host)
    private int actorNumeroTurnoActual = -1;

    void Awake()
    {
        Instance = this;
    }

    // ─── TURNO ───────────────────────────────────────────────────────────────

    // El host llama esto para avisar a todos quién juega y mostrar el panel correcto
    public void AnunciarTurnoConPanel(int indiceTurno)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // Mapear índice de turno a actor number: el jugador en posición indiceTurno
        var jugadores = PhotonNetwork.PlayerList;
        int actorNumber = indiceTurno < jugadores.Length ? jugadores[indiceTurno].ActorNumber : 1;
        photonView.RPC(nameof(RPC_RecibirTurnoConPanel), RpcTarget.All, actorNumber, indiceTurno);
    }

    [PunRPC]
    private void RPC_RecibirTurnoConPanel(int actorNumber, int indiceTurno)
    {
        actorNumeroTurnoActual = actorNumber;
        bool esMiTurno = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[Red] Turno actor {actorNumber}. ¿Es mi turno? {esMiTurno}");

        if (gameManager == null) return;

        var jugador = gameManager.todosLosJugadores.Count > indiceTurno
            ? gameManager.todosLosJugadores[indiceTurno] : null;
        if (jugador == null) return;

        var ui = FindAnyObjectByType<SeleccionFichaUI>();
        if (esMiTurno)
        {
            if (ui != null) ui.MostrarSeleccion(jugador);
            else if (gameManager.dado != null) gameManager.dado.gameObject.SetActive(true);
        }
        else
        {
            if (ui != null) ui.OcultarPanel();
        }
    }

    public void AnunciarTurno(int indiceTurno)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var jugadores = PhotonNetwork.PlayerList;
        int actorNumber = indiceTurno < jugadores.Length ? jugadores[indiceTurno].ActorNumber : 1;
        photonView.RPC(nameof(RPC_RecibirTurno), RpcTarget.All, actorNumber, indiceTurno);
    }

    [PunRPC]
    private void RPC_RecibirTurno(int actorNumber, int indiceTurno)
    {
        actorNumeroTurnoActual = actorNumber;
        bool esMiTurno = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[Red] RPC_RecibirTurno actor={actorNumber} idx={indiceTurno} miActor={PhotonNetwork.LocalPlayer.ActorNumber} miTurno={esMiTurno}");

        if (gameManager == null) { Debug.LogError("[Red] gameManager null"); return; }
        if (gameManager.dado == null) { Debug.LogError("[Red] dado null"); return; }

        // Asignar jugador al dado en todos los clientes
        if (indiceTurno < gameManager.todosLosJugadores.Count)
        {
            gameManager.dado.jugador = gameManager.todosLosJugadores[indiceTurno];
            Debug.Log($"[Red] dado.jugador = {gameManager.dado.jugador.name}");
        }

        // Resetear estado de eleccion ficha al inicio del turno
        gameManager.dado.jugador.moverFichaB = false;

        // Solo cliente con turno ve el dado
        gameManager.dado.gameObject.SetActive(esMiTurno);
        Debug.Log($"[Red] dado.activeSelf despues SetActive: {gameManager.dado.gameObject.activeSelf}");
    }

    public bool EsMiTurno => actorNumeroTurnoActual == PhotonNetwork.LocalPlayer.ActorNumber;

    // ─── DADO ─────────────────────────────────────────────────────────────────

    // Mostrar resultado dado en otros clientes (sin mover todavía)
    public void SincronizarResultadoDado(int resultado)
    {
        photonView.RPC(nameof(RPC_MostrarResultado), RpcTarget.Others, resultado);
    }

    [PunRPC]
    private void RPC_MostrarResultado(int resultado)
    {
        if (gameManager != null && gameManager.textoResultadoDadoHUD != null)
        {
            gameManager.textoResultadoDadoHUD.text = $"Dado: {resultado}";
            gameManager.textoResultadoDadoHUD.gameObject.SetActive(true);
        }
    }

    // Llamado por el jugador local cuando tira el dado
    public void EnviarResultadoDado(int resultado, int indiceFicha, bool esFichaB)
    {
        photonView.RPC(nameof(RPC_RecibirMovimiento), RpcTarget.All, resultado, indiceFicha, esFichaB);
    }

    [PunRPC]
    private void RPC_RecibirMovimiento(int pasos, int indiceFicha, bool esFichaB)
    {
        Debug.Log($"[Red] RPC_RecibirMovimiento ficha={indiceFicha} pasos={pasos} fichaB={esFichaB} master={PhotonNetwork.IsMasterClient}");
        if (gameManager == null) { Debug.LogError("[Red] gameManager null en GameSync"); return; }
        if (gameManager.todosLosJugadores == null) { Debug.LogError("[Red] todosLosJugadores null"); return; }
        if (indiceFicha < 0 || indiceFicha >= gameManager.todosLosJugadores.Count)
        {
            Debug.LogError($"[Red] indiceFicha {indiceFicha} fuera de rango ({gameManager.todosLosJugadores.Count})");
            return;
        }

        MovimientoFicha ficha = gameManager.todosLosJugadores[indiceFicha];
        if (ficha == null) { Debug.LogError("[Red] ficha null en lista"); return; }
        if (!ficha.gameObject.activeSelf)
        {
            Debug.LogWarning($"[Red] ficha {indiceFicha} inactiva al recibir movimiento - activando");
            ficha.gameObject.SetActive(true);
        }
        ficha.ElegirFicha(esFichaB);
        ficha.Avanzar(pasos);
    }

    // ─── FIN DE TURNO ────────────────────────────────────────────────────────

    // Llamado por MovimientoFicha al terminar de moverse (solo el host decide siguiente turno)
    public void NotificarFinDeMovimiento()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        gameManager.SiguienteTurno();
    }

    // ─── SALA ────────────────────────────────────────────────────────────────

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Red] {newPlayer.NickName} entró. Jugadores: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Red] {otherPlayer.NickName} salió.");
        // Si era el host, Photon transfiere el MasterClient automáticamente
    }
}
