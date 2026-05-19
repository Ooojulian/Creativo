using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CartasUIVisual : MonoBehaviour
{
    [Header("Referencias")]
    public Image imagenCarta;
    public CanvasGroup canvasGroup;
    
    [Header("Animación")]
    public float duracionFade = 0.3f;
    
    void Start()
    {
        if (canvasGroup == null) 
            canvasGroup = GetComponent<CanvasGroup>();
    }
    
    public void MostrarRevelacion(CardSO carta)
    {
        if (imagenCarta == null || carta == null) return;

        imagenCarta.sprite = carta.artwork;
        imagenCarta.gameObject.SetActive(true);

        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// Fade out con delay opcional y luego limpia
    /// </summary>
    public IEnumerator FadeOutYLimpiar(float delay = 0f)
    {
        if (delay > 0f) 
            yield return new WaitForSeconds(delay);
        
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
    
    /// <summary>
    /// Limpia la UI de cartas
    /// </summary>
    public void Limpiar()
    {
        StopAllCoroutines();
        if (imagenCarta != null)
        {
            imagenCarta.sprite = null;
            imagenCarta.gameObject.SetActive(false);
        }
        if (canvasGroup != null) 
            canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Corrutina de fade in
    /// </summary>
    private IEnumerator FadeIn()
    {
        float tiempo = 0f;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, tiempo / duracionFade);
            yield return null;
        }
        if (canvasGroup != null) 
            canvasGroup.alpha = 1f;
    }
}