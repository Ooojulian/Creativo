using UnityEngine;
using System.Collections;

public class MovimientoFicha : MonoBehaviour
{
    public GestorDeRutas ruta;
    public int indiceActual = 0;
    public float velocidad = 150f;
    public GameManager gm;

    [Header("Estado de cartas")]
    public bool escudoActivo = false;
    public bool pierdeSiguienteTurno = false;
    public bool dobleTiroPendiente = false;
    public bool silencioActivo = false;
    public bool maldicionActiva = false;

    [Header("Cartas - UI timing")]
    public float tiempoRevelacion = 1f; // tiempo mostrando la carta antes de aplicar (1 seg)
    public float tiempoResultado = 0f;  // delay antes del fade out (0 = fade inmediato)

    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;
    private PersonajeAnimador _animador;

    [Header("Nuevo Sistema de Cartas")]
    public PlayerInventory inventario;
    public int cartasAlEmpezarTurno = 0;

    [Header("Segunda Ficha")]
    public FichaInversa fichaB;
    public bool moverFichaB = false; // true = mover FichaB, false = mover FichaA (esta)

    [Header("Red")]
    public int actorNumber = -1; // ActorNumber de Photon del jugador que controla esta ficha

    void Awake()
    {
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>();
        if (inventario == null) inventario = GetComponent<PlayerInventory>();
        _animador = GetComponentInChildren<PersonajeAnimador>();
    }

    public void Avanzar(int cantidadPasos)
    {
        if (enMovimiento) return;

        // Si eligió mover la ficha B, delegar
        if (moverFichaB && fichaB != null)
        {
            fichaB.Avanzar(cantidadPasos);
            return;
        }

        var casillas = gm?.casillas;
        if (casillas == null || casillas.Count == 0) { if (gm != null) gm.SiguienteTurno(); return; }
        int casillasRestantes = casillas.Count - 1 - indiceActual;

        if (CardTriggerSystem.Instance != null)
            CardTriggerSystem.Instance.CheckNearGoal(this);

        if (cantidadPasos > casillasRestantes)
        {
            Debug.Log($"[MovimientoFicha] {name}: necesita {casillasRestantes} o menos para avanzar, sacó {cantidadPasos}. Turno perdido.");
            if (Photon.Pun.PhotonNetwork.IsMasterClient && gm != null) gm.SiguienteTurno();
            else if (GameSync.Instance == null && gm != null) gm.SiguienteTurno();
            return;
        }

        StartCoroutine(MoverPorLasCasillas(cantidadPasos));
    }

    // Llamado por la UI antes de tirar el dado
    public void ElegirFicha(bool usarFichaB)
    {
        moverFichaB = usarFichaB;
        Debug.Log($"[{name}] Ficha elegida: {(usarFichaB ? "B (inversa)" : "A (normal)")}");
    }

    IEnumerator MoverPorLasCasillas(int pasos)
    {
        enMovimiento = true;
        _animador?.SetMoviendo(true);

        // Ocultar dado y enfocar al jugador
        if (gm != null && gm.dado != null)
            gm.dado.gameObject.SetActive(false);

        if (camaraDirectora != null) camaraDirectora.SeguirJugador(transform);

        // Validar referencias
        var casillas = gm?.casillas;
        if (casillas == null || casillas.Count == 0)
        {
            Debug.LogError($"[MovimientoFicha] {name}: casillas no disponibles.");
            enMovimiento = false;
            if (gm != null) gm.SiguienteTurno();
            yield break;
        }

        int metaFinal = Mathf.Min(indiceActual + pasos, casillas.Count - 1);

        while (indiceActual < metaFinal)
        {
            indiceActual++;

            if (casillas[indiceActual] == null) continue;

            if (CardTriggerSystem.Instance != null)
            {
                foreach (var otro in gm.todosLosJugadores)
                {
                    if (otro != this && otro.indiceActual == indiceActual)
                        CardTriggerSystem.Instance.CheckOvertake(otro, this);
                }
            }

            Vector3 destino = casillas[indiceActual].position + Vector3.up * 0.5f;

            while (Vector3.Distance(transform.position, destino) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, destino, velocidad * Time.deltaTime);
                yield return null;
            }

            transform.position = destino;
            yield return new WaitForSeconds(0.08f);
        }

        enMovimiento = false;
        _animador?.SetMoviendo(false);
        Debug.Log($"[MovimientoFicha] {name} llegó a casilla {indiceActual}");

        // REVELAR -> AÑADIR A MANO
        yield return StartCoroutine(RevelarYAñadirCarta());

        if (camaraDirectora != null) camaraDirectora.VolverAlTablero();

        // Detectar colisión con ficha enemiga → batalla PPS
        if (Photon.Pun.PhotonNetwork.IsMasterClient && BatallaPPS.Instance != null && gm != null)
        {
            if (gm.DetectarColision(this, null, out int idxDef, out bool esBDef, out int actorDef))
            {
                int idxAtk = gm.todosLosJugadores.IndexOf(this);
                int actorAtk = idxAtk < Photon.Pun.PhotonNetwork.PlayerList.Length
                    ? Photon.Pun.PhotonNetwork.PlayerList[idxAtk].ActorNumber : -1;
                BatallaPPS.Instance.IniciarBatalla(actorAtk, actorDef, idxAtk, false, idxDef, esBDef);
                yield break;
            }
        }

        bool llegóAMeta = casillas != null && indiceActual >= casillas.Count - 1;

        // En red: solo host decide avance de turno y meta. Otros clientes solo animaron.
        bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
        if (!soyAutoridad)
        {
            Debug.Log($"[MovimientoFicha] {name} cliente termino animacion. Espera turno de host.");
            yield break;
        }

        if (gm != null)
        {
            if (llegóAMeta)
            {
                _animador?.SetVictoria();
                gm.LlegarAMeta(this);
            }
            else
            {
                if (dobleTiroPendiente)
                {
                    dobleTiroPendiente = false;
                    Debug.Log($"[Cartas] {name} repite turno por DobleTiro.");
                    gm.PrepararTurno();
                }
                else
                {
                    gm.SiguienteTurno();
                }
            }
        }
    }

    IEnumerator RevelarYAñadirCarta()
    {
        var casillas = gm?.casillas;
        if (casillas == null || casillas.Count == 0) yield break;
        if (indiceActual <= 0 || indiceActual >= casillas.Count) yield break;

        Transform casilla = casillas[indiceActual];
        if (casilla == null) yield break;

        CartaEnCasilla comp = casilla.GetComponent<CartaEnCasilla>();
        if (comp == null)
        {
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        // Determinar si es el jugador local — GameSync.EsMiTurno es la fuente correcta
        // (las fichas son objetos de escena, photonView.IsMine siempre apunta al host)
        bool esMia = true;
        if (Photon.Pun.PhotonNetwork.InRoom)
            esMia = GameSync.Instance != null && GameSync.Instance.EsMiTurno;

        // ─── Sin red: comportamiento local original ───────────────────────────
        if (!Photon.Pun.PhotonNetwork.InRoom)
        {
            CardSO card = comp.ObtenerCarta();
            if (card == null)
            {
                if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
                yield break;
            }

            if (gm != null && gm.uiCartas != null)
                gm.uiCartas.MostrarRevelacion(card);

            yield return new WaitForSeconds(tiempoRevelacion);

            if (inventario != null)
            {
                inventario.AddToHand(card);
                if (CardTriggerSystem.Instance != null)
                    CardTriggerSystem.Instance.CheckCardDrawn(this, card);
            }

            if (CardPlayUI.Instance != null)
                CardPlayUI.Instance.Mostrar(card, this);

            if (gm != null && gm.uiCartas != null)
                yield return StartCoroutine(gm.uiCartas.FadeOutYLimpiar(tiempoResultado));

            yield break;
        }

        // ─── Con red: solo el dueño del turno decide y muestra la carta ──────
        // Los demás clientes no hacen nada — el RPC_CartaRobada se encarga del AddToHand.
        if (!esMia) yield break;

        CardSO cartaElegida = comp.ObtenerCarta();

        if (cartaElegida == null)
        {
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        // Mostrar revelación solo en mi pantalla
        if (gm != null && gm.uiCartas != null)
            gm.uiCartas.MostrarRevelacion(cartaElegida);

        yield return new WaitForSeconds(tiempoRevelacion);

        // Sincronizar con todos los clientes (el RPC hace el AddToHand en todos)
        int fichaIndex = gm.todosLosJugadores.IndexOf(this);
        if (GameSync.Instance != null && fichaIndex >= 0)
            GameSync.Instance.SincronizarCartaRobada(fichaIndex, (int)cartaElegida.type);

        // Trigger de CheckCardDrawn solo local
        if (CardTriggerSystem.Instance != null)
            CardTriggerSystem.Instance.CheckCardDrawn(this, cartaElegida);

        // Fade out solo en mi pantalla
        if (gm != null && gm.uiCartas != null)
            yield return StartCoroutine(gm.uiCartas.FadeOutYLimpiar(tiempoResultado));

        // Abrir panel Usar/Guardar solo en mi pantalla
        if (CardPlayUI.Instance != null)
            CardPlayUI.Instance.Mostrar(cartaElegida, this);
    }

    public System.Collections.Generic.List<Transform> ObtenerCasillas()
    {
        if (gm == null) gm = FindAnyObjectByType<GameManager>();
        return gm?.casillas;
    }

    public EstadosJugador ObtenerEstados()
    {
        var e = GetComponent<EstadosJugador>();
        if (e == null) e = gameObject.AddComponent<EstadosJugador>();
        return e;
    }
}