using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHandUI : MonoBehaviour
{
    public GameManager gameManager;
    public Transform containerMano;
    public Transform containerReserva;
    public GameObject prefabCarta;

    void Update()
    {
        // Esto es poco eficiente para producción, pero sirve para prototipar
        // Idealmente usar eventos cuando cambie el inventario
        ActualizarUI();
    }

    public void ActualizarUI()
    {
        // Limpiar
        foreach (Transform child in containerMano) Destroy(child.gameObject);
        foreach (Transform child in containerReserva) Destroy(child.gameObject);

        MovimientoFicha jugador = gameManager.JugadorActual; 

        if (jugador == null || jugador.inventario == null) return;

        foreach (var card in jugador.inventario.hand)
        {
            GameObject obj = Instantiate(prefabCarta, containerMano);
            obj.GetComponent<Image>().sprite = card.artwork;
            obj.GetComponent<Button>().onClick.AddListener(() => CardPlayUI.Instance.Mostrar(card, jugador));
        }

        foreach (var card in jugador.inventario.reserve)
        {
            GameObject obj = Instantiate(prefabCarta, containerReserva);
            obj.GetComponent<Image>().sprite = card.artwork;
            // Las de reserva no se pueden clickear para usar, se activan solas
            obj.GetComponent<Button>().interactable = false; 
        }
    }
}
