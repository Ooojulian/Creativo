using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerHandUI : MonoBehaviour
{
    public GameManager gameManager;
    public Transform containerMano;
    public Transform containerReserva;
    public GameObject prefabCarta;

    // En red: jugador local fijo. En local: jugador del turno activo (cambia cada turno).
    private MovimientoFicha _jugadorLocal;
    private bool _modoLocal;

    private PlayerInventory _inventarioSuscrito;
    private bool _pendingRefresh;

    void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            _modoLocal = true;
        }
        else
        {
            _modoLocal = false;
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
            foreach (var ficha in gameManager.todosLosJugadores)
            {
                if (ficha != null && ficha.actorNumber == myActor)
                {
                    _jugadorLocal = ficha;
                    SuscribirInventario(_jugadorLocal);
                    _pendingRefresh = true;
                    break;
                }
            }
        }

        // Escuchar inicio de turno para re-suscribir en modo local cuando cambia el jugador activo
        if (gameManager != null)
            gameManager.OnTurnStarted += OnTurnStarted;
    }

    void OnDestroy()
    {
        DesuscribirInventario();
        if (gameManager != null)
            gameManager.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(MovimientoFicha jugador)
    {
        if (_modoLocal)
        {
            // En modo local: mostrar siempre las cartas del jugador cuyo turno es
            if (jugador != _jugadorLocal)
            {
                _jugadorLocal = jugador;
                SuscribirInventario(_jugadorLocal);
            }
        }
        // En modo red: _jugadorLocal es fijo (el local), pero el color de la reserva
        // cambia según si es mi turno — forzar redibujado
        _pendingRefresh = true;
    }

    private void SuscribirInventario(MovimientoFicha jugador)
    {
        DesuscribirInventario();
        if (jugador != null && jugador.inventario != null)
        {
            _inventarioSuscrito = jugador.inventario;
            _inventarioSuscrito.OnInventoryChanged += MarcarRefresh;
        }
    }

    private void DesuscribirInventario()
    {
        if (_inventarioSuscrito != null)
        {
            _inventarioSuscrito.OnInventoryChanged -= MarcarRefresh;
            _inventarioSuscrito = null;
        }
    }

    private void MarcarRefresh() => _pendingRefresh = true;

    private const float CARD_W = 80f;
    private const float CARD_H = 110f;

    void Update()
    {
        if (_pendingRefresh && _jugadorLocal != null)
        {
            _pendingRefresh = false;
            ActualizarUI();
        }
    }

    public void ActualizarUI()
    {
        foreach (Transform child in containerMano)   Destroy(child.gameObject);
        foreach (Transform child in containerReserva) Destroy(child.gameObject);

        if (_jugadorLocal == null || _jugadorLocal.inventario == null) return;

        Debug.Log($"[PlayerHandUI] ActualizarUI: mano={_jugadorLocal.inventario.hand.Count} reserva={_jugadorLocal.inventario.reserve.Count} jugador={_jugadorLocal.name} containerMano={containerMano != null} containerReserva={containerReserva != null} prefab={prefabCarta != null}");

        // Mostrar solo las cartas del jugador LOCAL de esta máquina
        foreach (var card in _jugadorLocal.inventario.hand)
        {
            var capturedCard   = card;
            var capturedPlayer = _jugadorLocal;
            GameObject obj = Instantiate(prefabCarta, containerMano);
            var rt = obj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(CARD_W, CARD_H);
            obj.GetComponent<Image>().sprite = capturedCard.artwork;

            var btn = obj.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError($"[PlayerHandUI] El prefabCarta '{prefabCarta.name}' no tiene un componente Button. " +
                               "Agrégalo en el Inspector para poder hacer clic en las cartas.");
                continue;
            }

            btn.onClick.AddListener(() =>
            {
                // En red: solo el jugador local puede jugar sus propias cartas en su turno
                if (Photon.Pun.PhotonNetwork.InRoom &&
                    GameSync.Instance != null && !GameSync.Instance.EsMiTurno) return;

                if (CardPlayUI.Instance == null) return;
                CardPlayUI.Instance.Mostrar(capturedCard, capturedPlayer);
            });
        }

        bool esMiTurno = !PhotonNetwork.InRoom ||
                         (GameSync.Instance != null && GameSync.Instance.EsMiTurno);

        foreach (var card in _jugadorLocal.inventario.reserve)
        {
            var capturedCard   = card;
            var capturedPlayer = _jugadorLocal;
            GameObject obj = Instantiate(prefabCarta, containerReserva);
            var rt2 = obj.GetComponent<RectTransform>();
            if (rt2 != null) rt2.sizeDelta = new Vector2(CARD_W, CARD_H);
            var img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = capturedCard.artwork;
                img.color = esMiTurno ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }

            var btn = obj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = esMiTurno;
                btn.onClick.AddListener(() =>
                {
                    if (PhotonNetwork.InRoom &&
                        GameSync.Instance != null && !GameSync.Instance.EsMiTurno) return;

                    if (CardPlayUI.Instance == null) return;
                    CardPlayUI.Instance.MostrarDesdeReserva(capturedCard, capturedPlayer);
                });
            }
        }
    }
}
