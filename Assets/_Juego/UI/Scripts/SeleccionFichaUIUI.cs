using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Versión UI Toolkit de SeleccionFichaUI.
/// Panel de selección entre Ficha A y Ficha B al inicio del turno.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SeleccionFichaUIUI : MonoBehaviour
{
    public GameManager gameManager;

    private UIDocument    _doc;
    private VisualElement _root;
    private VisualElement _panelRoot;
    private Button        _btnFichaA;
    private Button        _btnFichaB;
    private Label         _infoFichaA;
    private Label         _infoFichaB;

    private MovimientoFicha _jugadorActual;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        _root      = _doc.rootVisualElement;
        _panelRoot = _root.Q<VisualElement>("ficha-root");
        _btnFichaA = _root.Q<Button>("btn-ficha-a");
        _btnFichaB = _root.Q<Button>("btn-ficha-b");
        _infoFichaA = _root.Q<Label>("info-ficha-a");
        _infoFichaB = _root.Q<Label>("info-ficha-b");

        _btnFichaA?.RegisterCallback<ClickEvent>(_ => ElegirFicha(false));
        _btnFichaB?.RegisterCallback<ClickEvent>(_ => ElegirFicha(true));

        OcultarPanel();
    }

    void OnDisable()
    {
        _btnFichaA?.UnregisterCallback<ClickEvent>(_ => ElegirFicha(false));
        _btnFichaB?.UnregisterCallback<ClickEvent>(_ => ElegirFicha(true));
    }

    public void MostrarSeleccion(MovimientoFicha jugador)
    {
        _jugadorActual = jugador;

        if (_infoFichaA != null)
            _infoFichaA.text = $"Casilla {jugador.indiceActual}";

        if (_infoFichaB != null && jugador.fichaB != null)
            _infoFichaB.text = $"Casilla {jugador.fichaB.indiceActual}";

        if (_panelRoot != null)
            _panelRoot.style.display = DisplayStyle.Flex;
    }

    public void OcultarPanel()
    {
        if (_panelRoot != null)
            _panelRoot.style.display = DisplayStyle.None;
    }

    private void ElegirFicha(bool esB)
    {
        if (_jugadorActual == null) return;
        _jugadorActual.ElegirFicha(esB);
        OcultarPanel();

        if (gameManager != null && gameManager.dado != null)
            gameManager.dado.EjecutarMovimiento();
    }
}
