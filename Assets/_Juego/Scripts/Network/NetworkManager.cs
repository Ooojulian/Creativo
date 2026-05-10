using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        Conectar();
    }

    public void Conectar()
    {
        if (PhotonNetwork.IsConnected) return;
        Debug.Log("[Red] Conectando a Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Red] Conectado al servidor Photon. Esperando accion del menu.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Red] En sala. Jugadores: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Red] Jugador entró: {newPlayer.NickName}. Total: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Red] Jugador salió: {otherPlayer.NickName}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Red] Desconectado: {cause}");
    }

    public bool SoyElHost => PhotonNetwork.IsMasterClient;
    public int CantidadJugadores => PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
}
