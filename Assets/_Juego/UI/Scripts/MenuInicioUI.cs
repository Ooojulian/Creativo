using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class MenuInicioUI : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string escenaLobby = "Lobby";

    private UIDocument _doc;
    private Button _btnJugar;
    private Button _btnOpciones;
    private Button _btnSalir;

    private VisualElement _panelOpciones;
    private TextField _inputNombre;
    private Button _btnGuardarNombre;
    private Button _btnCerrarOpciones;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        var root = _doc.rootVisualElement;

        _btnJugar    = root.Q<Button>("btn-jugar");
        _btnOpciones = root.Q<Button>("btn-opciones");
        _btnSalir    = root.Q<Button>("btn-salir");

        _panelOpciones    = root.Q<VisualElement>("panel-opciones");
        _inputNombre      = root.Q<TextField>("input-nombre");
        _btnGuardarNombre = root.Q<Button>("btn-guardar-nombre");
        _btnCerrarOpciones = root.Q<Button>("btn-cerrar-opciones");

        _btnJugar?.RegisterCallback<ClickEvent>(_ => IrASeleccion());
        _btnOpciones?.RegisterCallback<ClickEvent>(_ => AbrirOpciones());
        _btnSalir?.RegisterCallback<ClickEvent>(_ => Salir());

        _btnGuardarNombre?.RegisterCallback<ClickEvent>(_ => GuardarNombre());
        _btnCerrarOpciones?.RegisterCallback<ClickEvent>(_ => CerrarOpciones());
    }

    void OnDisable()
    {
        _btnJugar?.UnregisterCallback<ClickEvent>(_ => IrASeleccion());
        _btnOpciones?.UnregisterCallback<ClickEvent>(_ => AbrirOpciones());
        _btnSalir?.UnregisterCallback<ClickEvent>(_ => Salir());
        _btnGuardarNombre?.UnregisterCallback<ClickEvent>(_ => GuardarNombre());
        _btnCerrarOpciones?.UnregisterCallback<ClickEvent>(_ => CerrarOpciones());
    }

    private void IrASeleccion()
    {
        SceneManager.LoadScene(escenaLobby);
    }

    private void AbrirOpciones()
    {
        if (_inputNombre != null)
            _inputNombre.value = PlayerPrefs.GetString("NombreJugador", "");

        _panelOpciones?.RemoveFromClassList("panel-oculto");
        _panelOpciones?.AddToClassList("panel-activo");
    }

    private void GuardarNombre()
    {
        string nombre = _inputNombre?.value.Trim() ?? "";
        if (!string.IsNullOrEmpty(nombre))
        {
            PlayerPrefs.SetString("NombreJugador", nombre);
            PlayerPrefs.Save();
        }
        CerrarOpciones();
    }

    private void CerrarOpciones()
    {
        _panelOpciones?.RemoveFromClassList("panel-activo");
        _panelOpciones?.AddToClassList("panel-oculto");
    }

    private void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
