using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;
/// <summary>
/// Panel principal que gestiona la UI de selección de cartas.
/// Coordina entre UICartasSeleccionables y UISeleccionObjetivo.
/// </summary>
public class UIPanelCartas : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelMano;
    [SerializeField] private GameObject panelObjetivos;
    
    [Header("Componentes")]
    [SerializeField] private UICartasSeleccionables uiCartas;
    [SerializeField] private UISeleccionObjetivo uiObjetivos;
    
    [SerializeField] private Text textoInstruccion;
    [SerializeField] private Button botonConfirmar;
    [SerializeField] private Button botonCancelar;
    
    private MovimientoFicha jugadorActual;
    private CartaDefinicion cartaEnSeleccion;
    private MovimientoFicha objetivoSeleccionado;
    
    private Action<List<CartaDefinicion>> onCartasAplicadas;
    private Action onOmitir;
    
    private List<CartaDefinicion> cartasAplicadas = new List<CartaDefinicion>();
    
    void Start()
    {
        if (botonConfirmar != null)
            botonConfirmar.onClick.AddListener(OnConfirmar);
            
        if (botonCancelar != null)
            botonCancelar.onClick.AddListener(OnCancelar);
    }
    
    // ────────────────────────────────────────
    // API PÚBLICA
    // ────────────────────────────────────────
    
    public void MostrarPanelCartas(
        MovimientoFicha jugador, 
        Action<List<CartaDefinicion>> confirmar, 
        Action omitir)
    {
        jugadorActual = jugador;
        onCartasAplicadas = confirmar;
        onOmitir = omitir;
        cartasAplicadas.Clear();
        
        gameObject.SetActive(true);
        PasarASeleccionMano();
    }
    
    // ────────────────────────────────────────
    // FLUJO INTERNO
    // ────────────────────────────────────────
    
    private void PasarASeleccionMano()
{
    if (textoInstruccion != null)
        textoInstruccion.text = "Selecciona cartas para usar (o Omitir)";
    
    panelMano.SetActive(true);
    panelObjetivos.SetActive(false);
    
    if (uiCartas != null)
    {
        uiCartas.MostrarCartas(
            jugadorActual,
            (cartasUsadas) => OnCartasSeleccionadas(cartasUsadas),
            OnOmitir
        );
    }
}

private void OnCartasSeleccionadas(List<CartaDefinicion> cartas)
{
    if (cartas.Count > 0)
    {
        CartaDefinicion primera = cartas[0];
        OnCartaSeleccionada(primera, null);
    }
    else
    {
        OnOmitir();
    }
}
    private void OnCartaSeleccionada(CartaDefinicion carta, MovimientoFicha objetivo = null)
    {
        cartaEnSeleccion = carta;
        
        if (objetivo != null)
        {
            // Ya tiene objetivo (ej: se eligió desde UI)
            AplicarCartaYContinuar(carta, objetivo);
        }
        else if (carta.PuedeCambiarseObjetivo())
        {
            // Necesita elegir objetivo
            PasarASeleccionObjetivo(carta);
        }
        else
        {
            // No necesita objetivo
            AplicarCartaYContinuar(carta, jugadorActual);
        }
    }
    
    public void PasarASeleccionObjetivo(CartaDefinicion carta)
    {
        if (textoInstruccion != null)
            textoInstruccion.text = $"Selecciona objetivo para {carta.nombreCarta}";
        
        panelMano.SetActive(false);
        panelObjetivos.SetActive(true);
        
        if (uiObjetivos != null)
        {
            uiObjetivos.MostrarObjetivos(
                jugadorActual,
                carta,
                OnObjetivoSeleccionado,
                OnCancelarSeleccion
            );
        }
    }
    
    private void OnObjetivoSeleccionado(MovimientoFicha objetivo)
    {
        objetivoSeleccionado = objetivo;
        AplicarCartaYContinuar(cartaEnSeleccion, objetivo);
    }
    
    private void AplicarCartaYContinuar(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        // Gastar energía y aplicar efecto
        SistemaEnergia energia = jugadorActual.ObtenerEnergia();
        
        if (!energia.GastarEnergia(carta.costoEnergia))
        {
            Debug.LogWarning("[UI] Energía insuficiente");
            return;
        }
        
        // Usar carta del inventario
        InventarioCartas inv = jugadorActual.ObtenerInventario();
        inv.UsarCarta(carta);
        cartasAplicadas.Add(carta);
        
        // Aplicar efecto
        EfectoCarta efectoCarta = FindAnyObjectByType<EfectoCarta>();
        if (efectoCarta != null)
            efectoCarta.AplicarCartaA(carta, objetivo);
        
        // Volver a mano
        PasarASeleccionMano();
    }
    
    private void OnCancelarSeleccion()
    {
        PasarASeleccionMano();
    }
    
    private void OnOmitir()
    {
        CerrarPanel();
        onOmitir?.Invoke();
    }
    
    private void OnConfirmar()
    {
        CerrarPanel();
        onCartasAplicadas?.Invoke(cartasAplicadas);
    }
    
    private void OnCancelar()
    {
        OnOmitir();
    }
    
    private void CerrarPanel()
    {
        gameObject.SetActive(false);
        panelMano.SetActive(false);
        panelObjetivos.SetActive(false);
    }
}