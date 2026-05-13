using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System;
using Random = UnityEngine.Random;
public class DadoLogico : MonoBehaviour
{
    [Header("Referencias")]
    public MovimientoFicha jugador;
    public TextMeshProUGUI textoResultado;
    public TextMeshProUGUI textoInstruccion;

    [Header("Posición fija en el tablero (visible desde GameCamera)")]
    public Vector3 posicionFija = new Vector3(650f, 100f, 250f);
    [Tooltip("Escala del dado para que sea visible desde la vista general")]
    public float escalaDado = 5f;

    [Header("Velocidad de animación")]
    [Tooltip("Duración total del lanzamiento en segundos")]
    public float duracionLanzamiento = 1.0f;
    [Tooltip("Segundos mostrando el resultado antes de pedir confirmación")]
    public float pausaResultado = 1.5f;
    [Tooltip("Velocidad máxima de giro en grados/s")]
    public float velocidadGiro = 900f;
    [Tooltip("Altura del salto durante la animación")]
    public float alturaSalto = 40f;

    public int resultadoActual;
    private bool lanzando = false;
    private bool esperandoConfirmacion = false;
    private Rigidbody rb;
    private Vector3 ejeRotacion;
    private CamaraDirectora camaraDirectora;
<<<<<<< Updated upstream:Assets/_Juego/Scripts/DadoLogico.cs
    private Vector3 escalaOriginal;
=======
    private Action<int> onDadoLanzado;
    private MovimientoFicha jugador_asignado;
>>>>>>> Stashed changes:Assets/Scripts/DadoLogico.cs

    // -------------------------------------------------
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Guardar la escala configurada en el prefab/escena para restaurarla en OnEnable
        // escalaDado actúa como multiplicador: si es 1 usa la escala del prefab tal cual
        escalaOriginal = escalaDado > 0
            ? transform.localScale / escalaDado
            : transform.localScale;
    }

    void OnEnable()
    {
        // Solo auto-posicionar si la posición actual es el origen (no ha sido configurada)
        if (posicionFija == Vector3.zero && camaraDirectora != null && camaraDirectora.puntoVistaTablero != null)
        {
            Vector3 centro = camaraDirectora.puntoVistaTablero.position;
            posicionFija = new Vector3(centro.x + 150f, 100f, centro.z - 150f);
        }

        // Cada vez que el dado se activa se reinicia a su posición fija
        lanzando = false;
        esperandoConfirmacion = false;
        ejeRotacion = Vector3.up;
        StopAllCoroutines();
        SnapAPosicion();
        transform.localScale = escalaOriginal * escalaDado;

        if (textoResultado != null)
            textoResultado.gameObject.SetActive(false);
        if (textoInstruccion != null)
        {
            textoInstruccion.text = "ESPACIO — Tirar el dado";
            textoInstruccion.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        // Mantener posición fija mientras no lanza
        if (!lanzando && !esperandoConfirmacion)
            SnapAPosicion();

        if (Keyboard.current == null) return;

        if (!lanzando && !esperandoConfirmacion && Keyboard.current.spaceKey.wasPressedThisFrame)
            Lanzar();

        if (esperandoConfirmacion && Keyboard.current.spaceKey.wasPressedThisFrame)
            ConfirmarMovimiento();
    }

    void SnapAPosicion()
    {
        transform.position = posicionFija;
    }

    // -------------------------------------------------
    public void Lanzar()
    {
        if (!lanzando)
            StartCoroutine(AnimarLanzamiento());
    }

    IEnumerator AnimarLanzamiento()
    {
        lanzando = true;

        if (textoResultado != null)   textoResultado.gameObject.SetActive(false);
        if (textoInstruccion != null) textoInstruccion.gameObject.SetActive(false);

        // Enfocar el dado mientras gira (ahora es no-op en CamaraDirectora para mantener vista general)
        if (camaraDirectora != null) camaraDirectora.EnfocarDado();

        // Resultado elegido al inicio (sin física)
        resultadoActual = Random.Range(1, 7);

        // Eje de rotación aleatorio diferente cada lanzamiento
        ejeRotacion = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        // Segunda rotación secundaria para mayor caos visual
        Vector3 ejeSec = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        float tiempo = 0f;
        while (tiempo < duracionLanzamiento)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracionLanzamiento;

            // Giro principal que desacelera, más rotación secundaria constante
            float vel = velocidadGiro * (1f - t * t);
            transform.Rotate(ejeRotacion, vel * Time.deltaTime, Space.Self);
            transform.Rotate(ejeSec, vel * 0.4f * Time.deltaTime, Space.Self);

            // Pequeño salto al inicio de la animación
            float salto = Mathf.Sin(t * Mathf.PI * 3f) * alturaSalto * (1f - t);
            transform.position = posicionFija + Vector3.up * salto;

            yield return null;
        }

        // Posición y rotación final según resultado
        transform.position = posicionFija;
        transform.rotation = RotacionParaResultado(resultadoActual);

        Debug.Log($"Resultado del dado: {resultadoActual}");

        if (textoResultado != null)
        {
            textoResultado.text = $"Dado: {resultadoActual}";
            textoResultado.gameObject.SetActive(true);
        }

        // Sincronizar resultado a otros clientes
        if (GameSync.Instance != null)
            GameSync.Instance.SincronizarResultadoDado(resultadoActual);

        // Pausa para visualizar el resultado
        yield return new WaitForSeconds(pausaResultado);

        // Pedir confirmación para mover la ficha
        lanzando = false;
        esperandoConfirmacion = true;
        if (textoInstruccion != null)
        {
            textoInstruccion.text = "ESPACIO — Mover ficha";
            textoInstruccion.gameObject.SetActive(true);
        }
    }

    void ConfirmarMovimiento()
    {
        esperandoConfirmacion = false;

        if (textoInstruccion != null)
            textoInstruccion.gameObject.SetActive(false);

<<<<<<< Updated upstream:Assets/_Juego/Scripts/DadoLogico.cs
        if (jugador == null) { Debug.LogWarning("No hay jugador asignado al dado."); return; }

        // Ocultar dado mientras elige ficha
        gameObject.SetActive(false);

        // Mostrar panel selección ficha A/B
        var ui = FindAnyObjectByType<SeleccionFichaUI>();
        if (ui != null) ui.MostrarSeleccion(jugador);
        else EjecutarMovimiento(); // fallback si no hay UI
    }

    public void EjecutarMovimiento()
    {
        if (GameSync.Instance != null)
        {
            int indiceFicha = FindAnyObjectByType<GameManager>().todosLosJugadores.IndexOf(jugador);
            GameSync.Instance.EnviarResultadoDado(resultadoActual, indiceFicha, jugador.moverFichaB);
        }
        else
        {
            jugador.Avanzar(resultadoActual);
=======
        // Si hay callback (de GestorTurnos), usar ese
        if (onDadoLanzado != null)
        {
            onDadoLanzado?.Invoke(resultadoActual);
        }
        // Si no, fallback al jugador asignado directo
        else if (jugador != null)
        {
            jugador.Avanzar(resultadoActual);
        }
        else
        {
            Debug.LogWarning("No hay jugador asignado al dado.");
>>>>>>> Stashed changes:Assets/Scripts/DadoLogico.cs
        }
    }

    // Mapeo original del script: up=5, -up=2, forward=6, -forward=1, right=3, -right=4
    Quaternion RotacionParaResultado(int resultado)
    {
        switch (resultado)
        {
            case 5: return Quaternion.identity;
            case 2: return Quaternion.Euler(180, 0, 0);
            case 6: return Quaternion.Euler(-90, 0, 0);
            case 1: return Quaternion.Euler(90, 0, 0);
            case 3: return Quaternion.Euler(0, 0, 90);
            case 4: return Quaternion.Euler(0, 0, -90);
            default: return Quaternion.identity;
        }
    }

    public void PrepararParaLanzar(MovimientoFicha nuevoJugador, Action<int> onLanzado)
    {
        jugador_asignado = nuevoJugador;
        onDadoLanzado = onLanzado;
        
        // Mostrar UI "ESPACIO para tirar"
        if (textoInstruccion != null)
        {
            textoInstruccion.text = "ESPACIO — Tirar el dado";
            textoInstruccion.gameObject.SetActive(true);
        }
    }
}