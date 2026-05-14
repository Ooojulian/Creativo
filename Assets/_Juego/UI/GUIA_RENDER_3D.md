# Guía: Cómo vincular modelos 3D en Selección de Personajes

## Dónde está el código
- Script principal: `Assets/_Juego/UI/Scripts/SeleccionPersonajesUI.cs`
- Método que carga modelos: `CargarModeloConDelay()` (coroutine) — línea ~139
- Línea donde se define la lista de personajes: **línea 29** (`private static readonly DatosPersonaje[] Personajes`)

---

## Campos del Inspector a configurar

En el GameObject **SeleccionPersonajesUI** en la escena `SeleccionPersonajes.unity`:

| Campo | Tipo | Qué asignar |
|---|---|---|
| `camaraPreview` | Camera | Crear un GameObject hijo llamado `CamaraPreview` con un componente Camera; arrastrar aquí. No debe tener AudioListener. |
| `renderTexture` | RenderTexture | Crear desde `Assets/_Juego/UI/` con `clic derecho → Create → Render Texture`; 512×512, formato ARGB32. Asignar aquí Y en el campo `Target Texture` de la CamaraPreview (Unity los vincula automáticamente si lo asignas por inspector o lo hace el script en runtime; el script ya lo hace en runtime, pero es buena práctica asignarlo también en la cámara desde el inspector). |
| `puntoVistaModelo` | Transform | Crear un GameObject vacío llamado `PuntoModelo` en posición `(0, -200, 0)` — lejos del tablero para que no interfiera visualmente. Arrastrar aquí. |
| `velocidadRotacion` | float | Valor por defecto 40 (grados/segundo). Ajustar al gusto. |

---

## Cómo crear la RenderTexture manualmente

1. En el Project panel, navegar a `Assets/_Juego/UI/`
2. Clic derecho → **Create → Render Texture**
3. Nombrarla `PersonajePreview`
4. En el Inspector de la RT:
   - **Size**: 512 × 512 (o 1024 × 1024 para mayor resolución)
   - **Color Format**: `ARGB32` (o `DefaultHDR` si el proyecto usa HDR)
   - **Depth Buffer**: `At least 16 bits depth` (necesario para que el modelo se renderice con profundidad)
   - **Anti-aliasing**: 2× o 4× si se desea suavizado de bordes
5. Asignar esta RT al campo `renderTexture` del Inspector de `SeleccionPersonajesUI`

---

## Configuración de la Cámara de Preview

La `CamaraPreview` requiere ajustes manuales en el Inspector:

1. Crear GameObject hijo de `SeleccionPersonajesUI` o vacío en la escena
2. Agregar componente `Camera`
3. Configurar:
   - **Clear Flags**: Solid Color
   - **Background**: color oscuro (#080610 — el mismo del viewport)
   - **Culling Mask**: seleccionar **solo** el layer `CharPreview` (layer 8)
   - **Target Texture**: arrastrar la RenderTexture `PersonajePreview`
   - **Depth**: -2 (menor que la cámara principal para que se renderice primero)
4. Si el proyecto usa **URP**:
   - Agregar componente `Universal Additional Camera Data`
   - **Render Type**: Base
   - **Rendering**: en el URP Asset, asegurarse de que la cámara aparezca en la stack si es Overlay, o dejarlo como Base si es independiente

---

## Configuración del Layer "CharPreview"

El script usa el layer **8** para aislar los modelos del viewport del resto de la escena.

1. Ir a `Edit → Project Settings → Tags and Layers`
2. En **Layers**, asignar el slot `User Layer 8` al nombre `CharPreview`
3. Verificar que la **Cámara Principal** (Main Camera) tenga el layer 8 **desmarcado** en su `Culling Mask` — el script lo hace automáticamente en `Start()`, pero conviene confirmarlo en el Inspector también

---

## Cómo agregar o cambiar un personaje

### Cambiar el prefab de un personaje existente

1. Abrir `SeleccionPersonajesUI.cs`
2. Ir a la **línea 29**: `private static readonly DatosPersonaje[] Personajes`
3. Encontrar la entrada del personaje a modificar (ej. `"Caballero"`)
4. Cambiar el campo `prefabPath` con la ruta exacta desde `Assets/`:
   ```csharp
   prefabPath = "Assets/_ThirdParty/Personajes/NuevoCarpeta/NuevoPrefab.prefab"
   ```
5. Guardar y recompilar. La carga usa `AssetDatabase` en Editor y `Resources` en build.

### Agregar un personaje nuevo

1. En `SeleccionPersonajesUI.cs`, línea 29, añadir una nueva entrada al array `Personajes`:
   ```csharp
   new DatosPersonaje {
       id             = "NuevoPersonaje",
       nombreUI       = "NUEVO",
       prefabPath     = "Assets/_ThirdParty/Personajes/.../NuevoPrefab.prefab",
       cardElementName= "card-nuevo"
   },
   ```
2. En `SeleccionPersonajes.uxml`, añadir un chip en `<ui:VisualElement name="char-strip">`:
   ```xml
   <ui:VisualElement name="card-nuevo" class="char-chip">
       <ui:Label text="🗡️" class="chip-icon" />
       <ui:Label text="NUEVO" class="chip-name" />
   </ui:VisualElement>
   ```
3. Para **builds** (no solo Editor): copiar el prefab a `Assets/Resources/Personajes/NuevoPersonaje.prefab` para que `Resources.Load` funcione en build.

---

## Troubleshooting

Si el modelo no aparece en el viewport:

- [ ] **RT no asignada**: verificar que los campos `camaraPreview` y `renderTexture` estén asignados en el Inspector de `SeleccionPersonajesUI`. Ambos deben apuntar al mismo objeto `RenderTexture`.
- [ ] **Layer incorrecto**: en `Project Settings → Tags and Layers`, el slot `User Layer 8` debe llamarse `CharPreview`. Si el nombre difiere, el modelo queda en el layer 0 y la cámara de preview (cullingMask = 1<<8) no lo ve.
- [ ] **Cámara principal muestra el modelo flotante**: la cámara principal debe excluir el layer 8 de su `Culling Mask`. El script lo hace en `Start()`, pero si hay otras cámaras en la escena deben configurarse manualmente.
- [ ] **Viewport negro pero sin modelo**: el viewport tiene la RT correcta pero el prefab no cargó. Revisar la consola por warnings `[SeleccionPersonajes] Prefab no encontrado`. En builds, el prefab debe estar en `Resources/Personajes/`.
- [ ] **URP: modelo negro o invisible**: agregar `Universal Additional Camera Data` a la `CamaraPreview` con `Render Type = Base`. Sin este componente, URP puede ignorar la cámara.
- [ ] **Dimensiones cero en el elemento UXML**: si `_viewportListo` nunca se activa (verificar en consola que aparezca `Viewport configurado`), el elemento `render-texture-display` no tiene altura. En el USS asegurarse de que `seleccion-render-display` tenga `position: absolute; top:0; left:0; right:0; bottom:0;` y que el padre `seleccion-viewport-wrap` tenga `flex-grow:1`.
- [ ] **Modelo fuera de cuadro**: `puntoVistaModelo` puede estar muy lejos o demasiado cerca del nearClipPlane de la cámara. Colocar el punto en `(0, -200, 0)` y ajustar la posición hasta que se vea correctamente.

---

## Configuración de layers — resumen

| Elemento | Layer | Culling Mask |
|---|---|---|
| Modelo 3D de preview | **8** (CharPreview) | — |
| Cámara de preview | — | Solo layer **8** |
| Cámara principal | — | Todos **excepto** layer 8 |

Para verificar en el Inspector:
- `CamaraPreview → Culling Mask`: debe mostrar únicamente `CharPreview` marcado
- `Main Camera → Culling Mask`: debe mostrar todos los layers **excepto** `CharPreview`
