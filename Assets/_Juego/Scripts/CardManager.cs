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

    public void EjecutarEfectoInmediato(CardSO card, MovimientoFicha usuario, MovimientoFicha rivalEspecifico = null, bool esFichaB = false, int randVal1 = -1, int randVal2 = -1)
    {
        Debug.Log($"Ejecutando efecto inmediato de {card.cardName} | fichaB={esFichaB} | randVal1={randVal1} | randVal2={randVal2}");
        // Si se pasó un rival específico (desde el selector de objetivos), usarlo.
        // Si no, caer al comportamiento por defecto (primer rival activo).
        MovimientoFicha rival = rivalEspecifico ?? EncontrarRival(usuario);

        switch (card.type)
        {
            case CardType.AvanceRapido:
                usuario.Avanzar(card.valor1 > 0 ? card.valor1 : 2);
                break;
            case CardType.Retroceso:
                if (rival != null) RetrocederEspecifico(rival, card.valor1 != 0 ? Mathf.Abs(card.valor1) : 3, esFichaB);
                break;
            case CardType.DobleTiro:
                usuario.dobleTiroPendiente = true;
                break;
            case CardType.PierdeTurno:
                if (rival != null) rival.pierdeSiguienteTurno = true;
                break;
            case CardType.Escudo:
                usuario.escudoActivo = true;
                break;
            case CardType.AuraMagica:
                usuario.escudoActivo = true;
                var energiaAura = usuario.GetComponent<EnergiaController>();
                if (energiaAura != null) energiaAura.GanarEnergia(card.valor1 > 0 ? card.valor1 : 1);
                break;
            case CardType.Recuperacion:
                var energia = usuario.GetComponent<EnergiaController>();
                if (energia != null) energia.GanarEnergia(card.valor1 > 0 ? card.valor1 : 2);
                break;
            case CardType.RoboArcano:
                CardSO cartaRobada = (randVal1 >= 0) ? ObtenerCartaPorTipo((CardType)randVal1) : ObtenerCartaAleatoria();
                if (cartaRobada != null)
                    usuario.GetComponent<PlayerInventory>().AddToHand(cartaRobada);
                break;
            case CardType.VisionDelSabio:
                var invVision = usuario.GetComponent<PlayerInventory>();
                if (randVal1 >= 0)
                {
                    CardSO c1 = ObtenerCartaPorTipo((CardType)randVal1);
                    if (c1 != null) invVision.AddToHand(c1);
                }
                else
                {
                    invVision.AddToHand(ObtenerCartaVentaja());
                }
                if (randVal2 >= 0)
                {
                    CardSO c2 = ObtenerCartaPorTipo((CardType)randVal2);
                    if (c2 != null) invVision.AddToHand(c2);
                }
                else
                {
                    invVision.AddToHand(ObtenerCartaVentaja());
                }
                break;
            case CardType.Maldicion:
                if (rival != null)
                {
                    var energiaRival = rival.GetComponent<EnergiaController>();
                    if (energiaRival != null) energiaRival.GastarEnergia(energiaRival.EnergiaActual);
                }
                break;
            case CardType.Silencio:
                if (rival != null) rival.silencioActivo = true;
                break;
            case CardType.Ruptura:
                if (rival != null) RivalDescartaEspecificoDeterministic(rival, randVal1, randVal2);
                break;
            case CardType.Intercambio:
                IntercambiarConCualquieraDeterministic(usuario, randVal1);
                break;
        }
    }

    public void EjecutarEfectoReserva(CardSO card, MovimientoFicha usuario, MovimientoFicha rivalInvolucrado = null, int randVal1 = -1, int randVal2 = -1)
    {
        Debug.Log($"Ejecutando efecto de reserva de {card.cardName} | randVal1={randVal1} | randVal2={randVal2}");
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
            case CardType.RoboArcano:
            case CardType.VisionDelSabio:
                var inv = usuario.GetComponent<PlayerInventory>();
                if (randVal1 >= 0)
                {
                    CardSO c = ObtenerCartaPorTipo((CardType)randVal1);
                    if (c != null) inv.AddToHand(c);
                }
                else
                {
                    inv.AddToHand(ObtenerCartaVentaja());
                }
                break;
            case CardType.Maldicion:
                if (rivalInvolucrado != null)
                {
                    var eR = rivalInvolucrado.GetComponent<EnergiaController>();
                    if (eR != null) eR.GastarEnergia(eR.EnergiaActual);
                }
                break;
            case CardType.Silencio:
                if (rivalInvolucrado != null) rivalInvolucrado.silencioActivo = true;
                break;
            case CardType.Ruptura:
                if (rivalInvolucrado != null) RivalDescartaEspecificoDeterministic(rivalInvolucrado, randVal1, randVal2);
                break;
            case CardType.Intercambio:
                IntercambiarConCualquieraDeterministic(usuario, randVal1);
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

    private void RetrocederEspecifico(MovimientoFicha rival, int pasos, bool esFichaB = false)
    {
        if (rival.escudoActivo)
        {
            Debug.Log($"[Escudo] {rival.name} bloquó el retroceso.");
            rival.escudoActivo = false;
            return;
        }

        if (esFichaB && rival.fichaB != null)
        {
            // Delegar en el método animado de FichaInversa
            rival.fichaB.Retroceder(pasos);
            Debug.Log($"[Retroceso] Llamando Retroceder({pasos}) en FichaB de {rival.name}");
        }
        else
        {
            // Ficha A (normal): retroceder = disminuir el índice
            rival.indiceActual = Mathf.Max(0, rival.indiceActual - pasos);
            var casillas = gameManager.casillas;
            if (casillas != null && rival.indiceActual < casillas.Count)
                rival.transform.position = casillas[rival.indiceActual].position + Vector3.up * 0.5f;
            Debug.Log($"[Retroceso] FichaA de {rival.name} retrocedió {pasos} casillas -> idx {rival.indiceActual}");
        }
    }

    private void IntercambiarConCualquieraDeterministic(MovimientoFicha usuario, int randRivalIdx)
    {
        gameManager.IntercambiarConOtroJugador(usuario, randRivalIdx);
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

    private void RivalDescartaEspecificoDeterministic(MovimientoFicha rival, int idx1, int idx2)
    {
        if (rival.escudoActivo)
        {
            Debug.Log($"[Escudo] {rival.name} bloqueó el descarte.");
            rival.escudoActivo = false;
            return;
        }
        var inv = rival.GetComponent<PlayerInventory>();
        if (inv == null || inv.hand.Count == 0) return;

        var indices = new List<int>();
        if (idx1 >= 0 && idx1 < inv.hand.Count) indices.Add(idx1);
        if (idx2 >= 0 && idx2 < inv.hand.Count && idx2 != idx1) indices.Add(idx2);

        indices.Sort((a, b) => b.CompareTo(a)); // ordenar de mayor a menor
        foreach (int index in indices)
        {
            if (index >= 0 && index < inv.hand.Count)
            {
                Debug.Log($"[Cartas] Ruptura descarta índice {index} ({inv.hand[index].cardName}) de J{gameManager.todosLosJugadores.IndexOf(rival) + 1}");
                inv.hand.RemoveAt(index);
            }
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

    /// <summary>
    /// Busca un CardSO por su tipo en los pools del juego.
    /// Se usa en los RPCs de red para reconstruir la carta exacta desde un entero.
    /// </summary>
    public CardSO ObtenerCartaPorTipo(CardType tipo)
    {
        if (poolCartasVentaja != null)
            foreach (var c in poolCartasVentaja)
                if (c != null && c.type == tipo) return c;

        if (poolCartasDesventaja != null)
            foreach (var c in poolCartasDesventaja)
                if (c != null && c.type == tipo) return c;

        Debug.LogWarning($"[CardManager] No se encontró carta de tipo {tipo} en ningún pool.");
        return null;
    }
}
