using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Sincroniza turnos y movimiento de fichas entre clientes.
// El Host es la fuente de verdad: decide el turno y envia RPCs a todos.
public class GameSync : MonoBehaviourPunCallbacks
{
    public static GameSync Instance;

    [Header("Referencias")]
    public GameManager gameManager;

    // Actor number del jugador cuyo turno es ahora (asignado por el host)
    private int actorNumeroTurnoActual = -1;

    void Awake()
    {
        Instance = this;
    }

    // ─── TURNO ───────────────────────────────────────────────────────────────

    // El host llama esto para avisar a todos quién juega y mostrar el panel correcto
    public void AnunciarTurnoConPanel(int indiceTurno)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // Mapear índice de turno a actor number: el jugador en posición indiceTurno
        var jugadores = PhotonNetwork.PlayerList;
        int actorNumber = indiceTurno < jugadores.Length ? jugadores[indiceTurno].ActorNumber : 1;
        photonView.RPC(nameof(RPC_RecibirTurnoConPanel), RpcTarget.All, actorNumber, indiceTurno);
    }

    [PunRPC]
    private void RPC_RecibirTurnoConPanel(int actorNumber, int indiceTurno)
    {
        actorNumeroTurnoActual = actorNumber;
        bool esMiTurno = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[Red] Turno actor {actorNumber}. ¿Es mi turno? {esMiTurno}");

        if (gameManager == null) return;

        var jugador = gameManager.todosLosJugadores.Count > indiceTurno
            ? gameManager.todosLosJugadores[indiceTurno] : null;
        if (jugador == null) return;

        var ui = FindAnyObjectByType<SeleccionFichaUI>();
        if (esMiTurno)
        {
            if (ui != null) ui.MostrarSeleccion(jugador);
            else if (gameManager.dado != null) gameManager.dado.gameObject.SetActive(true);
        }
        else
        {
            if (ui != null) ui.OcultarPanel();
        }

        // Lanzar el evento de inicio de turno en todos los clientes
        gameManager.DispararOnTurnStarted(jugador);
    }

    public void AnunciarTurno(int indiceTurno)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var jugadores = PhotonNetwork.PlayerList;
        int actorNumber = indiceTurno < jugadores.Length ? jugadores[indiceTurno].ActorNumber : 1;
        photonView.RPC(nameof(RPC_RecibirTurno), RpcTarget.All, actorNumber, indiceTurno);
    }

    [PunRPC]
    private void RPC_RecibirTurno(int actorNumber, int indiceTurno)
    {
        actorNumeroTurnoActual = actorNumber;
        bool esMiTurno = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[Red] RPC_RecibirTurno actor={actorNumber} idx={indiceTurno} miActor={PhotonNetwork.LocalPlayer.ActorNumber} miTurno={esMiTurno}");

        if (gameManager == null) { Debug.LogError("[Red] gameManager null"); return; }
        if (gameManager.dado == null) { Debug.LogError("[Red] dado null"); return; }

        // Asignar jugador al dado en todos los clientes
        if (indiceTurno < gameManager.todosLosJugadores.Count)
        {
            gameManager.dado.jugador = gameManager.todosLosJugadores[indiceTurno];
            Debug.Log($"[Red] dado.jugador = {gameManager.dado.jugador.name}");
        }

        // Resetear estado de eleccion ficha al inicio del turno
        gameManager.dado.jugador.moverFichaB = false;

        // Solo cliente con turno ve el dado
        gameManager.dado.gameObject.SetActive(esMiTurno);
        Debug.Log($"[Red] dado.activeSelf despues SetActive: {gameManager.dado.gameObject.activeSelf}");

        // Lanzar el evento de inicio de turno en todos los clientes
        if (indiceTurno < gameManager.todosLosJugadores.Count)
        {
            gameManager.DispararOnTurnStarted(gameManager.todosLosJugadores[indiceTurno]);
        }
    }

    public bool EsMiTurno => actorNumeroTurnoActual == PhotonNetwork.LocalPlayer.ActorNumber;

    // ─── DADO ─────────────────────────────────────────────────────────────────

    // Mostrar resultado dado en otros clientes (sin mover todavía)
    public void SincronizarResultadoDado(int resultado)
    {
        photonView.RPC(nameof(RPC_MostrarResultado), RpcTarget.Others, resultado);
    }

    [PunRPC]
    private void RPC_MostrarResultado(int resultado)
    {
        if (gameManager != null && gameManager.textoResultadoDadoHUD != null)
        {
            gameManager.textoResultadoDadoHUD.text = $"Dado: {resultado}";
            gameManager.textoResultadoDadoHUD.gameObject.SetActive(true);
        }
    }

    // Llamado por el jugador local cuando tira el dado
    public void EnviarResultadoDado(int resultado, int indiceFicha, bool esFichaB)
    {
        photonView.RPC(nameof(RPC_RecibirMovimiento), RpcTarget.All, resultado, indiceFicha, esFichaB);
    }

    [PunRPC]
    private void RPC_RecibirMovimiento(int pasos, int indiceFicha, bool esFichaB)
    {
        Debug.Log($"[Red] RPC_RecibirMovimiento ficha={indiceFicha} pasos={pasos} fichaB={esFichaB} master={PhotonNetwork.IsMasterClient}");
        if (gameManager == null) { Debug.LogError("[Red] gameManager null en GameSync"); return; }
        if (gameManager.todosLosJugadores == null) { Debug.LogError("[Red] todosLosJugadores null"); return; }
        if (indiceFicha < 0 || indiceFicha >= gameManager.todosLosJugadores.Count)
        {
            Debug.LogError($"[Red] indiceFicha {indiceFicha} fuera de rango ({gameManager.todosLosJugadores.Count})");
            return;
        }

        MovimientoFicha ficha = gameManager.todosLosJugadores[indiceFicha];
        if (ficha == null) { Debug.LogError("[Red] ficha null en lista"); return; }
        if (!ficha.gameObject.activeSelf)
        {
            Debug.LogWarning($"[Red] ficha {indiceFicha} inactiva al recibir movimiento - activando");
            ficha.gameObject.SetActive(true);
        }
        ficha.ElegirFicha(esFichaB);
        ficha.Avanzar(pasos);
    }

    // ─── FIN DE TURNO ────────────────────────────────────────────────────────

    // Llamado por MovimientoFicha al terminar de moverse (solo el host decide siguiente turno)
    public void NotificarFinDeMovimiento()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        gameManager.SiguienteTurno();
    }

    // ─── INICIAR PARTIDA ─────────────────────────────────────────────────────

    public void IniciarPartida(int cantidad)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_IniciarPartida), RpcTarget.All, cantidad);
    }

    [PunRPC]
    private void RPC_IniciarPartida(int cantidad)
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        var menu = MenuController.Instance;
        if (menu != null)
        {
            if (menu.panelEspera != null)        menu.panelEspera.SetActive(false);
            if (menu.panelMenu != null)          menu.panelMenu.SetActive(false);
            if (menu.fondoMenu != null)          menu.fondoMenu.SetActive(false);
            if (menu.panelUnirse != null)        menu.panelUnirse.SetActive(false);
            if (menu.botonIniciarPartida != null) menu.botonIniciarPartida.gameObject.SetActive(false);
        }
        gameManager.IniciarPartida(cantidad);
    }

    // ─── SALA ────────────────────────────────────────────────────────────────

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Red] {newPlayer.NickName} entró. Jugadores: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Red] {otherPlayer.NickName} salió.");
        // Si era el host, Photon transfiere el MasterClient automáticamente
    }

    // ─── CARTAS ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sincroniza el robo de una carta: todos los clientes agregan la misma
    /// carta al inventario del jugador indicado.
    /// </summary>
    public void SincronizarCartaRobada(int indexJugador, int cardTypeInt)
    {
        photonView.RPC(nameof(RPC_CartaRobada), RpcTarget.All, indexJugador, cardTypeInt);
    }

    [PunRPC]
    private void RPC_CartaRobada(int indexJugador, int cardTypeInt)
    {
        if (gameManager == null) return;
        if (indexJugador < 0 || indexJugador >= gameManager.todosLosJugadores.Count) return;

        CardType tipo = (CardType)cardTypeInt;
        CardSO carta = CardManager.Instance?.ObtenerCartaPorTipo(tipo);
        if (carta == null)
        {
            Debug.LogWarning($"[Red] RPC_CartaRobada: no se encontró carta de tipo {tipo}");
            return;
        }

        var jugador = gameManager.todosLosJugadores[indexJugador];
        jugador.inventario?.AddToHand(carta);
        Debug.Log($"[Red] RPC_CartaRobada: J{indexJugador + 1} recibió {carta.cardName}");
    }

    /// <summary>
    /// Sincroniza el uso inmediato de una carta: en todos los clientes se
    /// descuenta la energía, se elimina de la mano y se ejecuta su efecto.
    /// </summary>
    /// <param name="indexRival">Índice del rival objetivo en todosLosJugadores. -1 = sin objetivo específico.</param>
    /// <param name="esFichaB">true si el efecto debe aplicarse sobre la Ficha B (inversa) del rival.</param>
    public void SincronizarUsarCarta(int indexJugador, int cardTypeInt, int indexRival = -1, bool esFichaB = false, int randVal1 = -1, int randVal2 = -1)
    {
        photonView.RPC(nameof(RPC_UsarCarta), RpcTarget.All, indexJugador, cardTypeInt, indexRival, esFichaB, randVal1, randVal2);
    }

    [PunRPC]
    private void RPC_UsarCarta(int indexJugador, int cardTypeInt, int indexRival, bool esFichaB, int randVal1, int randVal2)
    {
        if (gameManager == null) return;
        if (indexJugador < 0 || indexJugador >= gameManager.todosLosJugadores.Count) return;

        CardType tipo = (CardType)cardTypeInt;
        var jugador = gameManager.todosLosJugadores[indexJugador];

        // Buscar por tipo dentro de la mano real del jugador
        CardSO carta = jugador.inventario?.hand.Find(c => c != null && c.type == tipo);
        if (carta == null)
        {
            Debug.LogWarning($"[Red] RPC_UsarCarta: no hay carta de tipo {tipo} en la mano de J{indexJugador + 1}");
            return;
        }

        // Gastar energía
        var energia = jugador.GetComponent<EnergiaController>();
        if (energia != null) energia.GastarEnergia(carta.costoEnergia);

        // Eliminar carta de la mano
        jugador.inventario.RemoveFromHand(carta);

        // Resolver rival objetivo
        MovimientoFicha rivalObjetivo = null;
        if (indexRival >= 0 && indexRival < gameManager.todosLosJugadores.Count)
            rivalObjetivo = gameManager.todosLosJugadores[indexRival];

        // Ejecutar efecto pasando qué token afectar y valores deterministas
        CardManager.Instance?.EjecutarEfectoInmediato(carta, jugador, rivalObjetivo, esFichaB, randVal1, randVal2);
        Debug.Log($"[Red] RPC_UsarCarta: J{indexJugador + 1} usó {carta.cardName} contra J{indexRival + 1} ficha{(esFichaB ? 'B' : 'A')}");
    }

    /// <summary>
    /// Sincroniza guardar una carta en la reserva: en todos los clientes se
    /// mueve la carta de la mano a la reserva del jugador.
    /// </summary>
    public void SincronizarGuardarCarta(int indexJugador, int cardTypeInt)
    {
        photonView.RPC(nameof(RPC_GuardarCarta), RpcTarget.All, indexJugador, cardTypeInt);
    }

    [PunRPC]
    private void RPC_GuardarCarta(int indexJugador, int cardTypeInt)
    {
        if (gameManager == null) return;
        if (indexJugador < 0 || indexJugador >= gameManager.todosLosJugadores.Count) return;

        CardType tipo = (CardType)cardTypeInt;
        var jugador = gameManager.todosLosJugadores[indexJugador];

        // ¡IMPORTANTE! Buscar por tipo dentro de la mano real del jugador,
        // no en el pool, para que Remove() encuentre la referencia exacta.
        CardSO carta = jugador.inventario?.hand.Find(c => c != null && c.type == tipo);
        if (carta == null)
        {
            Debug.LogWarning($"[Red] RPC_GuardarCarta: no hay carta de tipo {tipo} en la mano de J{indexJugador + 1}");
            return;
        }

        jugador.inventario.SaveToReserve(carta);
        Debug.Log($"[Red] RPC_GuardarCarta: J{indexJugador + 1} guardó {carta.cardName} en reserva");
    }

    /// <summary>
    /// Sincroniza el uso de una carta que estaba guardada en la RESERVA.
    /// Busca en la reserva (no en la mano) y ejecuta el efecto en todos los clientes.
    /// </summary>
    /// <param name="indexRival">Índice del rival objetivo en todosLosJugadores. -1 = sin objetivo específico.</param>
    /// <param name="esFichaB">true si el efecto debe aplicarse sobre la Ficha B (inversa) del rival.</param>
    public void SincronizarUsarCartaDeReserva(int indexJugador, int cardTypeInt, int indexRival = -1, bool esFichaB = false, int randVal1 = -1, int randVal2 = -1)
    {
        photonView.RPC(nameof(RPC_UsarCartaDeReserva), RpcTarget.All, indexJugador, cardTypeInt, indexRival, esFichaB, randVal1, randVal2);
    }

    [PunRPC]
    private void RPC_UsarCartaDeReserva(int indexJugador, int cardTypeInt, int indexRival, bool esFichaB, int randVal1, int randVal2)
    {
        if (gameManager == null) return;
        if (indexJugador < 0 || indexJugador >= gameManager.todosLosJugadores.Count) return;

        CardType tipo = (CardType)cardTypeInt;
        var jugador = gameManager.todosLosJugadores[indexJugador];

        CardSO carta = jugador.inventario?.reserve.Find(c => c != null && c.type == tipo);
        if (carta == null)
        {
            Debug.LogWarning($"[Red] RPC_UsarCartaDeReserva: no hay carta de tipo {tipo} en la reserva de J{indexJugador + 1}");
            return;
        }

        MovimientoFicha rivalObjetivo = null;
        if (indexRival >= 0 && indexRival < gameManager.todosLosJugadores.Count)
            rivalObjetivo = gameManager.todosLosJugadores[indexRival];

        jugador.inventario.RemoveFromReserve(carta);
        CardManager.Instance?.EjecutarEfectoInmediato(carta, jugador, rivalObjetivo, esFichaB, randVal1, randVal2);
        Debug.Log($"[Red] RPC_UsarCartaDeReserva: J{indexJugador + 1} jugó {carta.cardName} desde reserva contra J{indexRival + 1} ficha{(esFichaB ? 'B' : 'A')}");
    }
}

