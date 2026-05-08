using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CartasUIVisual : MonoBehaviour
{
    [Header("Imagen de la carta")]
    public Image imagenCarta;

    [Header("Animación")]
    public CanvasGroup canvasGroup; // Para fade in/out
    public float duracionFade = 0.3f;

    void Start()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Limpiar();
    }

    public void MostrarRevelacion(CardSO carta)
    {
        if (imagenCarta == null || carta == null) return;

        imagenCarta.sprite = carta.artwork;
        imagenCarta.gameObject.SetActive(true);

        // Fade in suave
        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }

    public void MostrarResultado(CardSO carta, bool bloqueadaPorEscudo)
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