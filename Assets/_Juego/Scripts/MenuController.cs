using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject panelMenu;
    public GameObject fondoMenu;

    public void IniciarJuego(int numJugadores)
    {
        Debug.Log("Iniciando juego con " + numJugadores + " jugadores");
        
        // Desactiva el menú
        if (panelMenu != null)
            panelMenu.SetActive(false);
        
        // Desactiva el fondo
        if (fondoMenu != null)
            fondoMenu.SetActive(false);
    }
}