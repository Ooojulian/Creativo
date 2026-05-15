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

    void Update()
    {
        // Solo actualizar si ya encontramos el jugador local
        if (_jugadorLocal != null)
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
            var capturedCard   = card;          // captura para el lambda
            var capturedPlayer = _jugadorLocal;
            GameObject obj = Instantiate(prefabCarta, containerMano);
            obj.GetComponent<Image>().sprite = capturedCard.artwork;
            obj.GetComponent<Button>().onClick.AddListener(
                () => CardPlayUI.Instance.Mostrar(capturedCard, capturedPlayer));
        }

        foreach (var card in _jugadorLocal.inventario.reserve)
        {
            GameObject obj = Instantiate(prefabCarta, containerReserva);
            obj.GetComponent<Image>().sprite = card.artwork;
            obj.GetComponent<Button>().interactable = false; // Se activan solas por trigger
        }
    }
}
