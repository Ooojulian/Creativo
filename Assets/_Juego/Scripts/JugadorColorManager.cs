using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Singleton persistente que asigna y aplica colores por jugador según su ActorNumber de Photon.
/// Jugador 1=Rojo, 2=Azul, 3=Verde, 4=Amarillo.
/// Usa MaterialPropertyBlock para no modificar instancias de material compartidas.
/// </summary>
public class JugadorColorManager : MonoBehaviourPunCallbacks
{
    public static JugadorColorManager Instance;

    // ── Paleta ────────────────────────────────────────────────────────────────

    private static readonly Color[] Paleta = {
        new Color(0.753f, 0.224f, 0.169f, 1f),   // 1 Rojo
        new Color(0.141f, 0.443f, 0.639f, 1f),   // 2 Azul
        new Color(0.118f, 0.518f, 0.286f, 1f),   // 3 Verde
        new Color(0.718f, 0.584f, 0.043f, 1f),   // 4 Amarillo
    };

    private static readonly string[] NombresColor = { "Rojo", "Azul", "Verde", "Amarillo" };
    private static readonly string[] ClasesCSS    = {
        "player-color-badge--rojo",
        "player-color-badge--azul",
        "player-color-badge--verde",
        "player-color-badge--amarillo",
    };
    private static readonly string[] ClasesPunto = {
        "player-dot--rojo",
        "player-dot--azul",
        "player-dot--verde",
        "player-dot--amarillo",
    };

    private const string PROP_COLOR_R = "cr";
    private const string PROP_COLOR_G = "cg";
    private const string PROP_COLOR_B = "cb";

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Publicar color propio en cuanto entremos a una sala
        if (PhotonNetwork.InRoom)
            PublicarColorPropio();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Aplica el color del jugador Photon al renderer del GameObject.</summary>
    public void AplicarColor(GameObject go, Player jugador)
    {
        if (go == null || jugador == null) return;

        Color color = ObtenerColor(jugador.ActorNumber);

        var block = new MaterialPropertyBlock();
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            // Solo modificar el material principal (índice 0 = cuerpo)
            r.GetPropertyBlock(block, 0);
            block.SetColor("_BaseColor", color);    // URP
            block.SetColor("_Color",     color);    // legacy fallback
            r.SetPropertyBlock(block, 0);
        }
    }

    /// <summary>Aplica el color leyendo las CustomProperties del jugador (para clientes remotos).</summary>
    public void AplicarColorDesdeProps(GameObject go, Player jugador)
    {
        if (go == null || jugador == null) return;
        Color color = ColorDesdeProps(jugador);
        AplicarColorDirecto(go, color);
    }

    // ── Helpers estáticos (accesibles desde UI sin instancia) ────────────────

    public static Color ObtenerColor(int actorNumber)
    {
        int idx = Mathf.Clamp(actorNumber - 1, 0, Paleta.Length - 1);
        return Paleta[idx];
    }

    public static string ObtenerNombreColor(int actorNumber)
    {
        int idx = Mathf.Clamp(actorNumber - 1, 0, NombresColor.Length - 1);
        return NombresColor[idx];
    }

    public static string ObtenerClaseCSS(int actorNumber)
    {
        int idx = Mathf.Clamp(actorNumber - 1, 0, ClasesCSS.Length - 1);
        return ClasesCSS[idx];
    }

    public static string ObtenerClasePunto(int actorNumber)
    {
        int idx = Mathf.Clamp(actorNumber - 1, 0, ClasesPunto.Length - 1);
        return ClasesPunto[idx];
    }

    // ── Photon ────────────────────────────────────────────────────────────────

    public override void OnJoinedRoom()
    {
        PublicarColorPropio();
    }

    private void PublicarColorPropio()
    {
        Color c = ObtenerColor(PhotonNetwork.LocalPlayer.ActorNumber);
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { PROP_COLOR_R, c.r },
            { PROP_COLOR_G, c.g },
            { PROP_COLOR_B, c.b },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // ── Internos ──────────────────────────────────────────────────────────────

    private static Color ColorDesdeProps(Player jugador)
    {
        var props = jugador.CustomProperties;
        if (props.TryGetValue(PROP_COLOR_R, out var r) &&
            props.TryGetValue(PROP_COLOR_G, out var g) &&
            props.TryGetValue(PROP_COLOR_B, out var b))
        {
            return new Color((float)r, (float)g, (float)b, 1f);
        }
        return ObtenerColor(jugador.ActorNumber);
    }

    private static void AplicarColorDirecto(GameObject go, Color color)
    {
        var block = new MaterialPropertyBlock();
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(block, 0);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color",     color);
            r.SetPropertyBlock(block, 0);
        }
    }
}
