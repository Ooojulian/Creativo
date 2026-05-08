using UnityEngine;
using TMPro;

public class UICartas : MonoBehaviour
{
    public TextMeshProUGUI textoCarta;

    void Awake()
    {
        if (textoCarta != null) textoCarta.text = "";
    }

    public void MostrarRevelacion(CardSO carta)
    {
        if (textoCarta == null) return;
        if (carta == null) { textoCarta.text = ""; return; }

        textoCarta.text = $"Sacaste: {carta.cardName}";
    }

    public void MostrarResultado(CardSO carta, bool bloqueadaPorEscudo)
    {
        if (textoCarta == null) return;
        if (carta == null) { textoCarta.text = ""; return; }

        if (bloqueadaPorEscudo)
            textoCarta.text = $"Sacaste: {carta.cardName}\n(Bloqueada por Escudo)";
        else
            textoCarta.text = $"Aplicando: {carta.cardName}";
    }

    public void Limpiar()
    {
        if (textoCarta != null) textoCarta.text = "";
    }
}