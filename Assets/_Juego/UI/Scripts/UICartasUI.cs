using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Versión UI Toolkit de UICartas + CartasUIVisual combinadas.
/// Muestra una carta revelada centrada con fade in/out.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UICartasUI : MonoBehaviour
{
    public static UICartasUI Instance;

    [SerializeField] private float duracionFade = 0.3f;

    private UIDocument    _doc;
    private VisualElement _root;
    private VisualElement _panelRoot;
    private Label         _lblNombre;
    private Label         _lblEstado;

    private Coroutine _fadeCoroutine;

    void Awake()
    {
        Instance = this;
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        _root      = _doc.rootVisualElement;
        _panelRoot = _root.Q<VisualElement>("carta-reveal-root");
        _lblNombre = _root.Q<Label>("carta-nombre");
        _lblEstado = _root.Q<Label>("carta-estado");

        Limpiar();
    }

    public void MostrarRevelacion(CardSO carta)
    {
        if (carta == null) return;

        if (_lblNombre != null) _lblNombre.text = carta.cardName;
        if (_lblEstado != null) _lblEstado.text = "Sacaste esta carta";

        SetVisible(true, 0f);
    }

    public void MostrarResultado(CardSO carta, bool bloqueadaPorEscudo)
    {
        if (carta == null) return;
        if (_lblNombre != null) _lblNombre.text = carta.cardName;
        if (_lblEstado != null)
            _lblEstado.text = bloqueadaPorEscudo ? "Bloqueada por Escudo" : $"Aplicando: {carta.cardName}";

        // Bajar opacidad si está bloqueada
        if (_panelRoot != null)
        {
            var panel = _root.Q<VisualElement>("carta-panel");
            if (panel != null)
                panel.style.opacity = bloqueadaPorEscudo ? 0.5f : 1f;
        }
    }

    public void Limpiar()
    {
        SetVisible(false, 0f);
        if (_lblNombre != null) _lblNombre.text = "";
        if (_lblEstado != null) _lblEstado.text = "";
    }

    public System.Collections.IEnumerator FadeOutYLimpiar(float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        var panel = _root?.Q<VisualElement>("carta-panel");

        while (t < duracionFade)
        {
            t += Time.deltaTime;
            if (panel != null)
                panel.style.opacity = Mathf.Lerp(1f, 0f, t / duracionFade);
            yield return null;
        }

        Limpiar();
    }

    private void SetVisible(bool visible, float opacity)
    {
        if (_panelRoot == null) return;
        _panelRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        var panel = _root?.Q<VisualElement>("carta-panel");
        if (panel != null) panel.style.opacity = opacity;
    }
}
