using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerInventory : MonoBehaviour
{
    public List<CardSO> hand = new List<CardSO>();
    public List<CardSO> reserve = new List<CardSO>();
    
    [Header("Configuración")]
    public int maxReserveSize = 5;
    public int maxHandSize = 10; // Opcional

    public event Action OnInventoryChanged;

    public bool AddToHand(CardSO card)
    {
        if (card == null) return false;
        
        if (hand.Count < maxHandSize)
        {
            hand.Add(card);
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool SaveToReserve(CardSO card)
    {
        if (reserve.Count < maxReserveSize)
        {
            hand.Remove(card);
            reserve.Add(card);
            Debug.Log($"Carta {card.cardName} guardada en reserva.");
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            Debug.Log("Reserva llena.");
            return false;
        }
    }

    public void RemoveFromHand(CardSO card)
    {
        if (hand.Remove(card))
        {
            OnInventoryChanged?.Invoke();
        }
    }

    public void RemoveFromReserve(CardSO card)
    {
        if (reserve.Remove(card))
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
