using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Panel Principal")]
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private CanvasGroup grupoGameOver;

    public bool EstaActivo => panelGameOver != null && panelGameOver.activeInHierarchy;

    [Header("Textos Adaptables")]
    [Tooltip("El texto principal que dice 'GAME OVER' o 'GRACIAS POR JUGAR'")]
    [SerializeField] private TextMeshProUGUI textoTitulo;
    [Tooltip("Subtítulo opcional para créditos breves o felicitaciones")]
    [SerializeField] private TextMeshProUGUI textoSubtitulo;

    [Header("Botones")]
    [SerializeField] private Button botonReiniciar;
    [SerializeField] private Button botonMenuPrincipal;

    [Header("Escenas")]
    [SerializeField] private string nombreEscenaMenu = "MenuPrincipal";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panelGameOver != null) panelGameOver.SetActive(false);

        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(ReiniciarNivel);
        if (botonMenuPrincipal != null) botonMenuPrincipal.onClick.AddListener(IrAlMenuPrincipal);
    }

    // 1. Llamada tradicional de muerte
    // 1. Llamada tradicional de muerte
    public void MostrarGameOver()
    {
        if (textoTitulo != null)
        {
            textoTitulo.text = "GAME OVER";
            textoTitulo.color = Color.red;
        }

        if (textoSubtitulo != null)
        {
            textoSubtitulo.text = "La nave ha sido destruida.";
        }

        // ---> AQUÍ: Detener en seco la música del nivel y reproducir la de Game Over <---
        if (NivelMusicManager.Instance != null)
        {
            NivelMusicManager.Instance.ActivarMusicaGameOver();
        }

        ActivarPantalla();
    }

    // 2. Llamada de victoria / fin de demo
    public void MostrarVictoria(string mensajePrincipal = "¡GRACIAS POR JUGAR!", string subtitulo = "Fin de la Demostración")
    {
        if (textoTitulo != null)
        {
            textoTitulo.text = mensajePrincipal;
            textoTitulo.color = new Color(0.2f, 0.9f, 1f); // Azul cielo brillante
        }

        if (textoSubtitulo != null)
        {
            textoSubtitulo.text = subtitulo;
        }

        ActivarPantalla();
    }

    private void ActivarPantalla()
    {
        if (panelGameOver == null) return;

        panelGameOver.SetActive(true);

        // En pantalla de Game Over / Victoria: cursor visible y libre para hacer clic
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (grupoGameOver != null)
        {
            StartCoroutine(FadeEntrada());
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    private IEnumerator FadeEntrada()
    {
        grupoGameOver.alpha = 0f;
        while (grupoGameOver.alpha < 1f)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            grupoGameOver.alpha += Time.unscaledDeltaTime * 2.5f;
            yield return null;
        }
        grupoGameOver.alpha = 1f;
        Time.timeScale = 0f;
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;

        // Volvemos al gameplay: ocultar el cursor y confinarlo a la ventana para la mirilla
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.ReiniciarNivel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;

        // En el menú principal el cursor debe permanecer libre y visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (string.IsNullOrEmpty(nombreEscenaMenu))
        {
            Debug.LogWarning("No se definió el nombre de la escena del menú principal.");
            return;
        }

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.CargarEscena(nombreEscenaMenu);
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaMenu);
        }
    }
}