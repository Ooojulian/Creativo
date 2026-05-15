using UnityEngine;
using System.Collections;

// Ficha que va de meta -> inicio (dirección inversa al MovimientoFicha normal)
public class FichaInversa : MonoBehaviour
{
    private GameManager gameManager;
    public int indiceActual;
    public float velocidad = 150f;

    private bool enMovimiento = false;
    private CamaraDirectora camaraDirectora;

    void Awake() 
    { 
        camaraDirectora = FindAnyObjectByType<CamaraDirectora>(); 
    }
    
    void Start() 
    { 
        gameManager = FindAnyObjectByType<GameManager>();
        Inicializar(); 
    }
    
    void OnEnable() 
    { 
        Inicializar(); 
    }

    public void Inicializar()
    {
        if (gameManager != null && gameManager.casillas.Count > 0)
        {
            indiceActual = gameManager.casillas.Count - 1;
            transform.position = gameManager.casillas[indiceActual].position + Vector3.up * 0.5f;
        }
    }

    public void Avanzar(int pasos)
    {
        if (enMovimiento) return;

        int casillasRestantes = indiceActual;

        if (pasos > casillasRestantes)
        {
            Debug.Log($"[FichaInversa] {name}: necesita {casillasRestantes} o menos, sacó {pasos}. Turno perdido.");
            if (gameManager != null) gameManager.SiguienteTurno();
            return;
        }

        StartCoroutine(MoverHaciaInicio(pasos));
    }

    IEnumerator MoverHaciaInicio(int pasos)
    {
        enMovimiento = true;

        if (gameManager != null && gameManager.dado != null) gameManager.dado.gameObject.SetActive(false);
        if (camaraDirectora != null) camaraDirectora.SeguirJugador(transform);

        int metaFinal = indiceActual - pasos;
        if (metaFinal < 0) metaFinal = 0;

        while (indiceActual > metaFinal)
        {
            indiceActual--;

            Vector3 destino = gameManager.casillas[indiceActual].position + Vector3.up * 0.5f;

            while (Vector3.Distance(transform.position, destino) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
                yield return null;
            }

            transform.position = destino;
            yield return new WaitForSeconds(0.08f);
        }

        enMovimiento = false;
        if (camaraDirectora != null) camaraDirectora.VolverAlTablero();
        Debug.Log($"[FichaInversa] {name} llegó a casilla {indiceActual}");

        bool llegóAlInicio = indiceActual <= 0;
        if (llegóAlInicio)
        {
            Debug.Log($"[FichaInversa] {name} llegó al inicio.");
        }
        else
        {
            if (gameManager != null) gameManager.SiguienteTurno();
        }
    }
}