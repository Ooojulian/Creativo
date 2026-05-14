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
        public string materialPath;     // ruta Assets/ del material PBR propio del personaje
        public string cardElementName;  // name= en UXML
    }

    private static readonly DatosPersonaje[] Personajes = {
        new DatosPersonaje {
            id             = "Caballero",
            nombreUI       = "CABALLERO",
            prefabPath     = "Assets/_ThirdParty/Personajes/DogKnight/Prefab/DogPBR.prefab",
            materialPath   = "Assets/_ThirdParty/Personajes/DogKnight/Material/PBR.mat",
            cardElementName= "card-caballero"
        },
        new DatosPersonaje {
            id             = "Fantasma",
            nombreUI       = "FANTASMA",
            prefabPath     = "Assets/_ThirdParty/Personajes/GhostCharacter_Free/Prefabs/Ghost.prefab",
            materialPath   = "Assets/_ThirdParty/Personajes/GhostCharacter_Free/Materials/MaterialGhost.mat",
            cardElementName= "card-fantasma"
        },
        new DatosPersonaje {
            id             = "Heroe",
            nombreUI       = "HÉROE",
            prefabPath     = "Assets/_ThirdParty/Personajes/RPG Tiny Hero Duo/Prefab/MaleCharacterPBR.prefab",
            materialPath   = "Assets/_ThirdParty/Personajes/RPG Tiny Hero Duo/Material/PBR_Default.mat",
            cardElementName= "card-heroe"
        },
        new DatosPersonaje {
            id             = "Heroina",
            nombreUI       = "HEROÍNA",
            prefabPath     = "Assets/_ThirdParty/Personajes/RPG Tiny Hero Duo/Prefab/FemaleCharacterPBR.prefab",
            materialPath   = "Assets/_ThirdParty/Personajes/RPG Tiny Hero Duo/Material/PBR_Default.mat",
            cardElementName= "card-heroina"
        },
    };

    // ── Inspector (opcionales — si no se asignan, se crean por código) ────────

    [Header("Escenas")]
    [SerializeField] private string escenaLobby  = "Lobby";
    [SerializeField] private string escenaJuego  = "SampleScene";

    [Header("Viewport 3D (opcional — se auto-crea si está vacío)")]
    [SerializeField] private float velocidadRotacion = 40f;

    // Layer 8 = "CharPreview" registrado en Project Settings → Tags and Layers.
    private const int LAYER_PREVIEW = 8;
    private const int RT_SIZE       = 512;

    // ── Estado de preview ─────────────────────────────────────────────────────

    private Camera        _camPreview;
    private RenderTexture _rt;
    private GameObject    _modeloActual;
    private Coroutine     _coroutineActual = null;

    // ── Referencias UI ────────────────────────────────────────────────────────

    private UIDocument    _doc;
    private VisualElement _root;
    private Button        _btnVolver;
    private Button        _btnConfirmar;
    private Label         _lblSeleccion;
    private Label         _lblJugadores;
    private Label         _lblNombrePersonaje;
    private Label         _lblDescPersonaje;
    private VisualElement _playerDots;
    private VisualElement _colorIndicador;
    private Label         _lblColor;
    private VisualElement _renderDisplay;
    private VisualElement _panelAvisoAbandono;
    private Label         _lblAvisoAbandono;

    private DatosPersonaje? _seleccionActual = null;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        CrearInfraestructuraPreview();

        // Fix 4: excluir layer 8 desde Awake para que sea efectivo antes de Start
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.cullingMask &= ~(1 << LAYER_PREVIEW);
            Debug.Log($"[Preview] Main Camera cullingMask actualizado: {mainCam.cullingMask}");
        }
    }

    void Start()
    {
        // VincularRTConDelay se inicia aquí — es la única coroutine de Start,
        // no interfiere con _coroutineActual (que solo gestiona carga de modelo).
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
        PhotonNetwork.AddCallbackTarget(this);

        _root               = _doc.rootVisualElement;
        _btnVolver          = _root.Q<Button>("btn-volver");
        _btnConfirmar       = _root.Q<Button>("btn-confirmar");
        _lblSeleccion       = _root.Q<Label>("lbl-seleccion");
        _lblJugadores       = _root.Q<Label>("lbl-jugadores");
        _lblNombrePersonaje = _root.Q<Label>("lbl-personaje-nombre");
        _lblDescPersonaje   = _root.Q<Label>("lbl-personaje-desc");
        _playerDots         = _root.Q<VisualElement>("player-dots");
        _colorIndicador     = _root.Q<VisualElement>("color-indicador");
        _lblColor           = _root.Q<Label>("lbl-color");
        _renderDisplay      = _root.Q<VisualElement>("render-texture-display");
        _panelAvisoAbandono = _root.Q<VisualElement>("panel-aviso-abandono");
        _lblAvisoAbandono   = _root.Q<Label>("lbl-aviso-abandono");

        _btnVolver?.RegisterCallback<ClickEvent>(_ => Volver());
        _btnConfirmar?.RegisterCallback<ClickEvent>(_ => Confirmar());

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
        PhotonNetwork.RemoveCallbackTarget(this);

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
        _modeloActual.transform.Rotate(Vector3.up, -velocidadRotacion * Time.deltaTime, Space.World);
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
        Debug.Log($"[Preview] cullingMask={_camPreview.cullingMask} " +
                  $"(esperado={1 << 8}, layer8=correcto={_camPreview.cullingMask == (1 << 8)})");
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
        // Cancelar coroutine anterior antes de destruir el modelo para evitar
        // que la coroutine vieja intente acceder al objeto ya destruido.
        if (_coroutineActual != null)
        {
            StopCoroutine(_coroutineActual);
            _coroutineActual = null;
        }
        if (_modeloActual != null)
        {
            Destroy(_modeloActual);
            _modeloActual = null;
        }
        _coroutineActual = StartCoroutine(CargarModeloCoroutine(datos));
    }

    private IEnumerator CargarModeloCoroutine(DatosPersonaje datos)
    {
        Debug.Log($"[Preview] INICIO coroutine {datos.id}");

        GameObject prefab = CargarPrefab(datos);
        if (prefab == null) yield break;

        // Instanciar en variable LOCAL — no en _modeloActual todavía.
        // Esto evita que si CargarModelo() se llama de nuevo mientras corremos,
        // la nueva invocación destruya el objeto que esta coroutine está usando.
        GameObject nuevoModelo = Instantiate(
            prefab,
            new Vector3(0f, -200f, 0f),
            Quaternion.Euler(0f, 180f, 0f)
        );
        nuevoModelo.name = $"_Preview_{datos.id}";

        // Limpiar scripts y asignar layer síncronamente, antes del yield.
        // LimpiarScripts desactiva GhostScript antes de que Start() corra.
        // Layer se asigna aquí Y de nuevo post-yield por si algún Start() lo resetea.
        LimpiarScriptsDeGameplay(nuevoModelo);
        AsignarLayerRecursivo(nuevoModelo, LAYER_PREVIEW);
        Debug.Log($"[Preview] Scripts limpiados. Layer root={nuevoModelo.layer} (pre-yield)");

        // Exponer la referencia compartida ahora que el objeto está limpio
        _modeloActual = nuevoModelo;

        yield return null; // deja que Start() de scripts restantes corra
        Debug.Log($"[Preview] POST yield 1 — modelo vivo: {_modeloActual != null}");

        // Verificar que el modelo no fue destruido mientras esperábamos
        if (_modeloActual == null || _modeloActual != nuevoModelo)
        {
            Debug.LogWarning("[Preview] Modelo destruido durante yield 1, abortando.");
            yield break;
        }

        // Re-asignar layer por si algún Start() lo reseteó
        AsignarLayerRecursivo(_modeloActual, LAYER_PREVIEW);
        Debug.Log($"[Preview] Layer re-asignado post-yield: root={_modeloActual.layer}");

        ValidarVisibilidad(_modeloActual, datos.id);
        AplicarMaterial(_modeloActual, datos.materialPath);
        Debug.Log("[Preview] Visibilidad validada y material aplicado.");

        yield return null; // segundo frame para que los renderers se inicialicen
        Debug.Log("[Preview] POST yield 2");

        if (_modeloActual == null) { yield break; }

        AjustarCamara(_modeloActual);
        Debug.Log("[Preview] COROUTINE COMPLETA");

        _coroutineActual = null;
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

    private static void AplicarMaterial(GameObject go, string materialPath)
    {
        if (string.IsNullOrEmpty(materialPath)) return;

        Material mat = null;
#if UNITY_EDITOR
        mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
#endif
        if (mat == null)
        {
            Debug.LogWarning($"[Preview] Material no encontrado: {materialPath}");
            return;
        }

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            // Asignar el material a todos los slots del renderer
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            r.sharedMaterials = mats;
        }

        Debug.Log($"[Preview] Material '{mat.name}' aplicado a {go.name}");
    }

    private static void AsignarLayerRecursivo(GameObject go, int layer)
    {
        if (go == null)
        {
            Debug.LogError("[Preview] AsignarLayerRecursivo: go es NULL");
            return;
        }
        foreach (var t in go.GetComponentsInChildren<Transform>(includeInactive: true))
            t.gameObject.layer = layer;
        Debug.Log($"[Preview] Layer recursivo aplicado. Root layer={go.layer}");
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

        Debug.Log($"[Preview] Bounds center={b.center}, camPos={_camPreview.transform.position}, " +
                  $"camTarget hacia {centro}, dist={distancia:F2}");
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
        // No llama StopAllCoroutines() — VincularRTConDelay corre independientemente.
        // La coroutine de carga se cancela en CargarModelo() antes de llamar aquí.
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
        SceneManager.LoadScene(escenaLobby);
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

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ActualizarIndicadorJugadores();
        Debug.Log($"[SeleccionPersonajes] Jugador abandonó: {otherPlayer.NickName}. " +
                  $"Quedan: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            StartCoroutine(VolverALobbyPorAbandonos());
    }

    private IEnumerator VolverALobbyPorAbandonos()
    {
        MostrarAvisoAbandono("Un jugador abandonó la partida. Volviendo a la sala...");
        yield return new WaitForSeconds(3f);

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(escenaLobby);
        else
            SceneManager.LoadScene(escenaLobby);
    }

    private void MostrarAvisoAbandono(string mensaje)
    {
        if (_panelAvisoAbandono != null)
            _panelAvisoAbandono.style.display = DisplayStyle.Flex;
        if (_lblAvisoAbandono != null)
            _lblAvisoAbandono.text = mensaje;
    }

    public override void OnJoinedRoom()
    {
        ActualizarIndicadorColor();
        ActualizarIndicadorJugadores();
    }
}
