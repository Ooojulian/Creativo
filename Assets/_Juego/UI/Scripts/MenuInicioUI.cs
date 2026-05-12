using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class MenuInicioUI : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string escenaSeleccionPersonajes = "SeleccionPersonajes";

    private UIDocument _doc;
    private Button _btnJugar;
    private Button _btnOpciones;
    private Button _btnSalir;

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

        _btnJugar?.RegisterCallback<ClickEvent>(_ => IrASeleccion());
        _btnOpciones?.RegisterCallback<ClickEvent>(_ => AbrirOpciones());
        _btnSalir?.RegisterCallback<ClickEvent>(_ => Salir());
    }

    void OnDisable()
    {
        _btnJugar?.UnregisterCallback<ClickEvent>(_ => IrASeleccion());
        _btnOpciones?.UnregisterCallback<ClickEvent>(_ => AbrirOpciones());
        _btnSalir?.UnregisterCallback<ClickEvent>(_ => Salir());
    }

    private void IrASeleccion()
    {
        SceneManager.LoadScene(escenaSeleccionPersonajes);
    }

    private void AbrirOpciones()
    {
        // Placeholder — implementar pantalla de opciones si se requiere
        Debug.Log("[MenuInicio] Opciones (no implementado).");
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
