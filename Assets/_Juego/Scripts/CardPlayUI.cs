using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CardPlayUI : MonoBehaviour
{
    public static CardPlayUI Instance;

    public GameObject panel;
    public TextMeshProUGUI textoCarta;
    public Button botonUsar;
    public Button botonGuardar;
    public Button botonCerrar;   // Siempre visible, permite cancelar sin gastar la carta

    private CardSO cartaSeleccionada;
    private MovimientoFicha jugadorActual;
    private bool _esDesdeReserva = false;  // true cuando la carta viene de la reserva

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        // Enlazar botones programáticamente para que siempre funcionen sin tener que configurarlos en el Inspector de Unity
        if (botonUsar != null)
        {
            botonUsar.onClick.RemoveAllListeners();
            botonUsar.onClick.AddListener(OnClickUsar);
        }
        if (botonGuardar != null)
        {
            botonGuardar.onClick.RemoveAllListeners();
            botonGuardar.onClick.AddListener(OnClickGuardar);
        }
        if (botonCerrar != null)
        {
            botonCerrar.onClick.RemoveAllListeners();
            botonCerrar.onClick.AddListener(OnClickCerrar);
        }
    }

    /// Abre el panel desde la MANO del jugador (muestra Usar + Guardar).
    public void Mostrar(CardSO card, MovimientoFicha jugador)
    {
        _esDesdeReserva = false;
        if (botonGuardar != null) botonGuardar.gameObject.SetActive(true);
        Debug.Log($"[CardPlayUI] Mostrar() carta de MANO: {card.cardName}");
        MostrarPanel(card, jugador);
    }

    /// Abre el panel desde la RESERVA del jugador (solo muestra Usar).
    public void MostrarDesdeReserva(CardSO card, MovimientoFicha jugador)
    {
        _esDesdeReserva = true;
        if (botonGuardar != null) botonGuardar.gameObject.SetActive(false);
        Debug.Log($"[CardPlayUI] MostrarDesdeReserva() carta de RESERVA: {card.cardName}");
        MostrarPanel(card, jugador);
    }

    private void MostrarPanel(CardSO card, MovimientoFicha jugador)
    {
        cartaSeleccionada = card;
        jugadorActual = jugador;

        var energia = jugador.GetComponent<EnergiaController>();
        int energiaActual = energia != null ? energia.EnergiaActual : 0;

        if (jugador.silencioActivo)
        {
            textoCarta.text = $"<color=red>¡ESTÁS SILENCIADO!</color>\nNo puedes usar cartas este turno.";
            botonUsar.interactable = false;
            if (botonGuardar != null) botonGuardar.interactable = false;
        }
        else
        {
            string origen = _esDesdeReserva ? "(Reserva)" : "";
            textoCarta.text = $"¿Qué deseas hacer con {card.cardName}? {origen}\n" +
                              $"<size=80%>Costo: {card.costoEnergia} Energía (Tienes: {energiaActual})</size>";

            botonUsar.interactable = energia != null && energia.TieneEnergia(card.costoEnergia);

            if (!_esDesdeReserva && botonGuardar != null)
                botonGuardar.interactable = jugador.inventario.reserve.Count < jugador.inventario.maxReserveSize;
        }

        panel.SetActive(true);
    }

    public void OnClickUsar()
    {
        if (jugadorActual == null || cartaSeleccionada == null) return;

        panel.SetActive(false);

        // Si es una carta de desventaja, pedir al jugador que elija el objetivo
        if (EsCartaDeDesventaja(cartaSeleccionada))
        {
            if (TargetSelectorUI.Instance != null)
            {
                bool soloJugador = cartaSeleccionada.type != CardType.Retroceso;
                TargetSelectorUI.Instance.Mostrar(jugadorActual, soloJugador, (indexRival, esFichaB) =>
                {
                    EjecutarConRival(indexRival, esFichaB);
                });
            }
            else
            {
                Debug.LogWarning("[CardPlayUI] TargetSelectorUI.Instance es null. Ejecutando sin selector. " +
                                 "Agrega el objeto TargetSelectorUI al Canvas de tu escena.");
                EjecutarConRival(-1, false);
            }
            return;
        }

        // Cartas de ventaja: ejecutar directamente sin selector
        EjecutarConRival(-1, false);
    }

    /// Devuelve true si la carta afecta a un rival y requiere selección de objetivo.
    private bool EsCartaDeDesventaja(CardSO card)
    {
        return card.type == CardType.Retroceso   ||
               card.type == CardType.PierdeTurno ||
               card.type == CardType.Intercambio ||
               card.type == CardType.Maldicion   ||
               card.type == CardType.Ruptura      ||
               card.type == CardType.Silencio;
    }

    /// Ejecuta la carta contra el rival con índice indexRival (-1 = auto-detectar).
    /// esFichaB indica qué token del rival es el objetivo.
    private void EjecutarConRival(int indexRival, bool esFichaB = false)
    {
        int randVal1 = -1;
        int randVal2 = -1;

        // Generar valores aleatorios localmente según el tipo de carta
        if (cartaSeleccionada.type == CardType.RoboArcano)
        {
            var pool = CardManager.Instance?.ObtenerCartaAleatoria();
            if (pool != null) randVal1 = (int)pool.type;
        }
        else if (cartaSeleccionada.type == CardType.VisionDelSabio)
        {
            var c1 = CardManager.Instance?.ObtenerCartaVentaja();
            if (c1 != null) randVal1 = (int)c1.type;
            var c2 = CardManager.Instance?.ObtenerCartaVentaja();
            if (c2 != null) randVal2 = (int)c2.type;
        }
        else if (cartaSeleccionada.type == CardType.Ruptura)
        {
            MovimientoFicha rival = null;
            if (indexRival >= 0 && GameManager.Instance != null && indexRival < GameManager.Instance.todosLosJugadores.Count)
                rival = GameManager.Instance.todosLosJugadores[indexRival];
            else if (GameManager.Instance != null)
            {
                foreach (var j in GameManager.Instance.todosLosJugadores)
                    if (j != jugadorActual && j.gameObject.activeSelf) { rival = j; break; }
            }

            if (rival != null && rival.inventario != null && rival.inventario.hand.Count > 0)
            {
                int cant = cartaSeleccionada.valor1 > 0 ? cartaSeleccionada.valor1 : 2;
                int count = rival.inventario.hand.Count;
                if (cant > 0)
                {
                    randVal1 = UnityEngine.Random.Range(0, count);
                }
                if (cant > 1 && count > 1)
                {
                    do {
                        randVal2 = UnityEngine.Random.Range(0, count);
                    } while (randVal2 == randVal1);
                }
            }
        }
        else if (cartaSeleccionada.type == CardType.Intercambio)
        {
            if (GameManager.Instance != null)
            {
                var candidatos = new List<int>();
                for (int i = 0; i < GameManager.Instance.todosLosJugadores.Count; i++)
                {
                    var j = GameManager.Instance.todosLosJugadores[i];
                    if (j != jugadorActual && j.gameObject.activeSelf) candidatos.Add(i);
                }
                if (candidatos.Count > 0)
                {
                    randVal1 = candidatos[UnityEngine.Random.Range(0, candidatos.Count)];
                }
            }
        }

        // ─── Con red: RPC con índice de rival, token y valores deterministas incluidos
        if (Photon.Pun.PhotonNetwork.InRoom && GameSync.Instance != null)
        {
            int idx = GameManager.Instance?.todosLosJugadores.IndexOf(jugadorActual) ?? -1;
            if (idx >= 0)
            {
                if (_esDesdeReserva)
                    GameSync.Instance.SincronizarUsarCartaDeReserva(idx, (int)cartaSeleccionada.type, indexRival, esFichaB, randVal1, randVal2);
                else
                    GameSync.Instance.SincronizarUsarCarta(idx, (int)cartaSeleccionada.type, indexRival, esFichaB, randVal1, randVal2);
            }
            return;
        }

        // ─── Sin red: ejecutar localmente con el rival resuelto
        MovimientoFicha rivalObjetivo = null;
        if (indexRival >= 0 && GameManager.Instance != null &&
            indexRival < GameManager.Instance.todosLosJugadores.Count)
            rivalObjetivo = GameManager.Instance.todosLosJugadores[indexRival];

        if (_esDesdeReserva)
        {
            jugadorActual.inventario.RemoveFromReserve(cartaSeleccionada);
            CardManager.Instance.EjecutarEfectoInmediato(cartaSeleccionada, jugadorActual, rivalObjetivo, esFichaB, randVal1, randVal2);
        }
        else
        {
            var energia = jugadorActual.GetComponent<EnergiaController>();
            if (energia != null && energia.GastarEnergia(cartaSeleccionada.costoEnergia))
            {
                jugadorActual.inventario.RemoveFromHand(cartaSeleccionada);
                CardManager.Instance.EjecutarEfectoInmediato(cartaSeleccionada, jugadorActual, rivalObjetivo, esFichaB, randVal1, randVal2);
            }
            else
            {
                Debug.Log("No tienes suficiente energía.");
            }
        }
    }


    public void OnClickGuardar()
    {
        if (jugadorActual == null || cartaSeleccionada == null) return;

        // ─── Con red: sincronizar por RPC
        if (Photon.Pun.PhotonNetwork.InRoom && GameSync.Instance != null)
        {
            int idx = GameManager.Instance?.todosLosJugadores.IndexOf(jugadorActual) ?? -1;
            if (idx >= 0)
            {
                panel.SetActive(false);
                GameSync.Instance.SincronizarGuardarCarta(idx, (int)cartaSeleccionada.type);
            }
            return;
        }

        // ─── Sin red: comportamiento local original
        panel.SetActive(false);
        jugadorActual.inventario.SaveToReserve(cartaSeleccionada);
    }


    public void OnClickCerrar()
    {
        panel.SetActive(false);
    }
}
