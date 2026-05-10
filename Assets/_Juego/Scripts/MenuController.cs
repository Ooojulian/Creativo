using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class MenuController : MonoBehaviourPun, IInRoomCallbacks, IMatchmakingCallbacks
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelEspera;
    public GameObject fondoMenu;

    [Header("UI Espera")]
    public TextMeshProUGUI textoEspera;
    public Button botonIniciarPartida;

    [Header("Configuración")]
    public int maxJugadores = 4;

    void OnEnable()  { PhotonNetwork.AddCallbackTarget(this); }
    void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

    public void CrearSala()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Menu] Photon no listo. Reintentando conexion...");
            PhotonNetwork.ConnectUsingSettings();
            return;
        }
        PhotonNetwork.NickName = "Host";
        RoomOptions opciones = new RoomOptions { MaxPlayers = (byte)maxJugadores };
        PhotonNetwork.CreateRoom("SalaCreativo", opciones, TypedLobby.Default);
        MostrarEspera();
    }

    public void UnirseSala()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Menu] Photon no listo. Reintentando conexion...");
            PhotonNetwork.ConnectUsingSettings();
            return;
        }
        PhotonNetwork.NickName = "Jugador" + Random.Range(2, 99);
        PhotonNetwork.JoinRoom("SalaCreativo");
        MostrarEspera();
    }

    private void MostrarEspera()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (fondoMenu != null) fondoMenu.SetActive(false);
        if (panelEspera != null) panelEspera.SetActive(true);
        ActualizarTextoEspera();
    }

    private void ActualizarTextoEspera()
    {
        if (textoEspera == null) return;
        int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        textoEspera.text = $"Jugadores en sala: {count}/{maxJugadores}\nEsperando...";
    }

    public void IniciarPartida()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int cantidad = PhotonNetwork.CurrentRoom.PlayerCount;
        photonView.RPC(nameof(RPC_IniciarPartida), RpcTarget.All, cantidad);
    }

    [PunRPC]
    private void RPC_IniciarPartida(int cantidad)
    {
        if (panelEspera != null) panelEspera.SetActive(false);
        FindAnyObjectByType<GameManager>().IniciarPartida(cantidad);
    }

    // ── IInRoomCallbacks ──────────────────────────────────────────────────────
    public void OnJoinedRoom()
    {
        ActualizarTextoEspera();
        if (botonIniciarPartida != null)
            botonIniciarPartida.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }
    public void OnPlayerEnteredRoom(Player p) { ActualizarTextoEspera(); }
    public void OnPlayerLeftRoom(Player p)    { ActualizarTextoEspera(); }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable props) { }
    public void OnPlayerPropertiesUpdate(Player p, ExitGames.Client.Photon.Hashtable props) { }
    public void OnMasterClientSwitched(Player p) { }

    // ── IMatchmakingCallbacks ─────────────────────────────────────────────────
    public void OnJoinRoomFailed(short code, string msg)
    {
        Debug.LogWarning($"[Menu] No se pudo unir: {msg}");
        if (panelEspera != null) panelEspera.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(true);
        if (fondoMenu != null) fondoMenu.SetActive(true);
    }
    public void OnCreatedRoom() { Debug.Log("[Menu] Sala creada."); }
    public void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogWarning($"[Menu] No se pudo crear sala: {msg}");
        if (panelEspera != null) panelEspera.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(true);
        if (fondoMenu != null) fondoMenu.SetActive(true);
    }
    public void OnLeftRoom() { }
    public void OnJoinedLobby() { }
    public void OnLeftLobby() { }
    public void OnJoinRandomFailed(short code, string msg) { }
    public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> list) { }
}
