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
    [Tooltip("Tiempo aproximado para alcanzar la posición objetivo (menor = más rápido)")]
    public float smoothTimePos = 0.3f;
    [Tooltip("Tiempo aproximado para alcanzar la rotación objetivo")]
    public float smoothTimeRot = 0.2f;

    // Variables internas para SmoothDamp
    private Vector3 currentVelocityPos;
    private float currentVelocityRot; // Para suavizar el ángulo si fuera necesario, pero usaremos Slerp corregido

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
                // Intentar obtener la posición fija del dado para evitar el "salto" de la animación
                Vector3 posicionEstableDado = dado.position;
                DadoLogico scriptDado = dado.GetComponent<DadoLogico>();
                if (scriptDado != null) posicionEstableDado = scriptDado.posicionFija;

                posObjetivo = posicionEstableDado + offsetDado;
                // Mirar directamente al punto estable del dado
                Vector3 dirDado = posicionEstableDado - posObjetivo;
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

        // Movimiento de posición suavizado con SmoothDamp
        transform.position = Vector3.SmoothDamp(transform.position, posObjetivo, ref currentVelocityPos, smoothTimePos);

        // Movimiento de rotación suavizado
        // Nota: Quaternion no tiene SmoothDamp nativo, usamos Slerp con un factor derivado de smoothTime
        float rotFactor = smoothTimeRot > 0 ? Time.deltaTime / smoothTimeRot : 1f;
        transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, rotFactor);
    }

    // ── API pública llamada desde DadoLogico y MovimientoFicha ───────────────

    /// <summary>Teletransporta la cámara inmediatamente a la vista general del tablero (sin suavizado).</summary>
    public void SnapAlTablero()
    {
        if (puntoVistaTablero == null) return;
        estado = Estado.Tablero;
        jugadorActivo = null;
        Vector3 centro = puntoVistaTablero.position;
        transform.position = new Vector3(centro.x, centro.y + alturaTablero, centro.z);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        currentVelocityPos = Vector3.zero;
    }

    /// <summary>Llama esto cuando empieza la animación del dado.</summary>
    public void EnfocarDado()
    {
        // Se ha desactivado la transición al dado para evitar mareos y mantener la vista general.
        // estado = Estado.Dado;
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
