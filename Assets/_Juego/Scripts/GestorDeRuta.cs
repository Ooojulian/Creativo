using UnityEngine;
using System.Collections.Generic;

public class GestorDeRuta : MonoBehaviour
{
    public List<Transform> casillas = new List<Transform>();

    private void OnDrawGizmos()
    {
        if (casillas.Count > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < casillas.Count - 1; i++)
            {
                if (casillas[i] != null && casillas[i + 1] != null)
                {
                    Gizmos.DrawLine(casillas[i].position, casillas[i + 1].position);
                }
            }
        }
    }
}
