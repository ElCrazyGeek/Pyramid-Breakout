using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelFinalUI : MonoBehaviour
{
    public static PanelFinalUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Se asegura de arrancar oculto al iniciar el juego
        gameObject.SetActive(false);
    }

    public void Mostrar()
    {
        gameObject.SetActive(true);

        // 1. Detener el reloj de Unity por completo
        Time.timeScale = 0f;

        // 2. Liberar el puntero del ratón para poder hacer clic
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Método a conectar en el botón Reiniciar
    public void ReiniciarNivel()
    {
        // SIEMPRE reanudar el tiempo antes de recargar
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Método a conectar en el botón Menú Principal
    public void IrAlMenuPrincipal(string nombreEscenaMenu = "MenuPrincipal")
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}