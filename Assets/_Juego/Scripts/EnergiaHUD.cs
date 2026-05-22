using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gestiona los botones de acciones de energía en el PanelHUD.
// Activa/desactiva botones según el estado del dado y la energía disponible.
public class EnergiaHUD : MonoBehaviour
{
    public static EnergiaHUD Instance;

    [Header("Botones de Energía")]
    public Button botonTiradaExtra;
    public Button botonDobleMovimiento;

    [Header("Textos de costo (opcional)")]
    public TextMeshProUGUI textoCostoTiradaExtra;
    public TextMeshProUGUI textoCostoDobleMovimiento;

    void Awake() { Instance = this; }

    void Start()
    {
        if (botonTiradaExtra != null)
            botonTiradaExtra.onClick.AddListener(OnTiradaExtra);

        if (botonDobleMovimiento != null)
            botonDobleMovimiento.onClick.AddListener(OnDobleMovimiento);

        ActualizarCostos();
        MostrarBotones(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted += _ => SetBotonesActivos(false);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted -= _ => SetBotonesActivos(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        bool juegoActivo = gm != null && gm.JugadorActual != null && gm.dado != null;
        if (!juegoActivo) { MostrarBotones(false); return; }

        // En red: solo mostrar botones al jugador local cuando es su turno
        bool esMiTurno = GameSync.Instance == null || !Photon.Pun.PhotonNetwork.InRoom
                         || (GameSync.Instance != null && GameSync.Instance.EsMiTurno);
        MostrarBotones(esMiTurno);
        if (!esMiTurno) return;

        bool puedeAccionar = !gm.dado.Lanzando;
        if (!puedeAccionar) { SetBotonesActivos(false); return; }

        var ctrl = gm.JugadorActual.GetComponent<EnergiaController>();
        if (ctrl == null) { SetBotonesActivos(false); return; }

        var acciones = EnergiaAcciones.Instance;
        if (acciones == null) { SetBotonesActivos(false); return; }

        if (botonTiradaExtra != null)
            botonTiradaExtra.interactable = ctrl.TieneEnergia(acciones.costoBoostDado);

        bool yaComprado = gm.JugadorActual.dobleTiroPendiente;
        if (botonDobleMovimiento != null)
            botonDobleMovimiento.interactable = !yaComprado && ctrl.TieneEnergia(acciones.costoDobleMovimiento);
    }

    void MostrarBotones(bool visible)
    {
        if (botonTiradaExtra != null)         botonTiradaExtra.gameObject.SetActive(visible);
        if (botonDobleMovimiento != null)     botonDobleMovimiento.gameObject.SetActive(visible);
        if (textoCostoTiradaExtra != null)    textoCostoTiradaExtra.gameObject.SetActive(visible);
        if (textoCostoDobleMovimiento != null) textoCostoDobleMovimiento.gameObject.SetActive(visible);
    }

    void OnTiradaExtra()
    {
        EnergiaAcciones.Instance?.UsarBoostDado();
    }

    void OnDobleMovimiento()
    {
        EnergiaAcciones.Instance?.ComprarDobleMovimiento();
    }

    void SetBotonesActivos(bool activo)
    {
        if (botonTiradaExtra != null)    botonTiradaExtra.interactable = activo;
        if (botonDobleMovimiento != null) botonDobleMovimiento.interactable = activo;
    }

    void ActualizarCostos()
    {
        var acciones = EnergiaAcciones.Instance;
        if (acciones == null) return;

        if (textoCostoTiradaExtra != null)
            textoCostoTiradaExtra.text = $"{acciones.costoBoostDado} ⚡";
        if (textoCostoDobleMovimiento != null)
            textoCostoDobleMovimiento.text = $"{acciones.costoDobleMovimiento} ⚡";
    }
}