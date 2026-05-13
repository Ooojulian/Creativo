// CartaVisualCasilla.cs (NUEVO - MÍNIMO)
using UnityEngine;
using UnityEngine.UI;

public class CartaVisualCasilla : MonoBehaviour
{
    public Image imagenCarta;
<<<<<<< Updated upstream:Assets/_Juego/Scripts/CartasUIVisual.cs

    [Header("Animación")]
    public CanvasGroup canvasGroup; // Para fade in/out
=======
    public CanvasGroup canvasGroup;
>>>>>>> Stashed changes:Assets/Scripts/CartasUIVisual.cs
    public float duracionFade = 0.3f;
    
    void Start()
    {
        if (canvasGroup == null) 
            canvasGroup = GetComponent<CanvasGroup>();
    }
<<<<<<< Updated upstream:Assets/_Juego/Scripts/CartasUIVisual.cs

    public void MostrarRevelacion(CardSO carta)
    {
        if (imagenCarta == null || carta == null) return;

        imagenCarta.sprite = carta.artwork;
=======
    
    public void MostrarRevelacion(CartaDefinicion carta)
    {
        if (imagenCarta == null || carta == null) return;
        
        imagenCarta.sprite = carta.icono;
>>>>>>> Stashed changes:Assets/Scripts/CartasUIVisual.cs
        imagenCarta.gameObject.SetActive(true);
        
        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }
<<<<<<< Updated upstream:Assets/_Juego/Scripts/CartasUIVisual.cs

    public void MostrarResultado(CardSO carta, bool bloqueadaPorEscudo)
=======
    
    public System.Collections.IEnumerator FadeOutYLimpiar(float delay = 0f)
>>>>>>> Stashed changes:Assets/Scripts/CartasUIVisual.cs
    {
        if (delay > 0f) 
            yield return new WaitForSeconds(delay);
        
        float tiempo = 0f;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, tiempo / duracionFade);
            yield return null;
        }
        
        Limpiar();
    }
    
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
<<<<<<< Updated upstream:Assets/_Juego/Scripts/CartasUIVisual.cs

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
=======
    
>>>>>>> Stashed changes:Assets/Scripts/CartasUIVisual.cs
    System.Collections.IEnumerator FadeIn()
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