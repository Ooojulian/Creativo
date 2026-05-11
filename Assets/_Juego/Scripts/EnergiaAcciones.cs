using UnityEngine;

/// <summary>
/// Proporciona métodos para gastar energía en acciones especiales.
/// </summary>
public class EnergiaAcciones : MonoBehaviour
{
    [Header("Costos")]
    public int costoBoostDado = 2;
    public int costoDobleMovimiento = 4;

    public void AplicarBoostDado()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.JugadorActual == null) return;

        var ctrl = gm.JugadorActual.GetComponent<EnergiaController>();
        if (ctrl != null && ctrl.GastarEnergia(costoBoostDado))
        {
            if (gm.dado != null)
            {
                gm.dado.modificadorExterno += 2;
                Debug.Log($"[Energia] Boost de dado (+2) aplicado a {gm.JugadorActual.name}");
            }
        }
    }

    public void ComprarDobleMovimiento()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.JugadorActual == null) return;

        var ctrl = gm.JugadorActual.GetComponent<EnergiaController>();
        if (ctrl != null && ctrl.GastarEnergia(costoDobleMovimiento))
        {
            gm.JugadorActual.dobleTiroPendiente = true;
            Debug.Log($"[Energia] Doble movimiento comprado por {gm.JugadorActual.name}");
        }
    }

    public bool IntentarJugarCarta(CardSO carta, MovimientoFicha usuario)
    {
        var ctrl = usuario.GetComponent<EnergiaController>();
        if (ctrl != null)
        {
            return ctrl.GastarEnergia(carta.costoEnergia);
        }
        return true; // Si no hay sistema de energía, permitir jugar
    }
}
