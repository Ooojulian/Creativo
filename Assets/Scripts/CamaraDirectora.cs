using UnityEngine;

public class CamaraDirectora : MonoBehaviour
{
    [Header("Objetivo del dado")]
    public Transform dado;

    [Header("Vista general del tablero (top-down)")]
    [Tooltip("Transform vacío centrado sobre el tablero. Solo se usa su posición XZ; la cámara siempre mira hacia abajo en Y.")]
    public Transform puntoVistaTablero;
    [Tooltip("Altura de la cámara sobre el tablero en vista general")]
    public float alturaTablero = 600f;

    [Header("Suavizado")]
    public float velocidadPos = 3f;
    public float velocidadRot = 3f;

    [Header("Offset al enfocar el dado (relativo a la posición del dado en mundo)")]
    [Tooltip("Y = altura sobre el dado, Z = distancia hacia atrás. Ajusta hasta que el dado quede bien encuadrado.")]
    public Vector3 offsetDado = new Vector3(0f, 150f, -200f);

    [Header("Offset al seguir al jugador")]
    public Vector3 offsetJugador = new Vector3(0f, 120f, -160f);

    private enum Estado { Tablero, Dado, Jugador }
    private Estado estado = Estado.Tablero;
    private Transform jugadorActivo;

    void LateUpdate()
    {
        Vector3 posObjetivo;
        Quaternion rotObjetivo;

        switch (estado)
        {
            case Estado.Dado when dado != null:
                posObjetivo = dado.position + offsetDado;
                // Mirar directamente al dado
                Vector3 dirDado = dado.position - posObjetivo;
                rotObjetivo = dirDado != Vector3.zero
                    ? Quaternion.LookRotation(dirDado)
                    : transform.rotation;
                break;

            case Estado.Jugador when jugadorActivo != null:
                posObjetivo = jugadorActivo.position + offsetJugador;
                Vector3 dirJugador = jugadorActivo.position - posObjetivo;
                rotObjetivo = dirJugador != Vector3.zero
                    ? Quaternion.LookRotation(dirJugador)
                    : transform.rotation;
                break;

            default:
                if (puntoVistaTablero != null)
                {
                    // Posición centrada sobre el tablero a alturaTablero, mirando recto hacia abajo
                    Vector3 centro = puntoVistaTablero.position;
                    posObjetivo = new Vector3(centro.x, centro.y + alturaTablero, centro.z);
                    rotObjetivo = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    return;
                }
                break;
        }

        transform.position = Vector3.Lerp(transform.position, posObjetivo, velocidadPos * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, velocidadRot * Time.deltaTime);
    }

    // ── API pública llamada desde DadoLogico y MovimientoFicha ───────────────

    /// <summary>Llama esto cuando empieza la animación del dado.</summary>
    public void EnfocarDado()
    {
        estado = Estado.Dado;
    }

    /// <summary>Llama esto cuando el jugador empieza a moverse.</summary>
    public void SeguirJugador(Transform jugador)
    {
        jugadorActivo = jugador;
        estado = Estado.Jugador;
    }

    /// <summary>Llama esto cuando termina el turno (vuelta a vista general).</summary>
    public void VolverAlTablero()
    {
        jugadorActivo = null;
        estado = Estado.Tablero;
    }
}
