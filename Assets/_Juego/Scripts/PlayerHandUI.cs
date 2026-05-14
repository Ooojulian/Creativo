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

        if (jugador == null) return;

        var inventario = jugador.ObtenerInventario();
        if (inventario == null) return;

        // Obtener cartas en mano usando el getter público
        List<CartaDefinicion> cartasEnMano = inventario.ObtenerMano();
        
        if (cartasEnMano != null)
        {
            foreach (var card in cartasEnMano)
            {
                GameObject obj = Instantiate(prefabCarta, containerMano);
                
                // Buscar componente Image y asignar sprite de la carta
                Image imgComponent = obj.GetComponent<Image>();
                if (imgComponent != null && card.icono != null)
                    imgComponent.sprite = card.icono;
                
                // Agregar listener al botón
                Button btnComponent = obj.GetComponent<Button>();
                if (btnComponent != null)
                    btnComponent.onClick.AddListener(() => CardPlayUI.Instance.Mostrar(card, jugador));
            }
        }

        // Para reserva, por ahora no mostrar (no existe getter en InventarioCartas)
        // TODO: Implementar sistema de cartas en reserva si es necesario
    }
}