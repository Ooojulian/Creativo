# Character Preview — Causas raíz y fixes aplicados

## Causa raíz de por qué no se veía nada

Tres causas simultáneas bloqueaban el pipeline completo:

| # | Causa | Efecto |
|---|---|---|
| 1 | No existía cámara de preview — solo la Main Camera que apunta al tablero | Nada renderizaba en la RenderTexture |
| 2 | Layer "CharPreview" (8) no existía en Project Settings; el clone quedaba en layer 0 (Default); `cullingMask = 1 << 8` no capturaba nada | RT recibía render vacío |
| 3 | `Ghost.prefab` incluye `GhostScript.cs` (namespace `Sample`) con `CharacterController` + `Input.GetKeyDown` (API vieja) + `GameObject.Find("Canvas/HP")` que lanza `NullReferenceException` e `InvalidOperationException` con New Input System, crasheando el frame antes de que el renderer pudiera emitir geometría | Exception en Update → renderer no emite geometría hacia la RT |

---

## Archivos modificados

| Archivo | Cambio |
|---|---|
| `Assets/_Juego/UI/Scripts/SeleccionPersonajesUI.cs` | Reescrito completamente con infraestructura autónoma (ver detalles abajo) |

> Los prefabs de `_ThirdParty/` **no fueron modificados**.

---

## Pipeline final: cómo llegan los píxeles a la UI

```
Awake()
  └─ CrearInfraestructuraPreview()
       ├─ new RenderTexture(512, 512, 16, ARGB32)  ← _rt
       └─ new Camera() en GameObject hijo          ← _camPreview
            cullingMask = 1 << 31    (layer 31, siempre libre)
            targetTexture = _rt
            depth = -2               (renderiza antes que Main Camera)

OnEnable()  →  VincularRTAlDisplay()
  └─ _renderDisplay.style.backgroundImage = Background.FromRenderTexture(_rt)

Click en chip  →  SeleccionarPersonaje()  →  CargarModeloCoroutine()
  ├─ Instantiate(prefab, (0,-200,0))
  ├─ AsignarLayerRecursivo(clone, 31)   ← todos los transforms al layer 31
  ├─ LimpiarScriptsDeGameplay(clone)    ← desactiva CharacterController,
  │    GhostScript, Rigidbody, Colliders y cualquier MB con nombre sospechoso
  ├─ yield return null                  ← esperar 1 frame para que Unity
  │    inicialice los renderers
  ├─ ValidarVisibilidad()               ← activa renderers, asigna material
  │    fallback si es null, loguea bounds
  └─ AjustarCamara()                   ← posición basada en bounds reales
       camera.transform.position = bounds.center + (0, size.y*0.1, -dist*1.5)
       camera.LookAt(bounds.center)
       nearClip = dist * 0.01f
       farClip  = dist * 3f

Unity render loop:
  _camPreview renders layer 31  →  _rt  →  UXML backgroundImage  →  pantalla
```

---

## Configuración de layers

| Elemento | Layer usado | Por qué |
|---|---|---|
| Clones de preview | **31** (siempre libre, no necesita nombre en Project Settings) | Evita colisiones con layers del proyecto |
| Cámara principal | Excluye layer 31 (hecho por código en `Start()`) | El clone en y=-200 no aparece en el tablero |
| Cámara de preview | Solo incluye layer 31 (`cullingMask = 1 << 31`) | Solo renderiza el clone, no el tablero |

**No se requiere configurar nada en Project Settings → Tags and Layers.**

---

## Cómo agregar un personaje nuevo

1. **En el script**, ir a la línea ~36 (`private static readonly DatosPersonaje[] Personajes`) y añadir:
   ```csharp
   new DatosPersonaje {
       id             = "MiPersonaje",
       nombreUI       = "MI PERSONAJE",
       prefabPath     = "Assets/_ThirdParty/Personajes/Carpeta/MiPrefab.prefab",
       cardElementName= "card-mipersonaje"
   },
   ```

2. **En el UXML** (`Assets/_Juego/UI/UXML/SeleccionPersonajes.uxml`), añadir dentro de `<ui:VisualElement name="char-strip">`:
   ```xml
   <ui:VisualElement name="card-mipersonaje" class="char-chip">
       <ui:Label text="🗡️" class="chip-icon" />
       <ui:Label text="MI PERSONAJE" class="chip-name" />
   </ui:VisualElement>
   ```

3. **Para builds** (no solo Editor): copiar el prefab a `Assets/Resources/Personajes/MiPersonaje.prefab`
   El código usa `AssetDatabase` en Editor y `Resources.Load` en build.

4. Si el prefab nuevo tiene scripts de gameplay que generen errores, se desactivarán
   automáticamente si su nombre de tipo contiene: `Controller`, `Input`, `Demo`, `Player`,
   `Movement`. Para añadir más, editar `NombresADesactivar` en el script.

---

## Checklist de debug si el modelo no aparece

- [ ] **Consola sin errores rojos**: cualquier exception en `Awake`/`Start` del clone puede cortar el pipeline antes de llegar a `AjustarCamara`. Revisar la consola primero.
- [ ] **Log "[Preview] Infraestructura creada"**: debe aparecer al entrar a la escena. Si no aparece, `Awake()` no ejecutó `CrearInfraestructuraPreview()`.
- [ ] **Log "[Preview] RT vinculada al elemento UXML"**: si no aparece, `_renderDisplay` es null — el name= en el UXML no coincide con `"render-texture-display"`.
- [ ] **Log "[Preview] X: N renderer(s)"**: si N=0, el prefab no tiene SkinnedMeshRenderer ni MeshRenderer. El clone existe pero es invisible por diseño.
- [ ] **Log "[Preview] Cámara ajustada"**: si no aparece, `AjustarCamara()` no llegó a ejecutarse (probablemente el prefab no cargó).
- [ ] **Viewport negro vs invisible**: si el área del viewport es del color de fondo oscuro pero no hay modelo = la RT está conectada pero el render está vacío. Verificar que `_camPreview.enabled = true` y que `cullingMask = 1 << 31`.
- [ ] **URP**: si el proyecto usa URP y la cámara de preview muestra negro, puede ser necesario agregar `UniversalAdditionalCameraData` al GameObject `_CamaraPreview` con `renderType = Base`. Esto no se puede hacer por código sin depender del assembly de URP — ver nota abajo.

---

## Configuración manual pendiente en el Inspector

**Ninguna** — el sistema se auto-configura completamente en `Awake()`.

### Excepción: proyectos con URP y stack de cámaras

Si el proyecto tiene el URP Asset configurado con "Renderer Feature" que solo aplica
a cámaras con `UniversalAdditionalCameraData`, la cámara generada por código puede
no recibir el mismo post-processing que la Main Camera. Para solucionarlo:

1. Entrar a Play Mode
2. En la Hierarchy, buscar el GameObject `_CamaraPreview` (hijo de `SeleccionPersonajesUI`)
3. En su Inspector, confirmar que tiene el componente `Universal Additional Camera Data`
   (Unity lo agrega automáticamente en URP). Verificar que `Render Type = Base`.
4. Si el modelo aparece negro o sin iluminación, ir al URP Asset y en
   `Lighting → Additional Lights` habilitar al menos 1 luz adicional por objeto.

El `_CamaraPreview` usa la misma configuración de renderer que la cámara principal
porque hereda el URP Asset del proyecto — en la mayoría de proyectos URP esto funciona
sin configuración adicional.
