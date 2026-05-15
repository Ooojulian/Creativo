using UnityEngine;
using TMPro;

public class EnergiaAcciones : MonoBehaviour
{
    public static EnergiaAcciones Instance;

    [Header("Costos")]
    public int costoBoostDado = 2;
    public int costoDobleMovimiento = 4;

    void Awake() { Instance = this; }

    // Añade +2 a la tirada del dado antes de lanzarlo o mientras espera confirmación.
    public void UsarBoostDado()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.JugadorActual == null) return;
        
        // Se puede usar antes de tirar o si ya tiró y está esperando confirmación (si permites modificar post-tiro)
        var ctrl = gm.JugadorActual.GetComponent<EnergiaController>();
        if (ctrl != null && ctrl.GastarEnergia(costoBoostDado))
        {
            if (gm.dado != null)
            {
                gm.dado.modificadorExterno += 2;
                Debug.Log($"[Energia] Boost +2 aplicado al dado por {gm.JugadorActual.name}");
                
                // Si ya tiró, refrescar el resultado visualmente si es posible
                if (gm.dado.EsperandoConfirmacion)
                {
                    gm.dado.resultadoActual += 2;
                    // Forzar actualización de texto si el dado lo permite (aquí asumo que resultadoActual es público)
                    if (gm.dado.textoResultado != null)
                        gm.dado.textoResultado.text = $"Dado: {gm.dado.resultadoActual}";
                }
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
            return ctrl.GastarEnergia(carta.costoEnergia);
        return true;
    }
}
