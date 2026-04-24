using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BotónMenuFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Vector3 scalaBase;
    Image img;
    Color colorBase;

    void Start()
    {
        scalaBase = transform.localScale;
        img = GetComponent<Image>();
        colorBase = img.color; // Guarda el color original del botón
    }

    public void OnPointerEnter(PointerEventData e)
    {
        transform.localScale = scalaBase * 1.12f; // Se agranda 12%
        if (img != null) 
            img.color = new Color(colorBase.r * 1.4f, colorBase.g * 1.4f, colorBase.b * 1.4f, 1f); // Más brillante
    }

    public void OnPointerExit(PointerEventData e)
    {
        transform.localScale = scalaBase;
        if (img != null) 
            img.color = colorBase; // Vuelve al color original
    }
}