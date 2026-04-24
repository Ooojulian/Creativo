using UnityEngine;
using TMPro;

public class UICartas : MonoBehaviour
{
    public TextMeshProUGUI textoCarta;

    void Awake()
    {
        if (textoCarta != null) textoCarta.text = "";
    }

    public void MostrarRevelacion(TipoCarta carta)
    {
        if (textoCarta == null) return;
        if (carta == TipoCarta.Ninguna) { textoCarta.text = ""; return; }

        textoCarta.text = $"Sacaste: {carta}";
    }

    public void MostrarResultado(TipoCarta carta, bool bloqueadaPorEscudo)
    {
        if (textoCarta == null) return;
        if (carta == TipoCarta.Ninguna) { textoCarta.text = ""; return; }

        if (bloqueadaPorEscudo)
            textoCarta.text = $"Sacaste: {carta}\n(Bloqueada por Escudo)";
        else
            textoCarta.text = $"Aplicando: {carta}";
    }

    public void Limpiar()
    {
        if (textoCarta != null) textoCarta.text = "";
    }
}