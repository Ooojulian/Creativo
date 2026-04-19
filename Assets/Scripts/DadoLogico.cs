using UnityEngine;
using UnityEngine.InputSystem;

public class DadoLogico : MonoBehaviour
{
    public int resultadoActual;
    private Rigidbody rb;
    private bool estoyLanzando = false;
    private float tiempoSinMovimiento = 0f;

    [Header("Referencias de Juego")]
    public MovimientoFicha jugador;

    [Header("Posicion inicial")]
    public Vector3 posicionInicial = new Vector3(0f, 4f, 0f);

    [Header("Fuerza de lanzamiento")]
    public float fuerzaHorizontal = 8f;
    public float fuerzaAbajo = 12f;
    public float fuerzaRotacion = 20f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1f;
        Resetear();
    }

    void Resetear()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = posicionInicial;
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !estoyLanzando)
        {
            Lanzar();
        }

        if (estoyLanzando && rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
        {
            tiempoSinMovimiento += Time.deltaTime;
            if (tiempoSinMovimiento > 0.8f)
            {
                CheckDiceResult();
                estoyLanzando = false;
            }
        }
    }

    public void Lanzar()
    {
        Resetear();
        estoyLanzando = true;
        tiempoSinMovimiento = 0f;
        resultadoActual = 0;

        float x = Random.Range(-fuerzaHorizontal, fuerzaHorizontal);
        float z = Random.Range(-fuerzaHorizontal, fuerzaHorizontal);
        float y = -fuerzaAbajo;

        rb.linearVelocity = new Vector3(x, y, z);

        rb.angularVelocity = new Vector3(
            Random.Range(-fuerzaRotacion, fuerzaRotacion),
            Random.Range(-fuerzaRotacion, fuerzaRotacion),
            Random.Range(-fuerzaRotacion, fuerzaRotacion)
        );
    }

    void CheckDiceResult()
    {
        Vector3[] ejesLocales = {
            transform.up,
            -transform.up,
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };
        int[] caras = { 5, 2, 6, 1, 3, 4 };

        int mejorIndice = 0;
        float mejorDot = -1f;

        for (int i = 0; i < ejesLocales.Length; i++)
        {
            float d = Vector3.Dot(ejesLocales[i], Vector3.up);
            if (d > mejorDot)
            {
                mejorDot = d;
                mejorIndice = i;
            }
        }

        resultadoActual = caras[mejorIndice];
        Debug.Log($"Resultado físico detectado: {resultadoActual}");

        if (jugador != null)
        {
            jugador.Avanzar(resultadoActual);
        }
        else
        {
            Debug.LogWarning("¡Ojo! No has asignado al Jugador en el Inspector del Dado.");
        }
    }
}
