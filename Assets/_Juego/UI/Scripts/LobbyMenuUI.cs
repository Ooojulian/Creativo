using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Versión UI Toolkit de MenuController.
/// Maneja el lobby Photon: crear sala, unirse, esperar, iniciar.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LobbyMenuUI : MonoBehaviourPunCallbacks
{
    public static LobbyMenuUI Instance;

    [Header("Configuración")]
    [SerializeField] private int maxJugadores = 4;

    private UIDocument    _doc;
    private VisualElement _root;

    // Paneles
    private VisualElement _panelMenu;
    private VisualElement _panelUnirse;
    private VisualElement _panelEspera;

    // Botones menú
    private Button _btnCrear;
    private Button _btnUnirsePanel;

    // Botones unirse
    private TextField _inputCodigo;
    private Button    _btnConfirmarUnirse;
    private Button    _btnCancelarUnirse;

    // Espera
    private Label         _lblCodigoSala;
    private Label         _lblEspera;
    private Button        _btnIniciar;
    private Button        _btnSalirSala;
    private VisualElement _playerDotsLobby;

    private const string LETRAS = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private string _codigoPendiente;

    void Awake()
    {
        Instance = this;
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        _root = _doc.rootVisualElement;

        _panelMenu   = _root.Q<VisualElement>("panel-menu");
        _panelUnirse = _root.Q<VisualElement>("panel-unirse");
        _panelEspera = _root.Q<VisualElement>("panel-espera");

        _btnCrear        = _root.Q<Button>("btn-crear");
        _btnUnirsePanel  = _root.Q<Button>("btn-unirse-panel");

        _inputCodigo        = _root.Q<TextField>("input-codigo");
        _btnConfirmarUnirse = _root.Q<Button>("btn-confirmar-unirse");
        _btnCancelarUnirse  = _root.Q<Button>("btn-cancelar-unirse");

        _lblCodigoSala   = _root.Q<Label>("lbl-codigo-sala");
        _lblEspera       = _root.Q<Label>("lbl-espera");
        _btnIniciar      = _root.Q<Button>("btn-iniciar");
        _btnSalirSala    = _root.Q<Button>("btn-salir-sala");
        _playerDotsLobby = _root.Q<VisualElement>("player-dots-lobby");

        _btnCrear?.RegisterCallback<ClickEvent>(_ => CrearSala());
        _btnUnirsePanel?.RegisterCallback<ClickEvent>(_ => MostrarPanelUnirse());
        _btnConfirmarUnirse?.RegisterCallback<ClickEvent>(_ => ConfirmarUnirse());
        _btnCancelarUnirse?.RegisterCallback<ClickEvent>(_ => CancelarUnirse());
        _btnIniciar?.RegisterCallback<ClickEvent>(_ => IniciarPartida());
        _btnSalirSala?.RegisterCallback<ClickEvent>(_ => SalirDeSala());

        MostrarPanel(_panelMenu);
    }

    void OnDisable()
    {
        _btnCrear?.UnregisterCallback<ClickEvent>(_ => CrearSala());
        _btnUnirsePanel?.UnregisterCallback<ClickEvent>(_ => MostrarPanelUnirse());
        _btnConfirmarUnirse?.UnregisterCallback<ClickEvent>(_ => ConfirmarUnirse());
        _btnCancelarUnirse?.UnregisterCallback<ClickEvent>(_ => CancelarUnirse());
        _btnIniciar?.UnregisterCallback<ClickEvent>(_ => IniciarPartida());
        _btnSalirSala?.UnregisterCallback<ClickEvent>(_ => SalirDeSala());
    }

    // ── Acciones ──────────────────────────────────────────────────────────────

    public void CrearSala()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Lobby] Photon no listo.");
            return;
        }
        string codigo = GenerarCodigo();
        PhotonNetwork.NickName = "Host";
        var opts = new RoomOptions { MaxPlayers = (byte)maxJugadores, IsVisible = false };
        PhotonNetwork.CreateRoom(codigo, opts, TypedLobby.Default);
        MostrarEspera(codigo);
    }

    private void MostrarPanelUnirse()
    {
        MostrarPanel(_panelUnirse);
    }

    private void ConfirmarUnirse()
    {
        if (_inputCodigo == null || string.IsNullOrWhiteSpace(_inputCodigo.text))
        {
            Debug.LogWarning("[Lobby] Código vacío.");
            return;
        }
        _codigoPendiente = _inputCodigo.text.Trim().ToUpper();
        PhotonNetwork.NickName = "Jugador" + Random.Range(2, 99);
        MostrarEspera(null);

        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(_codigoPendiente);
            _codigoPendiente = null;
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void CancelarUnirse()
    {
        MostrarPanel(_panelMenu);
    }

    public void IniciarPartida()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int cantidad = PhotonNetwork.CurrentRoom.PlayerCount;
        GameSync.Instance.IniciarPartida(cantidad);
    }

    private void SalirDeSala()
    {
        PhotonNetwork.LeaveRoom();
        MostrarPanel(_panelMenu);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void MostrarPanel(VisualElement objetivo)
    {
        _panelMenu?.Q<VisualElement>()?.RemoveFromHierarchy();  // no-op si null

        void Set(VisualElement el, bool visible)
        {
            if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        Set(_panelMenu,   objetivo == _panelMenu);
        Set(_panelUnirse, objetivo == _panelUnirse);
        Set(_panelEspera, objetivo == _panelEspera);
    }

    private void MostrarEspera(string codigo)
    {
        MostrarPanel(_panelEspera);
        if (_lblCodigoSala != null)
            _lblCodigoSala.text = codigo != null ? codigo : "----";
        if (_lblEspera != null)
            _lblEspera.text = "Conectando...";
        _btnIniciar?.RemoveFromClassList("display-none");
        if (_btnIniciar != null) _btnIniciar.style.display = DisplayStyle.None;
    }

    private void ActualizarEspera()
    {
        if (!PhotonNetwork.InRoom) return;
        int count = PhotonNetwork.CurrentRoom.PlayerCount;
        int max   = PhotonNetwork.CurrentRoom.MaxPlayers;

        if (_lblEspera != null)
            _lblEspera.text = $"Jugadores en sala: {count}/{max}\nEsperando...";

        bool puedeIniciar = PhotonNetwork.IsMasterClient && count >= 2;
        if (_btnIniciar != null)
            _btnIniciar.style.display = puedeIniciar ? DisplayStyle.Flex : DisplayStyle.None;

        // Puntos de jugadores
        if (_playerDotsLobby != null)
        {
            _playerDotsLobby.Clear();
            for (int i = 0; i < max; i++)
            {
                var dot = new VisualElement();
                dot.AddToClassList("player-dot");
                if (i < count) dot.AddToClassList("player-dot--active");
                _playerDotsLobby.Add(dot);
            }
        }
    }

    private string GenerarCodigo()
    {
        var c = new char[4];
        for (int i = 0; i < 4; i++)
            c[i] = LETRAS[Random.Range(0, LETRAS.Length)];
        return new string(c);
    }

    // ── Callbacks Photon ──────────────────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        if (_codigoPendiente != null)
        {
            PhotonNetwork.JoinRoom(_codigoPendiente);
            _codigoPendiente = null;
        }
    }

    public override void OnJoinedRoom()
    {
        ActualizarEspera();
    }

    public override void OnPlayerEnteredRoom(Player p) => ActualizarEspera();
    public override void OnPlayerLeftRoom(Player p)    => ActualizarEspera();

    public override void OnJoinRoomFailed(short code, string msg)
    {
        Debug.LogWarning($"[Lobby] No se pudo unir: {msg}");
        MostrarPanel(_panelUnirse);
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogWarning($"[Lobby] No se pudo crear sala: {msg}");
        MostrarPanel(_panelMenu);
    }
}
