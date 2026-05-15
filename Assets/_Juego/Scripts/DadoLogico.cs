using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

    [Header("Botón táctil")]
    [SerializeField] private Button _btnTirarDado;

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
    private Vector3 escalaOriginal;
    private Action<int> onDadoLanzado;

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

        escalaOriginal = escalaDado > 0
            ? transform.localScale / escalaDado
            : transform.localScale;
    }

    void OnEnable()
    {
        if (posicionFija == Vector3.zero && camaraDirectora != null && camaraDirectora.puntoVistaTablero != null)
        {
            Vector3 centro = camaraDirectora.puntoVistaTablero.position;
            posicionFija = new Vector3(centro.x + 150f, 100f, centro.z - 150f);
        }

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

        // Buscar botón si no está asignado en el Inspector
        if (_btnTirarDado == null)
        {
            var go = GameObject.Find("BtnTirarDado");
            if (go != null) _btnTirarDado = go.GetComponent<Button>();
        }
        if (_btnTirarDado != null)
        {
            _btnTirarDado.onClick.RemoveListener(OnBotonTirarDado);
            _btnTirarDado.onClick.AddListener(OnBotonTirarDado);
            _btnTirarDado.gameObject.SetActive(true);
            _btnTirarDado.interactable = true;
        }
    }

    void Update()
    {
        if (!lanzando && !esperandoConfirmacion)
            SnapAPosicion();

        if (Keyboard.current == null) return;

        // Teclado solo funciona cuando es el turno del jugador local
        if (EsMiTurno())
        {
            if (!lanzando && !esperandoConfirmacion && Keyboard.current.spaceKey.wasPressedThisFrame)
                Lanzar();

            if (esperandoConfirmacion && Keyboard.current.spaceKey.wasPressedThisFrame)
                ConfirmarMovimiento();
        }
    }

    private bool EsMiTurno()
    {
        // Sin red: siempre es el turno del jugador local
        if (!Photon.Pun.PhotonNetwork.IsConnected) return true;
        // Con red: solo si el jugador asignado al dado es el jugador local
        if (jugador == null) return false;
        var photonView = jugador.GetComponent<Photon.Pun.PhotonView>();
        return photonView != null && photonView.IsMine;
    }

    private void OnBotonTirarDado()
    {
        if (!lanzando && !esperandoConfirmacion)
            Lanzar();
        else if (esperandoConfirmacion)
            ConfirmarMovimiento();
    }

    void OnDisable()
    {
        if (_btnTirarDado != null)
            _btnTirarDado.gameObject.SetActive(false);
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

        if (textoResultado != null) textoResultado.gameObject.SetActive(false);
        if (textoInstruccion != null) textoInstruccion.gameObject.SetActive(false);

        if (camaraDirectora != null) camaraDirectora.EnfocarDado();

        resultadoActual = Random.Range(1, 7);

        ejeRotacion = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

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

            float vel = velocidadGiro * (1f - t * t);
            transform.Rotate(ejeRotacion, vel * Time.deltaTime, Space.Self);
            transform.Rotate(ejeSec, vel * 0.4f * Time.deltaTime, Space.Self);

            float salto = Mathf.Sin(t * Mathf.PI * 3f) * alturaSalto * (1f - t);
            transform.position = posicionFija + Vector3.up * salto;

            yield return null;
        }

        transform.position = posicionFija;
        transform.rotation = RotacionParaResultado(resultadoActual);

        Debug.Log($"Resultado del dado: {resultadoActual}");

        if (textoResultado != null)
        {
            textoResultado.text = $"Dado: {resultadoActual}";
            textoResultado.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(pausaResultado);

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

        if (onDadoLanzado != null)
        {
            onDadoLanzado?.Invoke(resultadoActual);
        }
        else if (jugador != null)
        {
            jugador.Avanzar(resultadoActual);
        }
        else
        {
            Debug.LogWarning("No hay jugador asignado al dado.");
        }
    }

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
        jugador = nuevoJugador;
        onDadoLanzado = onLanzado;
        
        if (textoInstruccion != null)
        {
            textoInstruccion.text = "ESPACIO — Tirar el dado";
            textoInstruccion.gameObject.SetActive(true);
        }
    }

    // -------------------------------------------------
    // PROPIEDADES Y MÉTODOS PÚBLICOS
    // -------------------------------------------------

    public bool EsperandoConfirmacion => esperandoConfirmacion;

    public void RelanzarDado()
    {
        if (!lanzando && esperandoConfirmacion)
        {
            esperandoConfirmacion = false;
            Lanzar();
        }
    }

    public void EjecutarMovimiento()
    {
        ConfirmarMovimiento();
    }
}