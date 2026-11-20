using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelFinalUI : MonoBehaviour
{
    public static PanelFinalUI Instance { get; private set; }

    [Header("Transición de Entrada del Panel")]
    [SerializeField] private CanvasGroup grupoPanel;
    [SerializeField] private float duracionFadeEntrada = 0.4f;

    [Header("Escenas")]
    [SerializeField] private string nombreEscenaMenu = "MenuPrincipal";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (grupoPanel == null)
        {
            grupoPanel = GetComponent<CanvasGroup>();
        }

        // Arranca oculto al iniciar la partida
        gameObject.SetActive(false);
    }

    public void Mostrar()
    {
        gameObject.SetActive(true);

        // 1. Detener el reloj de Unity por completo
        Time.timeScale = 0f;

        // 2. Liberar el puntero del ratón para poder hacer clic en los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Hacer aparecer el panel suavemente y habilitar clics
        if (grupoPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeEntrada());
        }
    }

    private IEnumerator FadeEntrada()
    {
        grupoPanel.alpha = 0f;
        grupoPanel.interactable = false;
        grupoPanel.blocksRaycasts = true;

        float velocidad = 1f / Mathf.Max(0.05f, duracionFadeEntrada);
        while (grupoPanel.alpha < 1f)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            grupoPanel.alpha = Mathf.MoveTowards(grupoPanel.alpha, 1f, velocidad * Time.unscaledDeltaTime);
            yield return null;
        }

        grupoPanel.alpha = 1f;
        grupoPanel.interactable = true; // Garantiza que los botones se puedan pulsar
    }

    // Método a conectar en el botón Reiniciar
    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;

        // Volver a ocultar y confinar el cursor para la mirilla del gameplay
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

    // Método a conectar en el botón Menú Principal
    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;

        // Dejar el cursor visible y libre para el menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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