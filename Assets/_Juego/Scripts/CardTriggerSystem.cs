using UnityEngine;
using System.Collections.Generic;

public class CardTriggerSystem : MonoBehaviour
{
    public static CardTriggerSystem Instance;
    public GameManager gameManager;

    void Awake()
    {
        Instance = this;
    }

    // Llamado cuando un jugador es adelantado
    public void CheckOvertake(MovimientoFicha adelantado, MovimientoFicha adelantador)
    {
        TryTrigger(adelantado, CardType.Retroceso, adelantador);
    }

    // Llamado al inicio del turno
    public void CheckTurnStart(MovimientoFicha jugador)
    {
        TryTrigger(jugador, CardType.Intercambio);
    }

    // Llamado cuando un rival hace algo "especial"
    public void CheckSpecialAction(MovimientoFicha rival)
    {
        // Revisar todos los otros jugadores por PierdeTurno
        foreach (var j in gameManager.todosLosJugadores)
        {
            if (j != rival) TryTrigger(j, CardType.PierdeTurno, rival);
        }
    }

    // Llamado cuando alguien juega una carta contra otro
    public void CheckCardPlayedAgainst(MovimientoFicha objetivo, MovimientoFicha usuario)
    {
        TryTrigger(objetivo, CardType.Ruptura, usuario);
    }

    // Llamado al final del turno
    public void CheckTurnEnd(MovimientoFicha jugador, int cartasAlEmpezar)
    {
        var inv = jugador.GetComponent<PlayerInventory>();
        if (inv == null) return;
        if (inv.hand.Count < cartasAlEmpezar)
        {
            TryTrigger(jugador, CardType.RoboArcano);
        }
    }

    // Llamado cuando alguien roba carta
    public void CheckCardDrawn(MovimientoFicha jugador, CardSO card)
    {
        var invJugador = jugador.GetComponent<PlayerInventory>();
        if (invJugador == null) return;
        foreach (var j in gameManager.todosLosJugadores)
        {
            if (j != jugador)
            {
                if (invJugador.hand.Count >= 5)
                    TryTrigger(j, CardType.Fatiga, jugador);
            }
        }
    }

    // Llamado durante el movimiento
    public void CheckNearGoal(MovimientoFicha jugador)
    {
        var casillas = jugador.ObtenerCasillas();
        if (casillas == null) return;
        int casillasRestantes = casillas.Count - 1 - jugador.indiceActual;
        if (casillasRestantes <= 3)
        {
            TryTrigger(jugador, CardType.AvanceRapido);
        }
    }

    private void TryTrigger(MovimientoFicha usuario, CardType type, MovimientoFicha rival = null)
    {
        var inv = usuario.GetComponent<PlayerInventory>();
        if (inv == null) return;
        CardSO cardToTrigger = inv.reserve.Find(c => c.type == type);

        if (cardToTrigger != null)
        {
            Debug.Log($"¡Trigger de reserva! {cardToTrigger.cardName} de {usuario.name}");

            // En red: sincronizar via RPC para que todos ejecuten el mismo efecto
            if (Photon.Pun.PhotonNetwork.InRoom && GameSync.Instance != null && gameManager != null)
            {
                int idxUsuario = gameManager.todosLosJugadores.IndexOf(usuario);
                int idxRival   = rival != null ? gameManager.todosLosJugadores.IndexOf(rival) : -1;
                int randVal1 = -1, randVal2 = -1;

                if (type == CardType.Ruptura && rival != null)
                {
                    var invRival = rival.GetComponent<PlayerInventory>();
                    if (invRival != null && invRival.hand.Count > 0)
                    {
                        randVal1 = UnityEngine.Random.Range(0, invRival.hand.Count);
                        if (invRival.hand.Count > 1)
                            do { randVal2 = UnityEngine.Random.Range(0, invRival.hand.Count); }
                            while (randVal2 == randVal1);
                    }
                }
                else if (type == CardType.Intercambio)
                {
                    randVal1 = idxRival;
                }

                GameSync.Instance.SincronizarUsarCartaDeReserva(idxUsuario, (int)type, idxRival, false, randVal1, randVal2);
            }
            else
            {
                // Sin red: ejecutar localmente
                inv.RemoveFromReserve(cardToTrigger);
                CardManager.Instance.EjecutarEfectoReserva(cardToTrigger, usuario, rival);
            }
        }
    }
}
