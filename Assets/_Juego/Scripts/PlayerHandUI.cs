using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerHandUI : MonoBehaviour
{
    public GameManager gameManager;
    public Transform containerMano;
    public Transform containerReserva;
    public GameObject prefabCarta;

    // Referencia al jugador LOCAL de esta máquina (no el del turno activo)
    private MovimientoFicha _jugadorLocal;

    // Huellas digitales del inventario para detectar cambios sin redibujar cada frame
    private int _prevHandHash    = int.MinValue;
    private int _prevReserveHash = int.MinValue;

    void Start()
    {
        // Si no hay red (modo local), asumimos que la mano visible es la del jugador de turno
        // o por defecto el jugador 0.
        if (!PhotonNetwork.InRoom)
        {
            if (gameManager.todosLosJugadores.Count > 0)
                _jugadorLocal = gameManager.todosLosJugadores[0];
            return;
        }

        // Si hay red, buscamos cuál ficha nos corresponde
        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
        int myIndex = -1;
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (PhotonNetwork.PlayerList[i].ActorNumber == myActor)
            {
                myIndex = i;
                break;
            }
        }

        if (myIndex >= 0 && myIndex < gameManager.todosLosJugadores.Count)
        {
            _jugadorLocal = gameManager.todosLosJugadores[myIndex];
        }
    }

    // Calcula una huella digital rápida del inventario del jugador.
    // Devuelve true solo cuando la mano o la reserva cambiaron.
    bool InventarioCambio()
    {
        if (_jugadorLocal == null || _jugadorLocal.inventario == null) return false;

        int handHash = _jugadorLocal.inventario.hand.Count;
        foreach (var c in _jugadorLocal.inventario.hand)
            if (c != null) handHash ^= (int)c.type * 397;

        int reserveHash = _jugadorLocal.inventario.reserve.Count;
        foreach (var c in _jugadorLocal.inventario.reserve)
            if (c != null) reserveHash ^= (int)c.type * 397;

        if (handHash != _prevHandHash || reserveHash != _prevReserveHash)
        {
            _prevHandHash    = handHash;
            _prevReserveHash = reserveHash;
            return true;
        }
        return false;
    }

    void Update()
    {
        // Solo redibuja la mano si el inventario realmente cambió.
        // Si se actualizara cada frame, los botones se destruirían antes
        // de que Unity registre el clic del jugador.
        if (_jugadorLocal != null && InventarioCambio())
            ActualizarUI();
    }

    public void ActualizarUI()
    {
        // Limpiar contenedores
        foreach (Transform child in containerMano)   Destroy(child.gameObject);
        foreach (Transform child in containerReserva) Destroy(child.gameObject);

        if (_jugadorLocal == null || _jugadorLocal.inventario == null) return;

        // Mostrar solo las cartas del jugador LOCAL de esta máquina
        foreach (var card in _jugadorLocal.inventario.hand)
        {
            var capturedCard   = card;
            var capturedPlayer = _jugadorLocal;
            GameObject obj = Instantiate(prefabCarta, containerMano);
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
                Debug.Log($"[PlayerHandUI] ¡Click registrado físicamente en el botón de la carta: {capturedCard.cardName}!");
                if (CardPlayUI.Instance == null)
                {
                    Debug.LogError("[PlayerHandUI] CardPlayUI.Instance es null. " +
                                   "Asegúrate de que el GameObject con el script CardPlayUI " +
                                   "exista en la escena y esté activo.");
                    return;
                }
                CardPlayUI.Instance.Mostrar(capturedCard, capturedPlayer);
            });
        }

        foreach (var card in _jugadorLocal.inventario.reserve)
        {
            var capturedCard   = card;
            var capturedPlayer = _jugadorLocal;
            GameObject obj = Instantiate(prefabCarta, containerReserva);
            obj.GetComponent<Image>().sprite = capturedCard.artwork;

            var btn = obj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[PlayerHandUI] Click en carta de RESERVA: {capturedCard.cardName}");
                    if (CardPlayUI.Instance == null) { Debug.LogError("[PlayerHandUI] CardPlayUI.Instance es null"); return; }
                    CardPlayUI.Instance.MostrarDesdeReserva(capturedCard, capturedPlayer);
                });
            }
        }
    }
}
