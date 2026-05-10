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

    // El host llama esto para avisar a todos de quién es el turno
    public void AnunciarTurno(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_RecibirTurno), RpcTarget.All, actorNumber);
    }

    [PunRPC]
    private void RPC_RecibirTurno(int actorNumber)
    {
        actorNumeroTurnoActual = actorNumber;
        bool esMiTurno = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[Red] Turno del actor {actorNumber}. ¿Es mi turno? {esMiTurno}");

        // Activar/desactivar dado según si es tu turno
        if (gameManager != null && gameManager.dado != null)
            gameManager.dado.gameObject.SetActive(esMiTurno);
    }

    public bool EsMiTurno => actorNumeroTurnoActual == PhotonNetwork.LocalPlayer.ActorNumber;

    // ─── DADO ─────────────────────────────────────────────────────────────────

    // Llamado por el jugador local cuando tira el dado
    public void EnviarResultadoDado(int resultado, int indiceFicha)
    {
        photonView.RPC(nameof(RPC_RecibirMovimiento), RpcTarget.All, resultado, indiceFicha);
    }

    [PunRPC]
    private void RPC_RecibirMovimiento(int pasos, int indiceFicha)
    {
        Debug.Log($"[Red] Mover ficha {indiceFicha} → {pasos} pasos");
        if (gameManager == null || gameManager.todosLosJugadores == null) return;
        if (indiceFicha < 0 || indiceFicha >= gameManager.todosLosJugadores.Count) return;

        MovimientoFicha ficha = gameManager.todosLosJugadores[indiceFicha];
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
