using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;

public enum EleccionPPS { Ninguna = 0, Piedra = 1, Papel = 2, Tijera = 3 }

// Sistema de batalla Piedra-Papel-Tijera entre dos jugadores
public class BatallaPPS : MonoBehaviourPun
{
    public static BatallaPPS Instance;

    [Header("UI")]
    public GameObject panelBatalla;
    public Button botonPiedra;
    public Button botonPapel;
    public Button botonTijera;
    public TextMeshProUGUI textoEstado;
    public TextMeshProUGUI textoResultado;

    [Header("Referencias")]
    public GameManager gameManager;

    // Estado batalla
    private int actorAtacante = -1;
    private int actorDefensor = -1;
    private int indiceFichaAtacante = -1;
    private bool fichaBAtacante = false;
    private int indiceFichaDefensor = -1;
    private bool fichaBDefensor = false;

    private EleccionPPS eleccionAtacante = EleccionPPS.Ninguna;
    private EleccionPPS eleccionDefensor = EleccionPPS.Ninguna;

    void Awake()
    {
        Instance = this;
        if (panelBatalla != null) panelBatalla.SetActive(false);
        if (textoEstado != null)    { textoEstado.text = "";    textoEstado.gameObject.SetActive(false); }
        if (textoResultado != null) { textoResultado.text = ""; textoResultado.gameObject.SetActive(false); }
        if (botonPiedra != null) botonPiedra.onClick.AddListener(() => Elegir(EleccionPPS.Piedra));
        if (botonPapel != null) botonPapel.onClick.AddListener(() => Elegir(EleccionPPS.Papel));
        if (botonTijera != null) botonTijera.onClick.AddListener(() => Elegir(EleccionPPS.Tijera));
    }

    // Llamado por host cuando detecta colisión
    public void IniciarBatalla(int actorAtk, int actorDef, int idxFichaAtk, bool fBAtk, int idxFichaDef, bool fBDef)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_IniciarBatalla), RpcTarget.All,
            actorAtk, actorDef, idxFichaAtk, fBAtk, idxFichaDef, fBDef);
    }

    [PunRPC]
    private void RPC_IniciarBatalla(int actorAtk, int actorDef, int idxFichaAtk, bool fBAtk, int idxFichaDef, bool fBDef)
    {
        actorAtacante = actorAtk;
        actorDefensor = actorDef;
        indiceFichaAtacante = idxFichaAtk;
        fichaBAtacante = fBAtk;
        indiceFichaDefensor = idxFichaDef;
        fichaBDefensor = fBDef;
        eleccionAtacante = EleccionPPS.Ninguna;
        eleccionDefensor = EleccionPPS.Ninguna;

        bool participo = PhotonNetwork.LocalPlayer.ActorNumber == actorAtk
                      || PhotonNetwork.LocalPlayer.ActorNumber == actorDef;

        if (panelBatalla != null) panelBatalla.SetActive(true);
        if (textoEstado != null)    textoEstado.gameObject.SetActive(true);
        if (textoResultado != null) textoResultado.gameObject.SetActive(true);

        if (participo)
        {
            if (textoEstado != null) textoEstado.text = "¡Batalla! Elige tu jugada";
            if (textoResultado != null) textoResultado.text = "";
            HabilitarBotones(true);
        }
        else
        {
            if (textoEstado != null) textoEstado.text = "Batalla en curso...";
            HabilitarBotones(false);
        }
    }

    private void Elegir(EleccionPPS eleccion)
    {
        HabilitarBotones(false);
        if (textoEstado != null) textoEstado.text = $"Elegiste: {eleccion}. Esperando rival...";
        photonView.RPC(nameof(RPC_RecibirEleccion), RpcTarget.All,
            PhotonNetwork.LocalPlayer.ActorNumber, (int)eleccion);
    }

    [PunRPC]
    private void RPC_RecibirEleccion(int actor, int eleccionInt)
    {
        EleccionPPS eleccion = (EleccionPPS)eleccionInt;
        if (actor == actorAtacante) eleccionAtacante = eleccion;
        else if (actor == actorDefensor) eleccionDefensor = eleccion;

        // Solo host resuelve cuando ambos eligieron
        if (PhotonNetwork.IsMasterClient
            && eleccionAtacante != EleccionPPS.Ninguna
            && eleccionDefensor != EleccionPPS.Ninguna)
        {
            ResolverBatalla();
        }
    }

    private void ResolverBatalla()
    {
        // 0 = empate, 1 = atacante gana, 2 = defensor gana
        int ganador = 0;
        if (eleccionAtacante != eleccionDefensor)
        {
            if ((eleccionAtacante == EleccionPPS.Piedra && eleccionDefensor == EleccionPPS.Tijera)
             || (eleccionAtacante == EleccionPPS.Papel && eleccionDefensor == EleccionPPS.Piedra)
             || (eleccionAtacante == EleccionPPS.Tijera && eleccionDefensor == EleccionPPS.Papel))
                ganador = 1;
            else
                ganador = 2;
        }
        photonView.RPC(nameof(RPC_MostrarResultado), RpcTarget.All,
            ganador, (int)eleccionAtacante, (int)eleccionDefensor);
    }

    [PunRPC]
    private void RPC_MostrarResultado(int ganador, int elecAtk, int elecDef)
    {
        string resultado = ganador == 0 ? "EMPATE"
            : ganador == 1 ? "Atacante gana" : "Defensor gana";
        if (textoResultado != null)
            textoResultado.text = $"{(EleccionPPS)elecAtk} vs {(EleccionPPS)elecDef}\n{resultado}";

        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(AplicarResultado(ganador));
    }

    private IEnumerator AplicarResultado(int ganador)
    {
        yield return new WaitForSeconds(2f);

        // Mandar perdedor al inicio (ficha derrotada)
        if (ganador == 1) // atacante gana → defensor pierde → su ficha al inicio
            EnviarFichaAlInicio(indiceFichaDefensor, fichaBDefensor);
        else if (ganador == 2) // defensor gana → atacante pierde
            EnviarFichaAlInicio(indiceFichaAtacante, fichaBAtacante);

        photonView.RPC(nameof(RPC_CerrarBatalla), RpcTarget.All);

        // Avanzar turno
        gameManager.SiguienteTurno();
    }

    private void EnviarFichaAlInicio(int indiceFicha, bool esB)
    {
        photonView.RPC(nameof(RPC_FichaAlInicio), RpcTarget.All, indiceFicha, esB);
    }

    [PunRPC]
    private void RPC_FichaAlInicio(int indiceFicha, bool esB)
    {
        if (gameManager == null || indiceFicha < 0 || indiceFicha >= gameManager.todosLosJugadores.Count) return;
        var jugador = gameManager.todosLosJugadores[indiceFicha];

        if (esB && jugador.fichaB != null)
        {
            // Ficha B: vuelve a meta (su inicio)
            int idx = jugador.ruta.casillas.Count - 1;
            jugador.fichaB.indiceActual = idx;
            jugador.fichaB.transform.position = jugador.ruta.casillas[idx].position + Vector3.up * 0.5f;
        }
        else
        {
            // Ficha A: vuelve a inicio
            jugador.indiceActual = 0;
            jugador.transform.position = jugador.ruta.casillas[0].position + Vector3.up * 0.5f;
        }
    }

    [PunRPC]
    private void RPC_CerrarBatalla()
    {
        if (panelBatalla != null) panelBatalla.SetActive(false);
        if (textoEstado != null)    { textoEstado.text = "";    textoEstado.gameObject.SetActive(false); }
        if (textoResultado != null) { textoResultado.text = ""; textoResultado.gameObject.SetActive(false); }
    }

    private void HabilitarBotones(bool activo)
    {
        if (botonPiedra != null) botonPiedra.interactable = activo;
        if (botonPapel != null) botonPapel.interactable = activo;
        if (botonTijera != null) botonTijera.interactable = activo;
    }
}
