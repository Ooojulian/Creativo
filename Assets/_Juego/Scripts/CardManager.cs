using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;

    public GameManager gameManager;

    void Awake()
    {
        Instance = this;
    }

    public void EjecutarEfectoInmediato(CardSO card, MovimientoFicha usuario)
    {
        Debug.Log($"Ejecutando efecto inmediato de {card.cardName}");
        switch (card.type)
        {
            case CardType.Pisoton:
                RetrocederRival(usuario, 3);
                break;
            case CardType.Desvio:
                IntercambiarConCualquiera(usuario);
                break;
            case CardType.LadronDeTurno:
                RivalPierdeTurno(usuario);
                break;
            case CardType.Olvido:
                RivalDescarta(usuario, 2);
                break;
            case CardType.Inspiracion:
                usuario.GetComponent<PlayerInventory>().AddToHand(ObtenerCartaAleatoria());
                break;
            case CardType.Espionaje:
                EspiarYRobar(usuario);
                break;
            case CardType.Sprint:
                usuario.Avanzar(2);
                break;
        }
    }

    public void EjecutarEfectoReserva(CardSO card, MovimientoFicha usuario, MovimientoFicha rivalInvolucrado = null)
    {
        Debug.Log($"Ejecutando efecto de reserva de {card.cardName}");
        switch (card.type)
        {
            case CardType.Pisoton:
                if (rivalInvolucrado != null) RetrocederEspecifico(rivalInvolucrado, 3);
                break;
            case CardType.Desvio:
                IntercambiarConCualquiera(usuario);
                break;
            case CardType.LadronDeTurno:
                if (rivalInvolucrado != null) 
                {
                    RivalPierdeTurno(rivalInvolucrado);
                }
                break;
            case CardType.Olvido:
                if (rivalInvolucrado != null)
                {
                    RivalDescartaEspecifico(rivalInvolucrado, 2);
                }
                break;
            case CardType.Inspiracion:
                var inv = usuario.GetComponent<PlayerInventory>();
                inv.AddToHand(ObtenerCartaAleatoria());
                inv.AddToHand(ObtenerCartaAleatoria());
                break;
            case CardType.Espionaje:
                if (rivalInvolucrado != null) TomarCartaDeRival(usuario, rivalInvolucrado);
                break;
            case CardType.Sprint:
                usuario.Avanzar(3);
                break;
        }
    }

    // Métodos auxiliares
    
    private void RetrocederRival(MovimientoFicha usuario, int pasos)
    {
        MovimientoFicha rival = EncontrarRival(usuario);
        if (rival != null) RetrocederEspecifico(rival, pasos);
    }

    private void RetrocederEspecifico(MovimientoFicha rival, int pasos)
    {
        rival.indiceActual = Mathf.Max(0, rival.indiceActual - pasos);
        rival.transform.position = gameManager.casillas[rival.indiceActual].position + Vector3.up * 0.5f;
    }

    private void IntercambiarConCualquiera(MovimientoFicha usuario)
    {
        MovimientoFicha rival = EncontrarRival(usuario);
        if (rival != null)
        {
            int tempIndice = usuario.indiceActual;
            usuario.indiceActual = rival.indiceActual;
            rival.indiceActual = tempIndice;
            
            usuario.transform.position = gameManager.casillas[usuario.indiceActual].position + Vector3.up * 0.5f;
            rival.transform.position = gameManager.casillas[rival.indiceActual].position + Vector3.up * 0.5f;
        }
    }

    private void RivalPierdeTurno(MovimientoFicha rival)
    {
        if (rival != null)
        {
            rival.ObtenerEstados().AplicarEstado(EstadosJugador.SILENCIO, 1);
        }
    }

    private void RivalDescarta(MovimientoFicha usuario, int cantidad)
    {
        MovimientoFicha rival = EncontrarRival(usuario);
        if (rival != null) RivalDescartaEspecifico(rival, cantidad);
    }

    private void RivalDescartaEspecifico(MovimientoFicha rival, int cantidad)
    {
        var inv = rival.GetComponent<PlayerInventory>();
        for (int i = 0; i < cantidad; i++)
        {
            if (inv.hand.Count > 0) inv.hand.RemoveAt(Random.Range(0, inv.hand.Count));
        }
    }

    private void EspiarYRobar(MovimientoFicha usuario)
    {
        MovimientoFicha rival = EncontrarRival(usuario);
        if (rival != null) TomarCartaDeRival(usuario, rival);
    }

    private void TomarCartaDeRival(MovimientoFicha usuario, MovimientoFicha rival)
    {
        var invRival = rival.GetComponent<PlayerInventory>();
        var invUsuario = usuario.GetComponent<PlayerInventory>();
        if (invRival.hand.Count > 0)
        {
            int idx = Random.Range(0, invRival.hand.Count);
            CardSO card = invRival.hand[idx];
            invRival.hand.RemoveAt(idx);
            invUsuario.AddToHand(card);
        }
    }

    private MovimientoFicha EncontrarRival(MovimientoFicha usuario)
    {
        foreach (var j in gameManager.todosLosJugadores)
        {
            if (j != usuario && j.gameObject.activeSelf) return j;
        }
        return null;
    }

    private CardSO ObtenerCartaAleatoria()
    {
        return null; 
    }
}
