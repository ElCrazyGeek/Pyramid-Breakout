using System.Collections;
using UnityEngine;

public class ControladorHUDGameplay : MonoBehaviour
{
    public static ControladorHUDGameplay Instance { get; private set; }

    [Header("Referencias UI")]
    public CanvasGroup grupoHUD; // Panel de vida con CanvasGroup

    [Header("Configuración del Fade")]
    public float duracionFade = 0.5f;

    private Coroutine rutinaFade;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inicializar apagado por defecto si las cinemáticas arrancan primero
        if (grupoHUD != null)
        {
            grupoHUD.alpha = 0f;
            grupoHUD.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra el HUD de vida con fade de 0 a 100%.
    /// </summary>
    public void MostrarHUD()
    {
        if (grupoHUD == null) return;
        if (rutinaFade != null) StopCoroutine(rutinaFade);
        rutinaFade = StartCoroutine(ProcesarFade(1f));
    }

    /// <summary>
    /// Oculta el HUD de vida con fade de 100 a 0%.
    /// </summary>
    public void OcultarHUD()
    {
        if (grupoHUD == null) return;
        if (rutinaFade != null) StopCoroutine(rutinaFade);
        rutinaFade = StartCoroutine(ProcesarFade(0f));
    }

    private IEnumerator ProcesarFade(float alphaObjetivo)
    {
        grupoHUD.gameObject.SetActive(true);

        float velocidad = 1f / (duracionFade > 0f ? duracionFade : 0.1f);

        // Se usa unscaledDeltaTime para que funcione aunque el juego esté pausado (Time.timeScale = 0)
        while (!Mathf.Approximately(grupoHUD.alpha, alphaObjetivo))
        {
            grupoHUD.alpha = Mathf.MoveTowards(grupoHUD.alpha, alphaObjetivo, velocidad * Time.unscaledDeltaTime);
            yield return null;
        }

        grupoHUD.alpha = alphaObjetivo;

        // Si se ocultó por completo, desactiva el GameObject para optimizar rendimiento
        if (alphaObjetivo <= 0f)
        {
            grupoHUD.gameObject.SetActive(false);
        }

        rutinaFade = null;
    }
}