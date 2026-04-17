using UnityEngine;

public class BoardNode : MonoBehaviour
{
    public enum NodeType
    {
        Normal,
        DrawCard,
        Advance,
        Delay,
        Start,
        Finish
    }

    [Header("Configuración de la Casilla")]
    public NodeType nodeType = NodeType.Normal;

    [Header("Conexiones")]
    [Tooltip("La casilla a la que el jugador se moverá después de esta.")]
    public BoardNode nextNode;

    // Usaremos esta función para saber exactamente dónde colocar la ficha del jugador (un poco más arriba)
    public Vector3 GetNodePosition()
    {
        return transform.position + new Vector3(0, 0.5f, 0); 
    }
}
