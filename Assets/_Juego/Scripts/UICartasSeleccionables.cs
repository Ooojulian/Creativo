using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

/// <summary>
/// Muestra las cartas en mano del jugador como botones seleccionables.
/// Maneja la selección, validación de energía y selección de objetivo.
/// </summary>
public class UICartasSeleccionables : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contenedorCartas;
    [SerializeField] private GameObject prefabCartaBoton;
    
    [SerializeField] private Button botonOmitir;
    [SerializeField] private Button botonTerminarTurno;
    
    [SerializeField] private Text textoEnergia;
    
    private MovimientoFicha jugadorActual;
    private SistemaEnergia sistemaEnergia;
    private InventarioCartas inventario;
    private List<Button> botonesCartas = new List<Button>();
    
    private CartaDefinicion cartaSeleccionada;
    private Action<List<CartaDefinicion>> onConfirmar;
    private Action onOmitir;
    
    void Start()
    {
        if (botonOmitir != null)
            botonOmitir.onClick.AddListener(OnOmitir);
            
        if (botonTerminarTurno != null)
            botonTerminarTurno.onClick.AddListener(OnTerminarTurno);
    }
    
    // ────────────────────────────────────────
    // MOSTRAR/ACTUALIZAR CARTAS
    // ────────────────────────────────────────
    
    public void MostrarCartas(MovimientoFicha jugador, Action<List<CartaDefinicion>> confirmar, Action omitir)
    {
        jugadorActual = jugador;
        sistemaEnergia = jugador.ObtenerEnergia();
        inventario = jugador.ObtenerInventario();
        
        onConfirmar = confirmar;
        onOmitir = omitir;
        
        gameObject.SetActive(true);
        RefrescarCartas();
        ActualizarTextoEnergia();
    }
    
    private void RefrescarCartas()
    {
        // Limpiar botones anteriores
        foreach (var btn in botonesCartas)
            Destroy(btn.gameObject);
        botonesCartas.Clear();
        
        // Crear botón por cada carta
        List<CartaDefinicion> mano = inventario.ObtenerMano();
        foreach (var carta in mano)
        {
            CrearBotonCarta(carta);
        }
    }
    
    private void CrearBotonCarta(CartaDefinicion carta)
    {
        GameObject go = Instantiate(prefabCartaBoton, contenedorCartas);
        Button btn = go.GetComponent<Button>();
        Image img = go.GetComponent<Image>();
        Text txt = go.GetComponentInChildren<Text>();
        
        if (img != null && carta.icono != null)
            img.sprite = carta.icono;
            
        if (txt != null)
            txt.text = $"{carta.nombreCarta}\n({carta.costoEnergia} E)";
        
        bool puedeUsar = sistemaEnergia.PuedeGastar(carta.costoEnergia);
        btn.interactable = puedeUsar;
        
        btn.onClick.AddListener(() => OnSeleccionarCarta(carta));
        botonesCartas.Add(btn);
    }
    
    private void ActualizarTextoEnergia()
    {
        if (textoEnergia != null)
        {
            int actual = sistemaEnergia.ObtenerEnergiaActual();
            int maximo = sistemaEnergia.ObtenerEnergiaMaxima();
            textoEnergia.text = $"Energía: {actual}/{maximo}";
        }
    }
    
    // ────────────────────────────────────────
    // SELECCIÓN DE CARTAS
    // ────────────────────────────────────────
    
    private void OnSeleccionarCarta(CartaDefinicion carta)
    {
        cartaSeleccionada = carta;
        
        // Validar energía
        if (!sistemaEnergia.PuedeGastar(carta.costoEnergia))
        {
            Debug.LogWarning($"[UI] Energía insuficiente para {carta.nombreCarta}");
            return;
        }
        
        // Si necesita objetivo, mostrar selector
        if (carta.PuedeCambiarseObjetivo())
    {
        // ← CAMBIAR ESTO:
        UIPanelCartas panelCartas = FindAnyObjectByType<UIPanelCartas>();
        if (panelCartas != null)
            panelCartas.PasarASeleccionObjetivo(carta);
    }
    else
    {
        AplicarCarta(carta, jugadorActual);
    }
}
    
    private void MostrarSeleccionObjetivo(CartaDefinicion carta)
    {
        // Implementar UISeleccionObjetivo
        Debug.Log($"[UI] Mostrando selector de objetivo para {carta.nombreCarta}");
        // TODO: Llamar a UISeleccionObjetivo.Mostrar()
    }
    
    private void AplicarCarta(CartaDefinicion carta, MovimientoFicha objetivo)
    {
        // Validar energía nuevamente
        if (!sistemaEnergia.GastarEnergia(carta.costoEnergia))
            return;
        
        // Usar carta del inventario
        inventario.UsarCarta(carta);
        
        // Aplicar efecto
        EfectoCarta efectoCarta = FindAnyObjectByType<EfectoCarta>();
        if (efectoCarta != null)
            efectoCarta.AplicarCartaA(carta, objetivo);
        
        // Actualizar UI
        ActualizarTextoEnergia();
        RefrescarCartas();
    }
    
    private void OnOmitir()
    {
        gameObject.SetActive(false);
        onOmitir?.Invoke();
    }
    
    private void OnTerminarTurno()
    {
        gameObject.SetActive(false);
        onConfirmar?.Invoke(new List<CartaDefinicion>());
    }
}