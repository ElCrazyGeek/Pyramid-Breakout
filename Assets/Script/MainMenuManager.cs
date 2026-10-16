using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escena")]
    // Cambia "GameScene" por el nombre exacto de la escena de tu juego
    [SerializeField] private string nombreEscenaJuego = "GameScene";

    [Header("Paneles / Submenús")]
    [SerializeField] private GameObject panelOpciones;

    // 1. Botón JUGAR
    public void Jugar()
    {
        Debug.Log("Cargando el juego...");
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // 2. Botón OPCIONES (Abrir)
    public void AbrirOpciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(true);
        }
    }

    // 3. Botón VOLVER (Cerrar Opciones)
    public void CerrarOpciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(false);
        }
    }

    // 4. Botón SALIR
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}