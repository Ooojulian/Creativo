using UnityEngine;
using System.Collections;

public class MovimientoFicha : MonoBehaviour
{
    public GestorDeRuta ruta;
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

        int casillasRestantes = ruta.casillas.Count - 1 - indiceActual;

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
        if (ruta == null || ruta.casillas == null || ruta.casillas.Count == 0)
        {
            Debug.LogError($"[MovimientoFicha] {name}: ruta no asignada o sin casillas.");
            enMovimiento = false;
            if (gm != null) gm.SiguienteTurno();
            yield break;
        }

        int metaFinal = indiceActual + pasos;
        if (metaFinal >= ruta.casillas.Count)
            metaFinal = ruta.casillas.Count - 1;

        while (indiceActual < metaFinal)
        {
            int indiceAnterior = indiceActual;
            indiceActual++;

            if (ruta.casillas[indiceActual] == null)
            {
                Debug.LogWarning($"[MovimientoFicha] casilla[{indiceActual}] es null, saltando.");
                continue;
            }

            // CHECK: Overtake (Pisotón)
            if (CardTriggerSystem.Instance != null)
            {
                foreach (var otro in gm.todosLosJugadores)
                {
                    if (otro != this && otro.indiceActual == indiceActual)
                    {
                        // Si yo paso a alguien que estaba en esta casilla
                        CardTriggerSystem.Instance.CheckOvertake(otro, this);
                    }
                }
            }

            Vector3 destino = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;

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

        bool llegóAMeta = indiceActual >= ruta.casillas.Count - 1;

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

        // En red: solo host decide avance de turno y meta. Otros clientes solo animaron.
        bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
        if (!soyAutoridad)
        {
            Debug.Log($"[MovimientoFicha] {name} cliente termino animacion. Espera turno de host.");
            yield break;
        }

        // Si hay una carta, el turno avanza DESPUÉS de cerrarla.
        // Si no se reveló carta (ej: no hay CartaEnCasilla), avanzamos aquí.
        if (!reveloCarta)
        {
            ContinuarTurno(llegóAMeta);
        }
    }

    private bool reveloCarta = false;

    IEnumerator RevelarYAñadirCarta()
    {
        reveloCarta = false;
        if (ruta == null || ruta.casillas == null || ruta.casillas.Count == 0) yield break;
        if (indiceActual <= 0 || indiceActual >= ruta.casillas.Count) yield break;

        Transform casilla = ruta.casillas[indiceActual];
        if (casilla == null) yield break;

        CartaEnCasilla comp = casilla.GetComponent<CartaEnCasilla>();
        if (comp == null)
        {
            Debug.LogError($"[Atención] La casilla '{casilla.name}' NO TIENE el script 'CartaEnCasilla'. Por eso no te da ninguna carta. Revisa la advertencia de 'Missing Script' en el inspector de esta casilla.");
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        CardSO card = comp.ObtenerCarta();

        if (card == null)
        {
            Debug.LogWarning($"[Cartas] No se obtuvo ninguna carta en la casilla {indiceActual}. El pool puede estar vacío.");
            if (gm != null && gm.uiCartas != null) gm.uiCartas.Limpiar();
            yield break;
        }

        Debug.Log($"[Cartas] Se reveló la carta {card.cardName}. Evaluando esMia...");

        // 1) Mostrar revelación SOLO SI ES MI FICHA (privado para el jugador local)
        bool esMia = true;
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            int myActor = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
            int myIndex = -1;
            for (int i = 0; i < Photon.Pun.PhotonNetwork.PlayerList.Length; i++)
            {
                if (Photon.Pun.PhotonNetwork.PlayerList[i].ActorNumber == myActor)
                {
                    myIndex = i;
                    break;
                }
            }
            int fichaIndex = gm.todosLosJugadores.IndexOf(this);
            esMia = (myIndex == fichaIndex);
        }

        reveloCarta = true;

        if (esMia && gm != null && gm.uiCartas != null)
            gm.uiCartas.MostrarRevelacion(card);

        // Esperar solo en el cliente que posee esta ficha
        if (esMia)
            yield return new WaitForSeconds(tiempoRevelacion);

        // 2) Añadir a la mano (si deciden usarla o guardarla, se removerá)
        if (inventario != null)
        {
            bool added = inventario.AddToHand(card);
            Debug.Log($"[Cartas] Añadido a la mano: {added}. Total en mano: {inventario.hand.Count}");
            if (CardTriggerSystem.Instance != null)
                CardTriggerSystem.Instance.CheckCardDrawn(this, card);
        }

        // 3) Abrir panel Usar/Guardar SOLO para el jugador local
        Debug.Log($"[Cartas] esMia = {esMia}, CardPlayUI.Instance = {(CardPlayUI.Instance != null)}");
        if (esMia && CardPlayUI.Instance != null)
        {
            Debug.Log($"[Cartas] Mostrando UI de carta...");
            CardPlayUI.Instance.Mostrar(card, this);
            
            // Esperar a que el jugador cierre el panel o use la carta
            while (CardPlayUI.Instance.panel.activeSelf)
            {
                yield return null;
            }
        }

        // 4) Fade out de la revelación DESPUÉS de haber tomado la decisión
        if (esMia && gm != null && gm.uiCartas != null)
            yield return StartCoroutine(gm.uiCartas.FadeOutYLimpiar(tiempoResultado));

        bool llegóAMeta = indiceActual >= ruta.casillas.Count - 1;
        bool soyAutoridad = GameSync.Instance == null || Photon.Pun.PhotonNetwork.IsMasterClient;
        if (soyAutoridad)
        {
            ContinuarTurno(llegóAMeta);
        }
    }

    public void ContinuarTurno(bool llegóAMeta)
    {
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
                    if (gm.dado != null) gm.dado.gameObject.SetActive(true);
                    Debug.Log($"[Cartas] {name} repite turno por DobleTiro.");
                }
                else
                {
                    gm.SiguienteTurno();
                }
            }
        }
    }
}