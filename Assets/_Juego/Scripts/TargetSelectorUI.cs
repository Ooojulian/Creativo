using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Ventana emergente de dos pasos para cartas de desventaja:
/// Paso 1 – Elige al jugador rival.
/// Paso 2 – Elige cuál de sus dos fichas afectar (A normal / B inversa).
/// </summary>
public class TargetSelectorUI : MonoBehaviour
{
    public static TargetSelectorUI Instance;

    [Header("Referencias")]
    public GameObject panel;
    public TextMeshProUGUI textoTitulo;      // Título que cambia en cada paso
    public Transform contenedorBotones;      // Con VerticalLayoutGroup
    public GameObject prefabBoton;

    private Action<int, bool> _callback;     // (indexRival, esFichaB)
    private int _rivalSeleccionado = -1;
    private bool _soloJugador = false;
    private readonly List<GameObject> _botonesActivos = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el selector. onObjetivoSeleccionado(indexRival, esFichaB).
    /// indexRival = índice en GameManager.todosLosJugadores.
    /// esFichaB   = true si se eligió Ficha B (inversa), false para Ficha A.
    /// soloJugador= si es true, no se muestra el paso 2 (ficha A/B) y se ejecuta directo.
    /// </summary>
    public void Mostrar(MovimientoFicha usuarioActual, bool soloJugador, Action<int, bool> onObjetivoSeleccionado)
    {
        _callback = onObjetivoSeleccionado;
        _rivalSeleccionado = -1;
        _soloJugador = soloJugador;
        MostrarPaso1Jugadores(usuarioActual);
    }

    public void Ocultar()
    {
        LimpiarBotones();
        if (panel != null) panel.SetActive(false);
    }

    // ─── PASO 1: Elegir jugador ───────────────────────────────────────────────

    private void MostrarPaso1Jugadores(MovimientoFicha usuarioActual)
    {
        LimpiarBotones();
        if (textoTitulo != null) textoTitulo.text = "¿A quién deseas atacar?";

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[TargetSelectorUI] GameManager.Instance es null.");
            _callback?.Invoke(-1, false);
            return;
        }

        bool hayRivales = false;
        for (int i = 0; i < gm.todosLosJugadores.Count; i++)
        {
            var jugador = gm.todosLosJugadores[i];
            if (jugador == usuarioActual || !jugador.gameObject.activeSelf) continue;

            int capturedIndex = i;
            var btnObj = CrearBoton($"Jugador {i + 1}");
            btnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                _rivalSeleccionado = capturedIndex;
                if (_soloJugador)
                {
                    Ocultar();
                    _callback?.Invoke(capturedIndex, false);
                }
                else
                {
                    MostrarPaso2Fichas(gm.todosLosJugadores[capturedIndex]);
                }
            });
            hayRivales = true;
        }

        if (!hayRivales)
        {
            Debug.LogWarning("[TargetSelectorUI] No hay rivales activos.");
            _callback?.Invoke(-1, false);
            return;
        }

        panel.SetActive(true);
    }

    // ─── PASO 2: Elegir ficha ────────────────────────────────────────────────

    private void MostrarPaso2Fichas(MovimientoFicha rival)
    {
        LimpiarBotones();
        if (textoTitulo != null) textoTitulo.text = "¿Qué ficha afectar?";

        // Botón Ficha A (siempre disponible)
        var btnA = CrearBoton("Ficha A  (Normal →)");
        btnA.GetComponent<Button>().onClick.AddListener(() =>
        {
            Ocultar();
            _callback?.Invoke(_rivalSeleccionado, false);
        });

        // Botón Ficha B (solo si el rival la tiene activa)
        bool tieneFichaB = rival.fichaB != null && rival.fichaB.gameObject.activeSelf;
        if (tieneFichaB)
        {
            var btnB = CrearBoton("Ficha B  (Inversa ←)");
            btnB.GetComponent<Button>().onClick.AddListener(() =>
            {
                Ocultar();
                _callback?.Invoke(_rivalSeleccionado, true);
            });
        }

        // Botón Volver al paso anterior
        var btnVolver = CrearBoton("← Volver");
        btnVolver.GetComponent<Button>().onClick.AddListener(() =>
        {
            // Buscar quién es el usuario para volver al paso 1
            var gm = GameManager.Instance;
            if (gm != null)
            {
                // Necesitamos al usuario original: lo reconstruimos desde el selector
                MostrarPaso1JugadoresDesdeRival(rival, gm);
            }
        });
    }

    /// Volver al paso 1 usando la lista de todos los jugadores sin depender del usuario original.
    private void MostrarPaso1JugadoresDesdeRival(MovimientoFicha rivalActual, GameManager gm)
    {
        LimpiarBotones();
        if (textoTitulo != null) textoTitulo.text = "¿A quién deseas atacar?";

        MovimientoFicha usuarioActual = gm.JugadorActual;

        bool hayRivales = false;
        for (int i = 0; i < gm.todosLosJugadores.Count; i++)
        {
            var jugador = gm.todosLosJugadores[i];
            if (jugador == usuarioActual || !jugador.gameObject.activeSelf) continue;

            int capturedIndex = i;
            var btnObj = CrearBoton($"Jugador {i + 1}");
            btnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                _rivalSeleccionado = capturedIndex;
                MostrarPaso2Fichas(gm.todosLosJugadores[capturedIndex]);
            });
            hayRivales = true;
        }

        if (!hayRivales)
        {
            _callback?.Invoke(-1, false);
            Ocultar();
        }
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private GameObject CrearBoton(string texto)
    {
        var btnObj = Instantiate(prefabBoton, contenedorBotones);
        _botonesActivos.Add(btnObj);

        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = texto;
        else
        {
            var legacy = btnObj.GetComponentInChildren<Text>();
            if (legacy != null) legacy.text = texto;
        }
        return btnObj;
    }

    private void LimpiarBotones()
    {
        foreach (var b in _botonesActivos)
            if (b != null) Destroy(b);
        _botonesActivos.Clear();
    }
}
