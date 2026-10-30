using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string nombreEscenaJuego = "GameScene";

    [Header("Paneles / Submenús")]
    [SerializeField] private GameObject panelOpciones;
    [SerializeField] private GameObject panelCreditos; // <-- Referencia al panel de Créditos

    // 1. Botón JUGAR
    public void Jugar()
    {
        Debug.Log("Cargando el juego...");
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // 2. Botón OPCIONES
    public void AbrirOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    // 3. Botón CRÉDITOS
    public void AbrirCreditos()
    {
        if (panelCreditos != null) panelCreditos.SetActive(true);
    }

    public void CerrarCreditos()
    {
        if (panelCreditos != null) panelCreditos.SetActive(false);
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