using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Versión UI Toolkit del EnergiaHUD.
/// Reemplaza EnergiaHUD.cs cuando se usa UIDocument en lugar de Canvas uGUI.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class EnergiaHUDUI : MonoBehaviour
{
    public static EnergiaHUDUI Instance;

    private UIDocument   _doc;
    private VisualElement _root;
    private Label        _lblValor;
    private Button       _btnTiradaExtra;
    private Button       _btnDobleMov;
    private Label        _costoTirada;
    private Label        _costoDoble;

    void Awake()
    {
        Instance = this;
        _doc  = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        _root          = _doc.rootVisualElement;
        _lblValor      = _root.Q<Label>("energy-value");
        _btnTiradaExtra = _root.Q<Button>("btn-tirada-extra");
        _btnDobleMov   = _root.Q<Button>("btn-doble-mov");
        _costoTirada   = _root.Q<Label>("costo-tirada");
        _costoDoble    = _root.Q<Label>("costo-doble");

        _btnTiradaExtra?.RegisterCallback<ClickEvent>(_ => EnergiaAcciones.Instance?.UsarTiradaExtra());
        _btnDobleMov?.RegisterCallback<ClickEvent>(_ => EnergiaAcciones.Instance?.ComprarDobleMovimiento());

        ActualizarCostos();
        SetVisible(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted += OnTurnStarted;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(MovimientoFicha _) => SetBotonesInteractivos(false);

    void Update()
    {
        var gm = GameManager.Instance;
        bool juegoActivo = gm != null && gm.JugadorActual != null && gm.dado != null;
        SetVisible(juegoActivo);
        if (!juegoActivo) return;

        // Actualizar valor de energía
        var ctrl = gm.JugadorActual.GetComponent<EnergiaController>();
        if (_lblValor != null)
            _lblValor.text = ctrl != null ? ctrl.EnergiaActual.ToString() : "0";

        bool dadoListo = gm.dado.EsperandoConfirmacion;
        if (!dadoListo) { SetBotonesInteractivos(false); return; }

        if (ctrl == null) { SetBotonesInteractivos(false); return; }

        var acc = EnergiaAcciones.Instance;
        if (acc == null) { SetBotonesInteractivos(false); return; }

        if (_btnTiradaExtra != null)
            _btnTiradaExtra.SetEnabled(ctrl.TieneEnergia(acc.costoTiradaExtra));

        bool yaComprado = gm.JugadorActual.dobleTiroPendiente;
        if (_btnDobleMov != null)
            _btnDobleMov.SetEnabled(!yaComprado && ctrl.TieneEnergia(acc.costoDobleMovimiento));
    }

    private void SetVisible(bool visible)
    {
        if (_root == null) return;
        var hud = _root.Q<VisualElement>("energia-hud");
        if (hud != null)
            hud.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetBotonesInteractivos(bool activo)
    {
        _btnTiradaExtra?.SetEnabled(activo);
        _btnDobleMov?.SetEnabled(activo);
    }

    private void ActualizarCostos()
    {
        var acc = EnergiaAcciones.Instance;
        if (acc == null) return;
        if (_costoTirada != null) _costoTirada.text = $"({acc.costoTiradaExtra}⚡)";
        if (_costoDoble  != null) _costoDoble.text  = $"({acc.costoDobleMovimiento}⚡)";
    }
}
