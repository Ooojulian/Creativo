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
        if (inv.hand.Count < cartasAlEmpezar)
        {
            TryTrigger(jugador, CardType.RoboArcano);
        }
    }

    // Llamado cuando alguien roba carta
    public void CheckCardDrawn(MovimientoFicha jugador, CardSO card)
    {
        foreach (var j in gameManager.todosLosJugadores)
        {
            if (j != jugador)
            {
                var inv = jugador.GetComponent<PlayerInventory>();
                if (inv.hand.Count >= 5) // O si la carta es especial
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
        CardSO cardToTrigger = inv.reserve.Find(c => c.type == type);
        
        if (cardToTrigger != null)
        {
            Debug.Log($"¡Trigger de reserva! {cardToTrigger.cardName} de {usuario.name}");
            inv.RemoveFromReserve(cardToTrigger);
            CardManager.Instance.EjecutarEfectoReserva(cardToTrigger, usuario, rival);
        }
    }
}
