using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

/// <summary>
/// Muestra los jugadores disponibles como objetivos para una carta.
/// Filtra según el alcance de la carta.
/// </summary>
public class UISeleccionObjetivo : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contenedorObjetivos;
    [SerializeField] private GameObject prefabBotonObjetivo;
    
    private CartaDefinicion cartaActual;
    private MovimientoFicha jugadorActual;
    private List<Button> botonesObjetivos = new List<Button>();
    
    private Action<MovimientoFicha> onObjetivoSeleccionado;
    private Action onCancelar;
    
    // ────────────────────────────────────────
    // MOSTRAR OBJETIVOS
    // ────────────────────────────────────────
    
    public void MostrarObjetivos(
        MovimientoFicha jugador,
        CartaDefinicion carta,
        Action<MovimientoFicha> onSeleccionar,
        Action onCancelarCallback)
    {
        jugadorActual = jugador;
        cartaActual = carta;
        onObjetivoSeleccionado = onSeleccionar;
        onCancelar = onCancelarCallback;
        
        gameObject.SetActive(true);
        RefrescarObjetivos();
    }
    
    private void RefrescarObjetivos()
    {
        // Limpiar botones anteriores
        foreach (var btn in botonesObjetivos)
            Destroy(btn.gameObject);
        botonesObjetivos.Clear();
        
        // Obtener jugadores disponibles según alcance
        List<MovimientoFicha> objetivos = ObtenerObjetivosDisponibles();
        
        foreach (var objetivo in objetivos)
        {
            CrearBotonObjetivo(objetivo);
        }
    }
    
    private List<MovimientoFicha> ObtenerObjetivosDisponibles()
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        List<MovimientoFicha> todos = new List<MovimientoFicha>(gm.ObtenerJugadoresActivos());
        
        // Si la carta es "desdeJugador", puedes elegir entre los demás
        if (cartaActual.alcance == CartaDefinicion.Alcance.DesdeJugador)
        {
            todos.RemoveAll(j => j == jugadorActual);
        }
        // Si es "Jugador", solo se aplica a ti (no mostrar selector)
        else if (cartaActual.alcance == CartaDefinicion.Alcance.Jugador)
        {
            todos.Clear();
            todos.Add(jugadorActual);
        }
        
        return todos;
    }
    
    private void CrearBotonObjetivo(MovimientoFicha objetivo)
    {
        GameObject go = Instantiate(prefabBotonObjetivo, contenedorObjetivos);
        Button btn = go.GetComponent<Button>();
        Text txt = go.GetComponentInChildren<Text>();
        
        int numJugador = FindAnyObjectByType<GameManager>()
            .ObtenerJugadoresActivos().IndexOf(objetivo) + 1;
            
        if (txt != null)
            txt.text = $"Jugador {numJugador}";
        
        btn.onClick.AddListener(() => OnSeleccionarObjetivo(objetivo));
        botonesObjetivos.Add(btn);
    }
    
    private void OnSeleccionarObjetivo(MovimientoFicha objetivo)
    {
        gameObject.SetActive(false);
        onObjetivoSeleccionado?.Invoke(objetivo);
    }
}
