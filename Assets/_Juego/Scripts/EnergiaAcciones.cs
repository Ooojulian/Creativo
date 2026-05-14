using UnityEngine;

public class EnergiaAcciones : MonoBehaviour
{
    public static EnergiaAcciones Instance;

    [Header("Costos")]
    public int costoTiradaExtra = 3;
    public int costoDobleMovimiento = 5;

    void Awake() { Instance = this; }

    // Relanza el dado después de ver el resultado. Solo disponible en estado esperandoConfirmacion.
    public void UsarTiradaExtra()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.JugadorActual == null) return;
        if (gm.dado == null || !gm.dado.EsperandoConfirmacion) return;

        var ctrl = gm.JugadorActual.GetComponent<EnergiaController>();
        if (ctrl != null && ctrl.GastarEnergia(costoTiradaExtra))
        {
            gm.dado.RelanzarDado();
            Debug.Log($"[Energia] Tirada extra usada por {gm.JugadorActual.name}");
        }
    }

    public void ComprarDobleMovimiento()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.JugadorActual == null) return;
        if (gm.dado == null || !gm.dado.EsperandoConfirmacion) return;

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
            return ctrl.GastarEnergia(carta.costoEnergia);
        return true;
    }
}
