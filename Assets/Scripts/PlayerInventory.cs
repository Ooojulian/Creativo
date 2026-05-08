using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public List<CardSO> hand = new List<CardSO>();
    public List<CardSO> reserve = new List<CardSO>();
    
    [Header("Configuración")]
    public int maxReserveSize = 5;
    public int maxHandSize = 10; // Opcional

    public bool AddToHand(CardSO card)
    {
        if (hand.Count < maxHandSize)
        {
            hand.Add(card);
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
        hand.Remove(card);
    }

    public void RemoveFromReserve(CardSO card)
    {
        reserve.Remove(card);
    }
}
