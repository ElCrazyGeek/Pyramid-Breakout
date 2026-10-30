using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Panel Game Over")]
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private CanvasGroup grupoGameOver; // Opcional para transición suave

    [Header("Botones")]
    [SerializeField] private Button botonReiniciar;
    [SerializeField] private Button botonMenuPrincipal;

    [Header("Ajustes")]
    [SerializeField] private string nombreEscenaMenu = "MenuPrincipal";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panelGameOver != null) panelGameOver.SetActive(false);

        // Asignar listeners por código
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(ReiniciarNivel);
        if (botonMenuPrincipal != null) botonMenuPrincipal.onClick.AddListener(IrAlMenuPrincipal);
    }

    public void MostrarGameOver()
    {
        if (panelGameOver == null) return;

        panelGameOver.SetActive(true);

        // Desbloquear cursor para que el jugador pueda interactuar con los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (grupoGameOver != null)
        {
            StartCoroutine(FadeEntrada());
        }
        else
        {
            // Pausar el juego de inmediato
            Time.timeScale = 0f;
        }
    }

    private IEnumerator FadeEntrada()
    {
        grupoGameOver.alpha = 0f;
        while (grupoGameOver.alpha < 1f)
        {
            grupoGameOver.alpha += Time.unscaledDeltaTime * 2f;
            yield return null;
        }
        grupoGameOver.alpha = 1f;
        Time.timeScale = 0f;
    }

    public void ReiniciarNivel()
    {
        // Reanudar el tiempo del motor antes de recargar
        Time.timeScale = 1f;
        string escenaActual = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(escenaActual);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nombreEscenaMenu))
        {
            SceneManager.LoadScene(nombreEscenaMenu);
        }
        else
        {
            Debug.LogWarning("No se asignó el nombre de la escena de menú principal.");
        }
    }
}