using UnityEngine;

public class MenuNiveles : MonoBehaviour
{
    public GameObject botonJugar;
    public GameObject contenedorModos;

    void Start()
    {
        // Al inicio, muestra solo "JUGAR"
        botonJugar.SetActive(true);
        contenedorModos.SetActive(false);
    }

    public void MostrarModos()
    {
        // Cuando haces clic en JUGAR, esconde el botón y muestra los modos
        botonJugar.SetActive(false);
        contenedorModos.SetActive(true);
    }

    public void Volver()
    {
        // Botón "Volver" para regresar a JUGAR (lo usaremos después)
        botonJugar.SetActive(true);
        contenedorModos.SetActive(false);
    }
}