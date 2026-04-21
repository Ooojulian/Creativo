using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CartasUIVisual : MonoBehaviour
{
    [Header("Imagen de la carta")]
    public Image imagenCarta;

    [Header("Diccionario de imágenes (arrastra aquí desde el Inspector)")]
    public Sprite spriteAvanceRapido;
    public Sprite spriteEscudo;
    public Sprite spriteDobleTiro;
    public Sprite spriteRetroceso;
    public Sprite spritePierdeTurno;
    public Sprite spriteIntercambio;

    [Header("Animación")]
    public CanvasGroup canvasGroup; // Para fade in/out
    public float duracionFade = 0.3f;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Limpiar();
    }

    public void MostrarRevelacion(TipoCarta carta)
    {
        if (imagenCarta == null) return;

        Sprite sprite = ObtenerSprite(carta);

        if (sprite == null)
        {
            Limpiar();
            return;
        }

        imagenCarta.sprite = sprite;
        imagenCarta.gameObject.SetActive(true);

        // Fade in suave
        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }

    public void MostrarResultado(TipoCarta carta, bool bloqueadaPorEscudo)
    {
        // Mantener la misma imagen, solo cambiar opacity si está bloqueada
        if (bloqueadaPorEscudo && canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    public void Limpiar()
    {
        StopAllCoroutines(); // Cancela cualquier fade activo antes de limpiar

        if (imagenCarta != null)
        {
            imagenCarta.sprite = null;
            imagenCarta.gameObject.SetActive(false);
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Espera 'delay' segundos, luego hace fade out y limpia la UI.
    /// Llamar con StartCoroutine o yield return StartCoroutine desde MovimientoFicha.
    /// </summary>
    public System.Collections.IEnumerator FadeOutYLimpiar(float delay = 0f)
    {
        if (delay > 0f)
            yield return new UnityEngine.WaitForSeconds(delay);

        float tiempo = 0f;
        float alphaInicio = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(alphaInicio, 0f, tiempo / duracionFade);
            yield return null;
        }

        Limpiar();
    }

    // Obtener el sprite según el tipo de carta
    private Sprite ObtenerSprite(TipoCarta carta)
    {
        return carta switch
        {
            TipoCarta.AvanceRapido  => spriteAvanceRapido,
            TipoCarta.Escudo        => spriteEscudo,
            TipoCarta.DobleTiro     => spriteDobleTiro,
            TipoCarta.Retroceso     => spriteRetroceso,
            TipoCarta.PierdeTurno   => spritePierdeTurno,
            TipoCarta.Intercambio   => spriteIntercambio,
            _                       => null
        };
    }

    // Corrutina de fade in
    System.Collections.IEnumerator FadeIn()
    {
        float tiempo = 0f;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, tiempo / duracionFade);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}