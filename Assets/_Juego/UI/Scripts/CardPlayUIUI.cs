using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Versión UI Toolkit de CardPlayUI.
/// Panel de decisión: ¿usar o guardar carta?
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class CardPlayUIUI : MonoBehaviour
{
    public static CardPlayUIUI Instance;

    private UIDocument    _doc;
    private VisualElement _root;
    private VisualElement _panelRoot;
    private Label         _lblPregunta;
    private Button        _btnUsar;
    private Button        _btnGuardar;
    private Button        _btnCerrar;

    private CardSO         _cartaSeleccionada;
    private MovimientoFicha _jugadorActual;

    void Awake()
    {
        Instance = this;
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        _root        = _doc.rootVisualElement;
        _panelRoot   = _root.Q<VisualElement>("cardplay-root");
        _lblPregunta = _root.Q<Label>("lbl-pregunta");
        _btnUsar     = _root.Q<Button>("btn-usar");
        _btnGuardar  = _root.Q<Button>("btn-guardar");
        _btnCerrar   = _root.Q<Button>("btn-cerrar");

        _btnUsar?.RegisterCallback<ClickEvent>(_ => OnClickUsar());
        _btnGuardar?.RegisterCallback<ClickEvent>(_ => OnClickGuardar());
        _btnCerrar?.RegisterCallback<ClickEvent>(_ => OnClickCerrar());

        SetVisible(false);
    }

    void OnDisable()
    {
        _btnUsar?.UnregisterCallback<ClickEvent>(_ => OnClickUsar());
        _btnGuardar?.UnregisterCallback<ClickEvent>(_ => OnClickGuardar());
        _btnCerrar?.UnregisterCallback<ClickEvent>(_ => OnClickCerrar());
    }

    public void Mostrar(CardSO card, MovimientoFicha jugador)
    {
        _cartaSeleccionada = card;
        _jugadorActual     = jugador;

        if (_lblPregunta != null)
            _lblPregunta.text = $"¿Qué deseas hacer con {card.cardName}?";

        bool reservaLlena = jugador.inventario.reserve.Count >= jugador.inventario.maxReserveSize;
        _btnGuardar?.SetEnabled(!reservaLlena);

        SetVisible(true);
    }

    private void OnClickUsar()
    {
        SetVisible(false);
        _jugadorActual.inventario.RemoveFromHand(_cartaSeleccionada);
        CardManager.Instance.EjecutarEfectoInmediato(_cartaSeleccionada, _jugadorActual);
    }

    private void OnClickGuardar()
    {
        SetVisible(false);
        _jugadorActual.inventario.SaveToReserve(_cartaSeleccionada);
    }

    private void OnClickCerrar()
    {
        SetVisible(false);
    }

    private void SetVisible(bool v)
    {
        if (_panelRoot != null)
            _panelRoot.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
