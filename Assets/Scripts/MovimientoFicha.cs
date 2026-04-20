using UnityEngine;
using System.Collections;

public class MovimientoFicha : MonoBehaviour
{
    public GestorDeRuta ruta;
    public int indiceActual = 0;
    public float velocidad = 150f;   // ~157 unidades entre casillas → ~1s por casilla
    public GameManager gm;

    private bool enMovimiento = false;

    public void Avanzar(int cantidadPasos)
    {
        if (enMovimiento) return;

        int casillasRestantes = ruta.casillas.Count - 1 - indiceActual;
        if (cantidadPasos > casillasRestantes)
        {
            Debug.Log($"[MovimientoFicha] {name}: necesita {casillasRestantes} o menos para avanzar, sacó {cantidadPasos}. Turno perdido.");
            if (gm != null) gm.SiguienteTurno();
            return;
        }

        StartCoroutine(MoverPorLasCasillas(cantidadPasos));
    }

    IEnumerator MoverPorLasCasillas(int pasos)
    {
        enMovimiento = true;

        // Ocultar dado mientras la ficha se mueve
        if (gm != null && gm.dado != null)
            gm.dado.gameObject.SetActive(false);

        // Validar referencias
        if (ruta == null || ruta.casillas == null || ruta.casillas.Count == 0)
        {
            Debug.LogError($"[MovimientoFicha] {name}: ruta no asignada o sin casillas.");
            enMovimiento = false;
            if (gm != null) gm.SiguienteTurno();
            yield break;
        }

        int metaFinal = indiceActual + pasos;
        if (metaFinal >= ruta.casillas.Count)
            metaFinal = ruta.casillas.Count - 1;

        while (indiceActual < metaFinal)
        {
            indiceActual++;

            if (ruta.casillas[indiceActual] == null)
            {
                Debug.LogWarning($"[MovimientoFicha] casilla[{indiceActual}] es null, saltando.");
                continue;
            }

            // La posición de la casilla es siempre world position
            Vector3 destino = ruta.casillas[indiceActual].position + Vector3.up * 0.5f;

            while (Vector3.Distance(transform.position, destino) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, destino, velocidad * Time.deltaTime);
                yield return null;
            }

            transform.position = destino;
            yield return new WaitForSeconds(0.08f);
        }

        enMovimiento = false;
        Debug.Log($"[MovimientoFicha] {name} llegó a casilla {indiceActual}");

        bool llegóAMeta = indiceActual >= ruta.casillas.Count - 1;
        if (gm != null)
        {
            if (llegóAMeta)
                gm.LlegarAMeta(this);
            else
                gm.SiguienteTurno();
        }
    }
}
