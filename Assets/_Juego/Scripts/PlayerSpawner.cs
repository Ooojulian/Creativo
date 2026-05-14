using UnityEngine;
using Photon.Pun;

/// <summary>
/// Instancia el prefab del personaje elegido en SeleccionPersonajes y aplica
/// el color de JugadorColorManager al modelo resultante.
///
/// Uso: añadir a un GameObject en SampleScene. Se llama a sí mismo en Start
/// si Photon ya está en sala, o puede invocarse desde GameManager.IniciarPartida.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Punto de spawn (se sobreescribe por GameManager si lo tiene)")]
    [SerializeField] private Transform puntoSpawn;

    [Header("Prefabs de personajes (deben estar en Resources/Personajes/)")]
    [SerializeField] private GameObject prefabCaballero;
    [SerializeField] private GameObject prefabFantasma;
    [SerializeField] private GameObject prefabHeroe;
    [SerializeField] private GameObject prefabHeroina;

    private GameObject _instanciaLocal = null;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // En modo offline o si ya estamos en sala, spawnear automáticamente
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer != null)
            SpawnPersonajeLocal();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Instancia el prefab del personaje elegido, sin red (solo cliente local).
    /// Llama a JugadorColorManager para aplicar el tint de color.
    /// </summary>
    public GameObject SpawnPersonajeLocal()
    {
        string id = PlayerPrefs.GetString(SeleccionPersonajesUI.PREF_PERSONAJE, "Caballero");
        return SpawnPersonaje(id, puntoSpawn != null ? puntoSpawn.position : Vector3.zero);
    }

    /// <param name="id">Id del personaje: "Caballero", "Fantasma", "Heroe", "Heroina"</param>
    /// <param name="posicion">Posición world donde spawnear</param>
    public GameObject SpawnPersonaje(string id, Vector3 posicion)
    {
        if (_instanciaLocal != null)
            Destroy(_instanciaLocal);

        GameObject prefab = ResolverPrefab(id);
        if (prefab == null)
        {
            Debug.LogWarning($"[PlayerSpawner] Prefab no encontrado para id={id}");
            return null;
        }

        _instanciaLocal = Instantiate(prefab, posicion, Quaternion.identity);

        // Adjuntar PersonajeAnimador si el prefab no lo tiene ya
        var animador = _instanciaLocal.GetComponentInChildren<PersonajeAnimador>();
        if (animador == null)
            _instanciaLocal.AddComponent<PersonajeAnimador>();

        // Aplicar color del jugador local
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            JugadorColorManager.Instance?.AplicarColor(_instanciaLocal, PhotonNetwork.LocalPlayer);

        return _instanciaLocal;
    }

    /// <summary>Versión Photon: instancia usando PhotonNetwork.Instantiate para sincronizar.</summary>
    public GameObject SpawnPersonajeRed(Vector3 posicion)
    {
        string id      = PlayerPrefs.GetString(SeleccionPersonajesUI.PREF_PERSONAJE, "Caballero");
        string recurso = $"Personajes/{id}";

        GameObject go = PhotonNetwork.Instantiate(recurso, posicion, Quaternion.identity);
        if (go != null)
            JugadorColorManager.Instance?.AplicarColor(go, PhotonNetwork.LocalPlayer);

        return go;
    }

    // ── Resolución de prefab ──────────────────────────────────────────────────

    private GameObject ResolverPrefab(string id)
    {
        // Primero intentar el slot del inspector
        switch (id)
        {
            case "Caballero": if (prefabCaballero) return prefabCaballero; break;
            case "Fantasma":  if (prefabFantasma)  return prefabFantasma;  break;
            case "Heroe":     if (prefabHeroe)     return prefabHeroe;     break;
            case "Heroina":   if (prefabHeroina)   return prefabHeroina;   break;
        }

        // Fallback: Resources
        return Resources.Load<GameObject>($"Personajes/{id}");
    }
}
