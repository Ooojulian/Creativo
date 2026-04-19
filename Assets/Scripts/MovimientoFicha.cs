using UnityEngine;
using System.Collections;

public class MovimientoFicha : MonoBehaviour
{
    public GestorDeRuta ruta;
    public int indiceActual = 0;
    public float velocidad = 5f;
    public GameManager gm;

    private bool enMovimiento = false;

    public void Avanzar(int cantidadPasos)
    {
        if (!enMovimiento)
        {
            StartCoroutine(MoverPorLasCasillas(cantidadPasos));
        }
    }

    IEnumerator MoverPorLasCasillas(int pasos)
    {
        enMovimiento = true;

        int metaFinal = indiceActual + pasos;

        if (metaFinal >= ruta.casillas.Count)
        {
            metaFinal = ruta.casillas.Count - 1;
        }

        while (indiceActual < metaFinal)
        {
            indiceActual++;
            Vector3 destino = ruta.casillas[indiceActual].position;
            destino.y += 0.5f;

            while (Vector3.Distance(transform.position, destino) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
        }

        enMovimiento = false;

        if (gm != null)
        {
            gm.SiguienteTurno();
        }

        Debug.Log("Turno terminado. Ficha en la casilla: " + indiceActual);
    }
}
