using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Pantalla de selección de personaje con preview 3D autocontenido.
///
/// El sistema de preview crea su propia cámara y RenderTexture por código —
/// no depende de configuración manual del Inspector para funcionar.
///
/// Pipeline: prefab clone → layer 31 → CamaraPreview (cullingMask=1<<31)
///           → RenderTexture 512×512 → backgroundImage del elemento UXML
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SeleccionPersonajesUI : MonoBehaviourPunCallbacks
{
    public const string PREF_PERSONAJE = "personaje_seleccionado";

    // ── Definición de personajes — línea 26 ──────────────────────────────────

    [System.Serializable]
    public struct DatosPersonaje
    {
        public string id;
        public string nombreUI;
        public string prefabPath;       // ruta Assets/ completa
        public string cardElementName;  // name= en UXML
    }

    private static readonly DatosPersonaje[] Personajes = {
        new DatosPersonaje {
            id             = "Caballero",
            nombreUI       = "CABALLERO",
            prefabPath     = "Assets/_ThirdParty/Personajes/DogKnight/Prefab/DogPBR.prefab",
            cardElementName= "card-caballero"
        },
        new DatosPersonaje {
            id             = "Fantasma",
            nombreUI       = "FANTASMA",
            prefabPath     = "Assets/_ThirdParty/Personajes/GhostCharacter_Free/Prefabs/Ghost.prefab",
            cardElementName= "card-fantasma"
        },
        new DatosPersonaje {
            id             = "Heroe",
            nombreUI       = "HÉROE",
            prefabPath     = "Assets/_ThirdParty/Personajes/RPG Tiny Hero Duo/Prefab/MaleCharacterPBR.prefab",
            cardElementName= "card-heroe"
        },
        new DatosPersonaje {
            id             = "Heroina",
            nombreUI       = "HEROÍNA",
            prefabPath     = "Assets/_ThirdParty/Personajes/RPG Tiny Hero Duo/Prefab/FemaleCharacterPBR.prefab",
            cardElementName= "card-heroina"
        },
    };

    // ── Inspector (opcionales — si no se asignan, se crean por código) ────────

    [Header("Escenas")]
    [SerializeField] private string escenaMenuInicio = "MenuInicio";
    [SerializeField] private string escenaJuego      = "SampleScene";

    [Header("Viewport 3D (opcional — se auto-crea si está vacío)")]
    [SerializeField] private float velocidadRotacion = 40f;

    // Layer 31 = User Layer reservado. Evita conflictos con layers del proyecto.
    // No necesita existir en Project Settings con nombre — solo necesita ser != 0.
    private const int LAYER_PREVIEW = 31;
    private const int RT_SIZE       = 512;

    // ── Estado de preview ─────────────────────────────────────────────────────

    private Camera        _camPreview;
    private RenderTexture _rt;
    private GameObject    _modeloActual;
    private int           _rotDirManual = 0;

    // ── Referencias UI ────────────────────────────────────────────────────────

    private UIDocument    _doc;
    private VisualElement _root;
    private Button        _btnVolver;
    private Button        _btnConfirmar;
    private Button        _btnRotIzq;
    private Button        _btnRotDer;
    private Label         _lblSeleccion;
    private Label         _lblJugadores;
    private Label         _lblNombrePersonaje;
    private Label         _lblDescPersonaje;
    private VisualElement _playerDots;
    private VisualElement _colorIndicador;
    private Label         _lblColor;
    private VisualElement _renderDisplay;

    private DatosPersonaje? _seleccionActual = null;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        CrearInfraestructuraPreview();
    }

    void Start()
    {
        // Excluir layer de preview de la cámara principal para que el modelo
        // no se vea flotando en la escena del tablero.
        Camera main = Camera.main;
        if (main != null)
            main.cullingMask &= ~(1 << LAYER_PREVIEW);

        // Fix Causa A: vincular RT después de 2 frames para que el layout de
        // UI Toolkit ya tenga dimensiones asignadas al elemento render-texture-display.
        StartCoroutine(VincularRTConDelay());
    }

    private IEnumerator VincularRTConDelay()
    {
        yield return null; // frame 1: UI Toolkit inicializa el layout
        yield return null; // frame 2: garantiza que el elemento tiene rect válido
        _renderDisplay = _doc.rootVisualElement.Q<VisualElement>("render-texture-display");
        VincularRTAlDisplay();
        Debug.Log($"[Preview] RT vinculada con delay. Rect del display: {_renderDisplay?.layout}");
    }

    void OnEnable()
    {
        _root               = _doc.rootVisualElement;
        _btnVolver          = _root.Q<Button>("btn-volver");
        _btnConfirmar       = _root.Q<Button>("btn-confirmar");
        _btnRotIzq          = _root.Q<Button>("btn-rot-izq");
        _btnRotDer          = _root.Q<Button>("btn-rot-der");
        _lblSeleccion       = _root.Q<Label>("lbl-seleccion");
        _lblJugadores       = _root.Q<Label>("lbl-jugadores");
        _lblNombrePersonaje = _root.Q<Label>("lbl-personaje-nombre");
        _lblDescPersonaje   = _root.Q<Label>("lbl-personaje-desc");
        _playerDots         = _root.Q<VisualElement>("player-dots");
        _colorIndicador     = _root.Q<VisualElement>("color-indicador");
        _lblColor           = _root.Q<Label>("lbl-color");
        _renderDisplay      = _root.Q<VisualElement>("render-texture-display");

        _btnVolver?.RegisterCallback<ClickEvent>(_ => Volver());
        _btnConfirmar?.RegisterCallback<ClickEvent>(_ => Confirmar());
        _btnRotIzq?.RegisterCallback<PointerDownEvent>(_ => _rotDirManual = -1);
        _btnRotIzq?.RegisterCallback<PointerUpEvent>(_ => _rotDirManual = 0);
        _btnRotDer?.RegisterCallback<PointerDownEvent>(_ => _rotDirManual = 1);
        _btnRotDer?.RegisterCallback<PointerUpEvent>(_ => _rotDirManual = 0);

        foreach (var p in Personajes)
        {
            var personaje = p;
            _root.Q<VisualElement>(p.cardElementName)
                 ?.RegisterCallback<ClickEvent>(_ => SeleccionarPersonaje(personaje));
        }

        // Registro de GeometryChangedEvent como fallback si el rect aún es cero
        _renderDisplay?.RegisterCallback<GeometryChangedEvent>(OnDisplayGeometryChanged);

        ActualizarBotonConfirmar();
        ActualizarIndicadorColor();
        ActualizarIndicadorJugadores();

        // Habilitar la cámara preview al entrar a la pantalla
        if (_camPreview != null) _camPreview.enabled = true;
    }

    void OnDisable()
    {
        _renderDisplay?.UnregisterCallback<GeometryChangedEvent>(OnDisplayGeometryChanged);
        DestruirModelo();
        if (_camPreview != null) _camPreview.enabled = false;

        // Restaurar cullingMask de la cámara principal
        Camera main = Camera.main;
        if (main != null)
            main.cullingMask |= 1 << LAYER_PREVIEW;
    }

    void OnDestroy()
    {
        // Destruir la cámara y la RT al salir de la escena (nunca DontDestroyOnLoad)
        if (_camPreview != null)
        {
            _camPreview.targetTexture = null;
            Destroy(_camPreview.gameObject);
        }
        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
        }
    }

    void Update()
    {
        if (_modeloActual == null) return;
        float dir = _rotDirManual != 0
            ? _rotDirManual * velocidadRotacion * 2f
            : velocidadRotacion;
        _modeloActual.transform.Rotate(Vector3.up, dir * Time.deltaTime, Space.World);
    }

    // ── Infraestructura de preview (se crea por código en Awake) ─────────────

    private void CrearInfraestructuraPreview()
    {
        // RenderTexture
        _rt = new RenderTexture(RT_SIZE, RT_SIZE, 16, RenderTextureFormat.ARGB32);
        _rt.name = "PersonajePreviewRT";
        _rt.Create();

        // Cámara de preview en un GameObject propio (hijo de este para que se destruya con la escena)
        var camGO = new GameObject("_CamaraPreview");
        camGO.transform.SetParent(transform);
        _camPreview = camGO.AddComponent<Camera>();

        _camPreview.clearFlags      = CameraClearFlags.SolidColor;
        _camPreview.backgroundColor = new Color(0.031f, 0.024f, 0.055f, 1f);
        // Fix Causa C: reset explícito a 0 antes de asignar para evitar que Unity
        // herede un cullingMask de otro componente o del prefab de cámara.
        _camPreview.cullingMask = 0;
        _camPreview.cullingMask = 1 << LAYER_PREVIEW;
        _camPreview.targetTexture   = _rt;
        _camPreview.nearClipPlane   = 0.01f;
        _camPreview.farClipPlane    = 500f;
        _camPreview.fieldOfView     = 45f;
        // Fix Causa B: depth=1 garantiza que esta cámara renderice DESPUÉS de la
        // Main Camera (depth=0 por defecto), por lo que su RT se escribe completa
        // antes de que UI Toolkit la lea en ese mismo frame.
        _camPreview.depth           = 1;
        _camPreview.enabled         = false; // se activa en OnEnable

        // Posición inicial alejada del tablero mientras no hay modelo
        camGO.transform.position = new Vector3(0f, -180f, -5f);

        Debug.Log($"[Preview] Infraestructura creada. RT={RT_SIZE}×{RT_SIZE}, layer={LAYER_PREVIEW}, depth={_camPreview.depth}, cullingMask={_camPreview.cullingMask}");
    }

    private void VincularRTAlDisplay()
    {
        if (_renderDisplay == null || _rt == null) return;
        _renderDisplay.style.backgroundImage = new StyleBackground(
            Background.FromRenderTexture(_rt));
        _renderDisplay.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        Debug.Log("[Preview] RT vinculada al elemento UXML.");
    }

    private void OnDisplayGeometryChanged(GeometryChangedEvent e)
    {
        if (e.newRect.width < 1f || e.newRect.height < 1f) return;
        // Re-vincular por si el elemento no tenía dimensiones antes
        VincularRTAlDisplay();
        _renderDisplay?.UnregisterCallback<GeometryChangedEvent>(OnDisplayGeometryChanged);
    }

    // ── Carga de modelo ───────────────────────────────────────────────────────

    private void CargarModelo(DatosPersonaje datos)
    {
        StartCoroutine(CargarModeloCoroutine(datos));
    }

    private IEnumerator CargarModeloCoroutine(DatosPersonaje datos)
    {
        DestruirModelo(); // destruir el anterior antes de instanciar

        GameObject prefab = CargarPrefab(datos);
        if (prefab == null) yield break;

        // Instanciar muy lejos del tablero (y=−200) para que sea invisible sin la RT
        Vector3 posPreview = new Vector3(0f, -200f, 0f);
        _modeloActual = Instantiate(prefab, posPreview, Quaternion.identity);
        _modeloActual.name = $"_Preview_{datos.id}";

        // Fix Causa D: limpiar scripts ANTES del yield, pero asignar layer DESPUÉS.
        // Motivo: Instantiate() dispara Awake() en el mismo frame; si GhostScript.Awake
        // o cualquier otro script resetea el layer, nuestra asignación previa se pierde.
        // Al esperar un frame dejamos que todos los Awake() corran y LUEGO sobreescribimos.
        LimpiarScriptsDeGameplay(_modeloActual);
        Debug.Log($"[Preview] Instancia creada: {_modeloActual.name} en layer={_modeloActual.layer} (antes de yield)");

        yield return null; // deja que Awake() de los scripts del prefab ejecute

        // Ahora asignar layer — después de que Awake() ya no puede resetearlo
        AsignarLayerRecursivo(_modeloActual, LAYER_PREVIEW);

        // Verificar que el layer quedó bien en todos los transforms
        int layersMal = 0;
        foreach (var t in _modeloActual.GetComponentsInChildren<Transform>(true))
            if (t.gameObject.layer != LAYER_PREVIEW) layersMal++;
        Debug.Log($"[Preview] Layer asignado: {LAYER_PREVIEW}. Transforms con layer incorrecto: {layersMal}");

        yield return null; // segundo frame para que los renderers se inicialicen

        // Validar visibilidad del modelo
        bool modeloVisible = ValidarVisibilidad(_modeloActual, datos.id);
        if (!modeloVisible)
            Debug.LogWarning($"[Preview] {datos.id}: modelo sin renderers visibles.");

        // Posicionar cámara basada en bounds reales
        AjustarCamara(_modeloActual);

        // Log de estado final del pipeline
        Debug.Log($"[Preview] Pipeline completo. CamEnabled={_camPreview?.enabled}, " +
                  $"cullingMask={_camPreview?.cullingMask}, " +
                  $"RT created={_rt?.IsCreated()}, " +
                  $"displayRect={_renderDisplay?.layout}");
    }

    private GameObject CargarPrefab(DatosPersonaje datos)
    {
        GameObject prefab = null;
#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(datos.prefabPath);
#endif
        if (prefab == null)
            prefab = Resources.Load<GameObject>($"Personajes/{datos.id}");

        if (prefab == null)
            Debug.LogWarning($"[Preview] Prefab no encontrado: {datos.prefabPath}");

        return prefab;
    }

    // ── Tarea 2: asignación de layer recursiva usando GetComponentsInChildren ─

    private static void AsignarLayerRecursivo(GameObject go, int layer)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(includeInactive: true))
            t.gameObject.layer = layer;
    }

    // ── Tarea 3: limpieza de scripts de gameplay ──────────────────────────────

    private static readonly string[] NombresADesactivar = {
        "Controller", "Input", "Demo", "Player", "Movement",
        "GhostScript",  // script específico del Ghost que usa Input.GetKeyDown
    };

    private static void LimpiarScriptsDeGameplay(GameObject clone)
    {
        // Desactivar CharacterController para que no genere conflictos de física
        foreach (var cc in clone.GetComponentsInChildren<CharacterController>(true))
        {
            cc.enabled = false;
            Debug.Log($"[Preview] CharacterController desactivado en {cc.gameObject.name}");
        }

        // Desactivar Rigidbody para evitar física no deseada
        foreach (var rb in clone.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Desactivar todos los MonoBehaviours cuyo nombre de tipo coincida
        foreach (var mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;
            foreach (var keyword in NombresADesactivar)
            {
                if (typeName.Contains(keyword))
                {
                    mb.enabled = false;
                    Debug.Log($"[Preview] Script desactivado: {typeName} en {mb.gameObject.name}");
                    break;
                }
            }
        }

        // Desactivar colliders (no los destruimos para no romper referencias)
        foreach (var col in clone.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }

    // ── Tarea 4: validación de visibilidad ────────────────────────────────────

    private static bool ValidarVisibilidad(GameObject go, string nombre)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(includeInactive: true);

        Debug.Log($"[Preview] {nombre}: {renderers.Length} renderer(s) encontrados.");

        if (renderers.Length == 0) return false;

        bool alguno = false;
        foreach (var r in renderers)
        {
            // Activar si estaba desactivado (algunos prefabs tienen renderers ocultos)
            r.enabled = true;

            // Verificar escala no cero
            Vector3 s = r.transform.lossyScale;
            if (Mathf.Abs(s.x) < 0.0001f || Mathf.Abs(s.y) < 0.0001f || Mathf.Abs(s.z) < 0.0001f)
            {
                Debug.LogWarning($"[Preview] Renderer en {r.gameObject.name} tiene escala ~0.");
                continue;
            }

            // Material fallback si es null
            if (r.sharedMaterial == null)
            {
                Shader fallback = Shader.Find("Universal Render Pipeline/Lit")
                               ?? Shader.Find("Standard");
                if (fallback != null)
                    r.material = new Material(fallback);
                Debug.LogWarning($"[Preview] Material null en {r.gameObject.name} — asignado fallback.");
            }

            alguno = true;
        }

        // Calcular y loguear bounds para diagnóstico
        Bounds b = CalcularBoundsEstatico(go);
        Debug.Log($"[Preview] {nombre}: bounds center={b.center}, size={b.size}");

        return alguno;
    }

    // ── Tarea 6: posicionamiento de cámara ────────────────────────────────────

    private void AjustarCamara(GameObject modelo)
    {
        if (modelo == null || _camPreview == null) return;

        Bounds b = CalcularBoundsEstatico(modelo);
        float magnitud = b.size.magnitude;
        if (magnitud < 0.01f) magnitud = 1f; // fallback

        float distancia = magnitud * 1.5f;
        Vector3 centro = b.center;

        _camPreview.transform.position = centro + new Vector3(0f, b.size.y * 0.1f, -distancia);
        _camPreview.transform.LookAt(centro);
        _camPreview.nearClipPlane = Mathf.Max(0.01f, distancia * 0.01f);
        _camPreview.farClipPlane  = distancia * 3f;

        Debug.Log($"[Preview] Cámara ajustada → pos={_camPreview.transform.position}, " +
                  $"centro={centro}, dist={distancia:F2}");
    }

    private static Bounds CalcularBoundsEstatico(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private void DestruirModelo()
    {
        StopAllCoroutines();
        if (_modeloActual != null)
        {
            Destroy(_modeloActual);
            _modeloActual = null;
        }
    }

    // ── Selección ─────────────────────────────────────────────────────────────

    private void SeleccionarPersonaje(DatosPersonaje datos)
    {
        foreach (var p in Personajes)
            _root.Q<VisualElement>(p.cardElementName)?.RemoveFromClassList("char-chip--selected");

        _seleccionActual = datos;
        _root.Q<VisualElement>(datos.cardElementName)?.AddToClassList("char-chip--selected");

        if (_lblSeleccion != null)       _lblSeleccion.text = $"Seleccionado: {datos.nombreUI}";
        if (_lblNombrePersonaje != null) _lblNombrePersonaje.text = datos.nombreUI;
        if (_lblDescPersonaje != null)   _lblDescPersonaje.text = "";

        CargarModelo(datos);

        PlayerPrefs.SetString(PREF_PERSONAJE, datos.id);
        PlayerPrefs.Save();

        ActualizarBotonConfirmar();

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            var props = new ExitGames.Client.Photon.Hashtable { { "personaje", datos.id } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    // ── Navegación ────────────────────────────────────────────────────────────

    private void Confirmar()
    {
        if (!_seleccionActual.HasValue)
        {
            Debug.LogWarning("[SeleccionPersonajes] No hay personaje seleccionado.");
            return;
        }

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(escenaJuego);
            return;
        }

        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "personaje", _seleccionActual.Value.id },
            { "listo",     true }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        _btnConfirmar?.SetEnabled(false);
        if (_btnConfirmar != null) _btnConfirmar.text = "Esperando...";

        if (PhotonNetwork.IsMasterClient) VerificarTodosListos();
    }

    private void Volver()
    {
        DestruirModelo();
        SceneManager.LoadScene(escenaMenuInicio);
    }

    // ── Indicadores UI ────────────────────────────────────────────────────────

    private void ActualizarBotonConfirmar()
    {
        if (_btnConfirmar == null) return;
        bool ok = _seleccionActual.HasValue;
        _btnConfirmar.SetEnabled(ok);
        _btnConfirmar.text = ok ? "CONFIRMAR SELECCIÓN →" : "Selecciona un héroe";
    }

    private void ActualizarIndicadorColor()
    {
        if (_colorIndicador == null) return;

        _colorIndicador.RemoveFromClassList("player-color-badge--rojo");
        _colorIndicador.RemoveFromClassList("player-color-badge--azul");
        _colorIndicador.RemoveFromClassList("player-color-badge--verde");
        _colorIndicador.RemoveFromClassList("player-color-badge--amarillo");

        if (!PhotonNetwork.InRoom)
        {
            if (_lblColor != null) _lblColor.text = "Sin sala";
            return;
        }

        string clase  = JugadorColorManager.ObtenerClaseCSS(PhotonNetwork.LocalPlayer.ActorNumber);
        string nombre = JugadorColorManager.ObtenerNombreColor(PhotonNetwork.LocalPlayer.ActorNumber);
        _colorIndicador.AddToClassList(clase);
        if (_lblColor != null) _lblColor.text = nombre;
    }

    private void ActualizarIndicadorJugadores()
    {
        if (!PhotonNetwork.InRoom)
        {
            if (_lblJugadores != null) _lblJugadores.text = "";
            return;
        }

        int total  = PhotonNetwork.CurrentRoom.PlayerCount;
        int listos = 0;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.TryGetValue("listo", out var v) && (bool)v) listos++;

        if (_lblJugadores != null) _lblJugadores.text = $"{listos} / {total} listos";

        if (_playerDots == null) return;
        _playerDots.Clear();

        for (int i = 0; i < Mathf.Max(total, 4); i++)
        {
            var dot = new VisualElement();
            dot.AddToClassList("player-dot");
            if (i < PhotonNetwork.PlayerList.Length)
            {
                var p     = PhotonNetwork.PlayerList[i];
                bool listo = p.CustomProperties.TryGetValue("listo", out var v) && (bool)v;
                if (listo) dot.AddToClassList("player-dot--active");
                dot.AddToClassList(JugadorColorManager.ObtenerClasePunto(p.ActorNumber));
            }
            _playerDots.Add(dot);
        }
    }

    private void VerificarTodosListos()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        foreach (var p in PhotonNetwork.PlayerList)
            if (!p.CustomProperties.TryGetValue("listo", out var v) || !(bool)v) return;
        PhotonNetwork.LoadLevel(escenaJuego);
    }

    // ── Callbacks Photon ──────────────────────────────────────────────────────

    public override void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable props)
    {
        ActualizarIndicadorJugadores();
        if (PhotonNetwork.IsMasterClient) VerificarTodosListos();
    }

    public override void OnPlayerEnteredRoom(Player p) => ActualizarIndicadorJugadores();
    public override void OnPlayerLeftRoom(Player p)    => ActualizarIndicadorJugadores();
    public override void OnJoinedRoom()
    {
        ActualizarIndicadorColor();
        ActualizarIndicadorJugadores();
    }
}
