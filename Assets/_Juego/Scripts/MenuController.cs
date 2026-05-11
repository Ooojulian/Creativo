using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class MenuController : MonoBehaviourPunCallbacks
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelEspera;
    public GameObject fondoMenu;
    public GameObject panelUnirse;

    [Header("UI Espera")]
    public TextMeshProUGUI textoEspera;
    public TextMeshProUGUI textoCodigoSala;
    public Button botonIniciarPartida;

    [Header("UI Unirse")]
    public TMP_InputField inputCodigo;

    [Header("Configuración")]
    public int maxJugadores = 4;

    public static MenuController Instance;

    private const string LETRAS = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private string codigoPendiente = null;

    void Awake() { Instance = this; }

    public void CrearSala()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Menu] Photon no listo.");
            return;
        }
        string codigo = GenerarCodigo();
        PhotonNetwork.NickName = "Host";
        RoomOptions opciones = new RoomOptions { MaxPlayers = (byte)maxJugadores, IsVisible = false };
        PhotonNetwork.CreateRoom(codigo, opciones, TypedLobby.Default);
        MostrarEspera(codigo);
    }

    public void MostrarPanelUnirse()
    {
        if (panelMenu != null)   panelMenu.SetActive(false);
        if (fondoMenu != null)   fondoMenu.SetActive(false);
        if (panelUnirse != null) panelUnirse.SetActive(true);
    }

    public void ConfirmarUnirse()
    {
        if (inputCodigo == null || string.IsNullOrWhiteSpace(inputCodigo.text))
        {
            Debug.LogWarning("[Menu] Código vacío.");
            return;
        }
        codigoPendiente = inputCodigo.text.Trim().ToUpper();
        PhotonNetwork.NickName = "Jugador" + Random.Range(2, 99);
        if (panelUnirse != null) panelUnirse.SetActive(false);
        MostrarEspera(null);

        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(codigoPendiente);
            codigoPendiente = null;
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void CancelarUnirse()
    {
        if (panelUnirse != null) panelUnirse.SetActive(false);
        if (panelMenu != null)   panelMenu.SetActive(true);
        if (fondoMenu != null)   fondoMenu.SetActive(true);
    }

    public void IniciarPartida()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int cantidad = PhotonNetwork.CurrentRoom.PlayerCount;
        GameSync.Instance.IniciarPartida(cantidad);
    }

    private string GenerarCodigo()
    {
        char[] c = new char[4];
        for (int i = 0; i < 4; i++)
            c[i] = LETRAS[Random.Range(0, LETRAS.Length)];
        return new string(c);
    }

    private void MostrarEspera(string codigo)
    {
        if (panelMenu != null)   panelMenu.SetActive(false);
        if (fondoMenu != null)   fondoMenu.SetActive(false);
        if (panelEspera != null) panelEspera.SetActive(true);
        if (botonIniciarPartida != null)
            botonIniciarPartida.gameObject.SetActive(false);
        if (textoCodigoSala != null)
            textoCodigoSala.text = codigo != null ? $"Código: {codigo}" : "";
        if (textoEspera != null)
            textoEspera.text = "Conectando...";
    }

    private void ActualizarBotonIniciar()
    {
        if (botonIniciarPartida == null) return;
        int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        bool puedeIniciar = PhotonNetwork.IsMasterClient && count >= 2;
        botonIniciarPartida.gameObject.SetActive(puedeIniciar);
    }

    private void ActualizarTextoEspera()
    {
        if (textoEspera == null) return;
        int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        int max   = PhotonNetwork.CurrentRoom?.MaxPlayers  ?? maxJugadores;
        textoEspera.text = $"Jugadores en sala: {count}/{max}\nEsperando...";
    }

    // ── Callbacks Photon ─────────────────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        if (codigoPendiente != null)
        {
            PhotonNetwork.JoinRoom(codigoPendiente);
            codigoPendiente = null;
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("[Menu] Sala creada.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Menu] OnJoinedRoom jugadores:{PhotonNetwork.CurrentRoom?.PlayerCount} host:{PhotonNetwork.IsMasterClient}");
        ActualizarTextoEspera();
        ActualizarBotonIniciar();
    }

    public override void OnPlayerEnteredRoom(Player p) { ActualizarTextoEspera(); ActualizarBotonIniciar(); }
    public override void OnPlayerLeftRoom(Player p)    { ActualizarTextoEspera(); ActualizarBotonIniciar(); }

    public override void OnJoinRoomFailed(short code, string msg)
    {
        Debug.LogWarning($"[Menu] No se pudo unir: {msg}");
        if (panelEspera != null) panelEspera.SetActive(false);
        if (panelUnirse != null) panelUnirse.SetActive(true);
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogWarning($"[Menu] No se pudo crear sala: {msg}");
        if (panelEspera != null) panelEspera.SetActive(false);
        if (panelMenu != null)   panelMenu.SetActive(true);
        if (fondoMenu != null)   fondoMenu.SetActive(true);
    }
}
