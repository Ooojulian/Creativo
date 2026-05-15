using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;

    public GameManager gameManager;

    [Header("Pools de Cartas Globales")]
    public List<CardSO> poolCartasVentaja;
    public List<CardSO> poolCartasDesventaja;

    void Awake()
    {
        Instance = this;
    }

    public void EjecutarEfectoInmediato(CardSO card, MovimientoFicha usuario)
    {
        Debug.Log($"Ejecutando efecto inmediato de {card.cardName}");
        MovimientoFicha rival = EncontrarRival(usuario);

        switch (card.type)
        {
            case CardType.AvanceRapido:
                usuario.Avanzar(card.valor1 > 0 ? card.valor1 : 2);
                break;
            case CardType.Retroceso:
                if (rival != null) RetrocederEspecifico(rival, card.valor1 != 0 ? Mathf.Abs(card.valor1) : 3);
                break;
            case CardType.DobleTiro:
                usuario.dobleTiroPendiente = true;
                break;
            case CardType.PierdeTurno:
                if (rival != null) rival.pierdeSiguienteTurno = true;
                break;
            case CardType.Escudo:
            case CardType.AuraMagica:
                usuario.escudoActivo = true;
                break;
            case CardType.Recuperacion:
                var energia = usuario.GetComponent<EnergiaController>();
                if (energia != null) energia.GanarEnergia(card.valor1 > 0 ? card.valor1 : 2);
                break;
            case CardType.Fatiga:
                if (rival != null)
                {
                    var energiaRival = rival.GetComponent<EnergiaController>();
                    if (energiaRival != null) energiaRival.GastarEnergia(card.valor1 != 0 ? Mathf.Abs(card.valor1) : 1);
                }
                break;
            case CardType.RoboArcano:
                usuario.GetComponent<PlayerInventory>().AddToHand(ObtenerCartaAleatoria());
                break;
            case CardType.Ruptura:
                if (rival != null) RivalDescartaEspecifico(rival, card.valor1 > 0 ? card.valor1 : 2);
                break;
            case CardType.Intercambio:
                IntercambiarConCualquiera(usuario);
                break;
        }
    }

    public void EjecutarEfectoReserva(CardSO card, MovimientoFicha usuario, MovimientoFicha rivalInvolucrado = null)
    {
        Debug.Log($"Ejecutando efecto de reserva de {card.cardName}");
        switch (card.type)
        {
            case CardType.AvanceRapido:
                usuario.Avanzar(card.valor1 > 0 ? card.valor1 : 3);
                break;
            case CardType.Retroceso:
                if (rivalInvolucrado != null) RetrocederEspecifico(rivalInvolucrado, 3);
                break;
            case CardType.DobleTiro:
                usuario.dobleTiroPendiente = true;
                break;
            case CardType.PierdeTurno:
                if (rivalInvolucrado != null) rivalInvolucrado.pierdeSiguienteTurno = true;
                break;
            case CardType.Escudo:
            case CardType.AuraMagica:
                usuario.escudoActivo = true;
                break;
            case CardType.Recuperacion:
                var energia = usuario.GetComponent<EnergiaController>();
                if (energia != null) energia.GanarEnergia(1);
                break;
            case CardType.Fatiga:
                if (rivalInvolucrado != null)
                {
                    var eR = rivalInvolucrado.GetComponent<EnergiaController>();
                    if (eR != null) eR.GastarEnergia(1);
                }
                break;
            case CardType.RoboArcano:
                var inv = usuario.GetComponent<PlayerInventory>();
                inv.AddToHand(ObtenerCartaAleatoria());
                break;
            case CardType.Ruptura:
                if (rivalInvolucrado != null) RivalDescartaEspecifico(rivalInvolucrado, 1);
                break;
            case CardType.Intercambio:
                IntercambiarConCualquiera(usuario);
                break;
        }
    }

    // Métodos auxiliares (implementación básica por ahora)
    
    private void RetrocederRival(MovimientoFicha usuario, int pasos)
    {
        // Elige un rival al azar o el más cercano adelante
        MovimientoFicha rival = EncontrarRival(usuario);
        if (rival != null) RetrocederEspecifico(rival, pasos);
    }

    private void RetrocederEspecifico(MovimientoFicha rival, int pasos)
    {
        if (rival.escudoActivo)
        {
            Debug.Log($"[Escudo] {rival.name} bloqueó el retroceso.");
            rival.escudoActivo = false;
            return;
        }
        rival.indiceActual = Mathf.Max(0, rival.indiceActual - pasos);
        rival.transform.position = rival.ruta.casillas[rival.indiceActual].position + Vector3.up * 0.5f;
    }

    private void IntercambiarConCualquiera(MovimientoFicha usuario)
    {
        // En intercambio, ¿el escudo protege? Generalmente sí en estos juegos.
        // Pero para no complicar, lo dejamos así o buscamos un rival sin escudo.
        gameManager.IntercambiarConOtroJugador(usuario);
    }

    private void RivalPierdeTurno(MovimientoFicha usuario)
    {
        MovimientoFicha rival = EncontrarRival(usuario);
        if (rival != null)
        {
            if (rival.escudoActivo)
            {
                Debug.Log($"[Escudo] {rival.name} bloqueó perder turno.");
                rival.escudoActivo = false;
                return;
            }
            rival.pierdeSiguienteTurno = true;
        }
    }

    private void RivalDescarta(MovimientoFicha usuario, int cantidad)
    {
        MovimientoFicha rival = EncontrarRival(usuario);
        if (rival != null) RivalDescartaEspecifico(rival, cantidad);
    }

    private void RivalDescartaEspecifico(MovimientoFicha rival, int cantidad)
    {
        if (rival.escudoActivo)
        {
            Debug.Log($"[Escudo] {rival.name} bloqueó el descarte.");
            rival.escudoActivo = false;
            return;
        }
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
        // Lógica simple: el primer jugador en la lista que no sea el usuario
        foreach (var j in gameManager.todosLosJugadores)
        {
            if (j != usuario && j.gameObject.activeSelf) return j;
        }
        return null;
    }

    public CardSO ObtenerCartaAleatoria()
    {
        bool sacarVentaja = Random.value > 0.5f;
        if (sacarVentaja && poolCartasVentaja != null && poolCartasVentaja.Count > 0)
        {
            return ObtenerCartaVentaja();
        }
        else if (poolCartasDesventaja != null && poolCartasDesventaja.Count > 0)
        {
            return ObtenerCartaDesventaja();
        }

        return null;
    }

    public CardSO ObtenerCartaVentaja()
    {
        if (poolCartasVentaja == null || poolCartasVentaja.Count == 0) return null;
        return poolCartasVentaja[Random.Range(0, poolCartasVentaja.Count)];
    }

    public CardSO ObtenerCartaDesventaja()
    {
        if (poolCartasDesventaja == null || poolCartasDesventaja.Count == 0) return null;
        return poolCartasDesventaja[Random.Range(0, poolCartasDesventaja.Count)];
    }
}
