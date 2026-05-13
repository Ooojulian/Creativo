using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// UI Toolkit del lobby Photon: crear sala, unirse, esperar, iniciar.
/// Reemplaza la capa UI de MenuController (la lógica Photon queda aquí).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LobbyMenuUI : MonoBehaviourPunCallbacks
{
    public static LobbyMenuUI Instance;

    [Header("Configuración")]
    [SerializeField] private int maxJugadores = 4;
    [SerializeField] private string escenaMenuInicio    = "MenuInicio";
    [SerializeField] private string escenaSeleccion     = "SeleccionPersonajes";

    private UIDocument    _doc;
    private VisualElement _root;

    // Paneles
    private VisualElement _panelMenu;
    private VisualElement _panelUnirse;
    private VisualElement _panelEspera;

    // Botones menú
    private Button _btnCrear;
    private Button _btnUnirsePanel;
    private Button _btnVolver;

    // Botones unirse
    private TextField _inputCodigo;
    private Button    _btnConfirmarUnirse;
    private Button    _btnCancelarUnirse;
    private Label     _lblErrorUnirse;

    // Espera
    private Label         _lblCodigoSala;
    private Label         _lblContador;
    private VisualElement _listaJugadores;
    private Button        _btnIniciar;
    private Button        _btnAbandonar;
    private Label         _lblAvisoMinimo;

    private const string LETRAS = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private string _codigoPendiente;
    private string _accionPendiente;   // "crear" | "unirse"

    void Awake()
    {
        Instance = this;
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.AddCallbackTarget(this);

        _root = _doc.rootVisualElement;
        Debug.Log($"[Lobby] OnEnable. Root null: {_root == null}");

        _panelMenu   = _root.Q<VisualElement>("panel-menu");
        _panelUnirse = _root.Q<VisualElement>("panel-unirse");
        _panelEspera = _root.Q<VisualElement>("panel-espera");

        _btnCrear        = _root.Q<Button>("btn-crear");
        _btnUnirsePanel  = _root.Q<Button>("btn-unirse-panel");
        _btnVolver       = _root.Q<Button>("btn-volver");

        _inputCodigo        = _root.Q<TextField>("input-codigo");
        _btnConfirmarUnirse = _root.Q<Button>("btn-confirmar-unirse");
        _btnCancelarUnirse  = _root.Q<Button>("btn-cancelar-unirse");
        _lblErrorUnirse     = _root.Q<Label>("lbl-error-unirse");

        _lblCodigoSala  = _root.Q<Label>("lbl-codigo-sala");
        _lblContador    = _root.Q<Label>("lbl-contador-jugadores");
        _listaJugadores = _root.Q<VisualElement>("lista-jugadores");
        _btnIniciar     = _root.Q<Button>("btn-iniciar-partida");
        _btnAbandonar   = _root.Q<Button>("btn-abandonar-sala");
        _lblAvisoMinimo = _root.Q<Label>("lbl-aviso-minimo");

        Debug.Log($"[Lobby] btn-crear null: {_btnCrear == null}");
        Debug.Log($"[Lobby] btn-unirse-panel null: {_btnUnirsePanel == null}");
        Debug.Log($"[Lobby] btn-volver null: {_btnVolver == null}");
        Debug.Log($"[Lobby] btn-confirmar-unirse null: {_btnConfirmarUnirse == null}");
        Debug.Log($"[Lobby] btn-cancelar-unirse null: {_btnCancelarUnirse == null}");
        Debug.Log($"[Lobby] btn-iniciar-partida null: {_btnIniciar == null}");
        Debug.Log($"[Lobby] btn-abandonar-sala null: {_btnAbandonar == null}");

        // Filtro automático del input: solo A-Z, máx 4 caracteres, mayúsculas
        if (_inputCodigo != null)
        {
            _inputCodigo.maxLength = 4;
            _inputCodigo.RegisterValueChangedCallback(evt =>
            {
                string filtrado = new string(
                    evt.newValue.ToUpper().Where(c => c >= 'A' && c <= 'Z').ToArray()
                );
                if (filtrado != evt.newValue)
                    _inputCodigo.SetValueWithoutNotify(filtrado);
            });
        }

        if (_btnCrear        != null) _btnCrear.clicked        += CrearSala;
        if (_btnUnirsePanel  != null) _btnUnirsePanel.clicked  += MostrarPanelUnirse;
        if (_btnVolver       != null) _btnVolver.clicked       += VolverAlMenu;
        if (_btnConfirmarUnirse != null) _btnConfirmarUnirse.clicked += ConfirmarUnirse;
        if (_btnCancelarUnirse  != null) _btnCancelarUnirse.clicked  += CancelarUnirse;
        if (_btnIniciar      != null) _btnIniciar.clicked      += IniciarPartida;
        if (_btnAbandonar    != null) _btnAbandonar.clicked    += SalirDeSala;

        MostrarPanel(_panelMenu);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);

        if (_btnCrear        != null) _btnCrear.clicked        -= CrearSala;
        if (_btnUnirsePanel  != null) _btnUnirsePanel.clicked  -= MostrarPanelUnirse;
        if (_btnVolver       != null) _btnVolver.clicked       -= VolverAlMenu;
        if (_btnConfirmarUnirse != null) _btnConfirmarUnirse.clicked -= ConfirmarUnirse;
        if (_btnCancelarUnirse  != null) _btnCancelarUnirse.clicked  -= CancelarUnirse;
        if (_btnIniciar      != null) _btnIniciar.clicked      -= IniciarPartida;
        if (_btnAbandonar    != null) _btnAbandonar.clicked    -= SalirDeSala;
    }

    // ── Acciones ──────────────────────────────────────────────────────────────

    public void CrearSala()
    {
        if (!PhotonNetwork.IsConnected)
        {
            _accionPendiente = "crear";
            PhotonNetwork.NickName = "Host";
            PhotonNetwork.ConnectUsingSettings();
            MostrarEspera(null);
            return;
        }
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Lobby] Photon conectando, espera.");
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
        OcultarError();
        if (_inputCodigo != null) _inputCodigo.SetValueWithoutNotify("");
        MostrarPanel(_panelUnirse);
    }

    private void ConfirmarUnirse()
    {
        // .value es la propiedad correcta de TextField en UI Toolkit
        string codigo = _inputCodigo?.value ?? "";
        Debug.Log($"[Lobby] ConfirmarUnirse llamado. Código: '{codigo}'");
        Debug.Log($"[Lobby] Photon conectado: {PhotonNetwork.IsConnected}");
        Debug.Log($"[Lobby] Photon estado: {PhotonNetwork.NetworkClientState}");

        if (string.IsNullOrWhiteSpace(codigo))
        {
            MostrarError("Ingresa un código de 4 letras.");
            return;
        }
        OcultarError();
        _codigoPendiente = codigo.Trim().ToUpper();
        PhotonNetwork.NickName = "Jugador" + Random.Range(2, 99);

        if (!PhotonNetwork.IsConnected)
        {
            _accionPendiente = "unirse";
            PhotonNetwork.ConnectUsingSettings();
            MostrarEspera(null);
            return;
        }
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Lobby] Photon conectando, espera.");
            return;
        }
        PhotonNetwork.JoinRoom(_codigoPendiente);
        _codigoPendiente = null;
        MostrarEspera(null);
    }

    private void CancelarUnirse()
    {
        OcultarError();
        MostrarPanel(_panelMenu);
    }

    private void VolverAlMenu()
    {
        SceneManager.LoadScene(escenaMenuInicio);
    }

    public void IniciarPartida()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.LoadLevel(escenaSeleccion);
    }

    private void SalirDeSala()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        MostrarPanel(_panelMenu);
    }

    private void MostrarError(string msg)
    {
        if (_lblErrorUnirse == null) return;
        _lblErrorUnirse.text = msg;
        _lblErrorUnirse.style.display = DisplayStyle.Flex;
    }

    private void OcultarError()
    {
        if (_lblErrorUnirse != null)
            _lblErrorUnirse.style.display = DisplayStyle.None;
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void MostrarPanel(VisualElement objetivo)
    {
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
            _lblCodigoSala.text = codigo ?? "----";
        if (_btnIniciar != null)
            _btnIniciar.style.display = DisplayStyle.None;
    }

    private void ActualizarListaJugadores()
    {
        if (!PhotonNetwork.InRoom) return;
        int count = PhotonNetwork.CurrentRoom.PlayerCount;

        if (_lblContador != null)
            _lblContador.text = $"{count} / {maxJugadores}";

        if (_listaJugadores != null)
        {
            _listaJugadores.Clear();
            for (int i = 0; i < maxJugadores; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("jugador-slot");

                var lbl = new Label();
                if (i < count)
                {
                    var jugador = PhotonNetwork.PlayerList[i];
                    string nombre = jugador.NickName;
                    if (jugador.IsLocal)        nombre += " (Tú)";
                    if (jugador.IsMasterClient) nombre += " ★";
                    lbl.text = "● " + nombre;
                    lbl.AddToClassList("jugador-activo");
                }
                else
                {
                    lbl.text = "○  —";
                    lbl.AddToClassList("jugador-vacio");
                }
                slot.Add(lbl);
                _listaJugadores.Add(slot);
            }
        }

        // Botón iniciar: visible solo al master, activo desde 2 jugadores
        if (_btnIniciar != null)
        {
            _btnIniciar.style.display = PhotonNetwork.IsMasterClient
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _btnIniciar.SetEnabled(PhotonNetwork.IsMasterClient && count >= 2);
        }

        // Aviso de mínimo de jugadores: visible solo al master con menos de 2
        if (_lblAvisoMinimo != null)
        {
            _lblAvisoMinimo.style.display = PhotonNetwork.IsMasterClient && count < 2
                ? DisplayStyle.Flex
                : DisplayStyle.None;
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
        Debug.Log($"[Lobby] OnConnectedToMaster. Acción pendiente: {_accionPendiente != null}");
        if (_accionPendiente == "crear")
        {
            _accionPendiente = null;
            string codigo = GenerarCodigo();
            if (_lblCodigoSala != null) _lblCodigoSala.text = codigo;
            var opts = new RoomOptions { MaxPlayers = (byte)maxJugadores, IsVisible = false };
            PhotonNetwork.CreateRoom(codigo, opts, TypedLobby.Default);
        }
        else if (_accionPendiente == "unirse" && _codigoPendiente != null)
        {
            _accionPendiente = null;
            PhotonNetwork.JoinRoom(_codigoPendiente);
            _codigoPendiente = null;
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[Lobby] OnJoinedRoom exitoso");
        ActualizarListaJugadores();
    }

    public override void OnPlayerEnteredRoom(Player p) => ActualizarListaJugadores();
    public override void OnPlayerLeftRoom(Player p)    => ActualizarListaJugadores();

    public override void OnJoinRoomFailed(short code, string msg)
    {
        Debug.Log($"[Lobby] OnJoinRoomFailed: {code} - {msg}");
        MostrarPanel(_panelUnirse);
        MostrarError("Sala no encontrada. Verifica el código.");
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogWarning($"[Lobby] No se pudo crear sala: {msg}");
        MostrarPanel(_panelMenu);
    }
}
