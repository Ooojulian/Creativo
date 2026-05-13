using UnityEngine;
using System.Collections.Generic;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject fondoMenu;
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject player3;
    [SerializeField] private GameObject player4;
    
    private GestorTurnos gestorTurnos;

    void Start()
    {
        gestorTurnos = FindAnyObjectByType<GestorTurnos>();
    }

    public void IniciarJuego(int numJugadores)
    {
        Debug.Log("Iniciando juego con " + numJugadores + " jugadores");
        
        if (panelMenu != null) panelMenu.SetActive(false);
        if (fondoMenu != null) fondoMenu.SetActive(false);
        
        ConfigurarJugadores(numJugadores);
        IniciarPartida();
    }
    
    private void ConfigurarJugadores(int numJugadores)
    {
        if (player1 != null) player1.SetActive(false);
        if (player2 != null) player2.SetActive(false);
        if (player3 != null) player3.SetActive(false);
        if (player4 != null) player4.SetActive(false);
        
        List<MovimientoFicha> jugadoresActivos = new List<MovimientoFicha>();
        
        switch (numJugadores)
        {
            case 2:
                player1.SetActive(true);
                player2.SetActive(true);
                jugadoresActivos.Add(player1.GetComponent<MovimientoFicha>());
                jugadoresActivos.Add(player2.GetComponent<MovimientoFicha>());
                break;
            case 3:
                player1.SetActive(true);
                player2.SetActive(true);
                player3.SetActive(true);
                jugadoresActivos.Add(player1.GetComponent<MovimientoFicha>());
                jugadoresActivos.Add(player2.GetComponent<MovimientoFicha>());
                jugadoresActivos.Add(player3.GetComponent<MovimientoFicha>());
                break;
            case 4:
                player1.SetActive(true);
                player2.SetActive(true);
                player3.SetActive(true);
                player4.SetActive(true);
                jugadoresActivos.Add(player1.GetComponent<MovimientoFicha>());
                jugadoresActivos.Add(player2.GetComponent<MovimientoFicha>());
                jugadoresActivos.Add(player3.GetComponent<MovimientoFicha>());
                jugadoresActivos.Add(player4.GetComponent<MovimientoFicha>());
                break;
        }
        
        if (gestorTurnos != null)
            gestorTurnos.AsignarJugadores(jugadoresActivos);
    }
    
    private void IniciarPartida()
{
    // Desactivar botón de omitir
    GameObject botonOmitir = GameObject.Find("Jugar Button");
    if (botonOmitir != null)
        botonOmitir.SetActive(false);
    
    // Iniciar turno UNA SOLA VEZ
    if (gestorTurnos != null)
        gestorTurnos.IniciarTurno();
}
}