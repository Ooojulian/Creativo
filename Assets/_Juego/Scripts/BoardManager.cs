using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Ruta del Tablero")]
    public List<BoardNode> path = new List<BoardNode>();

    // Esta función te permitirá vincular automáticamente todas las casillas desde el Editor sin hacerlo a mano
    [ContextMenu("Auto-vincular Nodos por Hijos")]
    public void AutoLinkNodes()
    {
        path.Clear();
        // Obtiene todos los objetos hijos que tengan el script BoardNode
        BoardNode[] nodes = GetComponentsInChildren<BoardNode>();
        
        for(int i = 0; i < nodes.Length; i++)
        {
            path.Add(nodes[i]);
            // Vinculamos el nodo actual con el siguiente
            if (i < nodes.Length - 1)
            {
                nodes[i].nextNode = nodes[i + 1];
            }
            else
            {
                nodes[i].nextNode = null; // Meta final
            }
            
            // Asignamos automáticamente si es el primero o el último
            if (i == 0) nodes[i].nodeType = BoardNode.NodeType.Start;
            if (i == nodes.Length - 1) nodes[i].nodeType = BoardNode.NodeType.Finish;
        }

        Debug.Log($"¡Se han vinculado {path.Count} casillas exitosamente! Revisa las conexiones.");
    }

    // Dibuja unas líneas verdes en la vista de escena (Scene) para que veas el camino de las casillas
    private void OnDrawGizmos()
    {
        if (path == null || path.Count < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (path[i] != null && path[i + 1] != null)
            {
                // Dibuja una línea verde desde un nodo al siguiente
                Gizmos.DrawLine(path[i].transform.position, path[i+1].transform.position);
            }
        }
    }
}
