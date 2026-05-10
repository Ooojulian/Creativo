using UnityEngine;

public enum CardType
{
    Pisoton,
    Desvio,
    LadronDeTurno,
    Olvido,
    Inspiracion,
    Espionaje,
    Sprint
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
}
