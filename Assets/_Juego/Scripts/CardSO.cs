using UnityEngine;

public enum CardType
{
    AvanceRapido,
    Retroceso,
    DobleTiro,
    PierdeTurno,
    Escudo,
    Recuperacion,
    Fatiga,
    RoboArcano,
    Ruptura,
    Intercambio,
    AuraMagica,
    VisionDelSabio,
    Maldicion,
    Silencio
}

[CreateAssetMenu(fileName = "New Card", menuName = "Cartas/Nueva Carta")]
public class CardSO : ScriptableObject
{
    public CardType type;
    public string cardName;
    [TextArea] public string immediateEffectDescription;
    [TextArea] public string savedConditionDescription;
    [TextArea] public string savedEffectDescription;
    public Sprite artwork;
    public int costoEnergia = 1;
    public int valor1;
}
