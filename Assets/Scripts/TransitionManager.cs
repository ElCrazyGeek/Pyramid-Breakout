using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("UI Cortina")]
    [SerializeField] private CanvasGroup cortinaCanvasGroup;
    [SerializeField] private float duracionFade = 0.5f;

    [Header("UI Carga (Opcional)")]
    [Tooltip("Panel o icono de carga que se muestra durante la transición. Si no usas, déjalo vacío.")]
    [SerializeField] private GameObject contenedorCarga;
    [SerializeField] private RectTransform spinnerTransform;
    [SerializeField] private float velocidadGiroSpinner = 250f;

    private bool cargando = false;

    private void Awake()
    {
        // Singleton persistente entre escenas
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (cortinaCanvasGroup != null)
        {
            cortinaCanvasGroup.alpha = 0f;
            cortinaCanvasGroup.blocksRaycasts = false;
        }

        if (contenedorCarga != null)
        {
            contenedorCarga.SetActive(false);
        }
    }

    /// <summary>
    /// Carga cualquier escena asegurando que esté 100% lista antes de mostrarla.
    /// </summary>
    public void CargarEscena(string nombreEscena)
    {
        if (!cargando && !string.IsNullOrEmpty(nombreEscena))
        {
            StartCoroutine(RutinaTransicion(nombreEscena));
        }
    }

    /// <summary>
    /// Reinicia la escena activa sin cortes bruscos.
    /// </summary>
    public void ReiniciarNivel()
    {
        if (!cargando)
        {
            string escenaActual = SceneManager.GetActiveScene().name;
            StartCoroutine(RutinaTransicion(escenaActual));
        }
    }

    private IEnumerator RutinaTransicion(string nombreEscena)
    {
        cargando = true;
        Time.timeScale = 1f;

        // 1. FADE OUT: Pantalla a negro
        if (cortinaCanvasGroup != null)
        {
            cortinaCanvasGroup.blocksRaycasts = true;
            while (cortinaCanvasGroup.alpha < 1f)
            {
                cortinaCanvasGroup.alpha = Mathf.MoveTowards(
                    cortinaCanvasGroup.alpha,
                    1f,
                    (1f / duracionFade) * Time.unscaledDeltaTime
                );
                yield return null;
            }
            cortinaCanvasGroup.alpha = 1f;
        }

        // 2. Encender icono de carga si está asignado
        if (contenedorCarga != null)
        {
            contenedorCarga.SetActive(true);
        }

        // 3. CARGA ASÍNCRONA EN SEGUNDO PLANO
        AsyncOperation operacion = SceneManager.LoadSceneAsync(nombreEscena);
        operacion.allowSceneActivation = false;

        // Carga en memoria RAM hasta el 90%
        while (operacion.progress < 0.9f)
        {
            ActualizarVisualesCarga(operacion.progress / 0.9f);
            yield return null;
        }

        ActualizarVisualesCarga(1f);

        // 4. DAR LUZ VERDE PARA CAMBIAR DE ESCENA
        operacion.allowSceneActivation = true;

        while (!operacion.isDone)
        {
            ActualizarVisualesCarga(1f);
            yield return null;
        }

        // 5. APAGAR EL SPINNER Y FORZAR PANTALLA EN NEGRO TOTAL
        if (contenedorCarga != null)
        {
            contenedorCarga.SetActive(false);
        }

        if (cortinaCanvasGroup != null)
        {
            cortinaCanvasGroup.alpha = 1f; // Mantiene la pantalla 100% tapada
        }

        // 6. ESPERA CLAVE: Dar tiempo al generador de niveles y a Cinemachine
        // Esperamos a que LevelSession esté activa o al menos 1.5 a 2 segundos reales
        float tiempoEsperaSeguridad = 1.8f; // Tiempo para que cargue la skybox y asteroides
        float temporizador = 0f;

        while (temporizador < tiempoEsperaSeguridad)
        {
            temporizador += Time.unscaledDeltaTime;
            // Si tienes LevelSession y ya cargó, puedes acelerar la salida:
            // if (Pyramid.Levels.LevelSession.Active != null) { ... }
            yield return null;
        }

        // Esperar un último frame para refresco visual
        yield return new WaitForEndOfFrame();

        // 7. FADE IN: Ahora sí, revelar el juego ya armado y con las estrellas puestas
        if (cortinaCanvasGroup != null)
        {
            while (cortinaCanvasGroup.alpha > 0f)
            {
                cortinaCanvasGroup.alpha = Mathf.MoveTowards(
                    cortinaCanvasGroup.alpha,
                    0f,
                    (1f / duracionFade) * Time.unscaledDeltaTime
                );
                yield return null;
            }
            cortinaCanvasGroup.alpha = 0f;
            cortinaCanvasGroup.blocksRaycasts = false;
        }

        cargando = false;
    }

    private void ActualizarVisualesCarga(float progreso)
    {
        if (spinnerTransform != null)
        {
            spinnerTransform.Rotate(0f, 0f, -velocidadGiroSpinner * Time.unscaledDeltaTime);
        }
    }
}