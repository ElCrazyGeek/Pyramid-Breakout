using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string nombreEscenaJuego = "GameScene";

    [Header("Fondo Oscuro para Submenús")]
    [Tooltip("Image negra con CanvasGroup colocada entre el menú principal y los paneles")]
    [SerializeField] private CanvasGroup fondoNegroPaneles;
    [Range(0f, 1f)][SerializeField] private float opacidadFondoNegro = 0.75f;
    [SerializeField] private float velocidadFadeFondo = 8f;

    [Header("Paneles / Submenús")]
    [SerializeField] private GameObject panelOpciones;
    [SerializeField] private GameObject panelCreditos;

    [SerializeField] private GameObject panelControles;

    [Header("Audio Mixer y Música del Menú")]
    [Tooltip("Arrastra aquí el grupo 'Musica' de tu MasterAudio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixerMusica;
    [Tooltip("Canción principal del menú")]
    [SerializeField] private AudioClip musicaMenu;
    [Range(0f, 1f)][SerializeField] private float volumenMusica = 0.7f;
    [SerializeField] private bool loopMusica = true;

    [Header("Transiciones de Audio (Segundos)")]
    [Min(0f)][SerializeField] private float duracionFadeEntrada = 1.0f;
    [Min(0.05f)][SerializeField] private float duracionFadeSalida = 0.45f;

    private Coroutine rutinaFadeFondo;
    private AudioSource sourceMusica;
    private Coroutine rutinaMusica;
    private bool cargandoJuego = false;

    private void Awake()
    {
        // 1. Inicializar paneles y cortina oscura
        if (fondoNegroPaneles != null)
        {
            fondoNegroPaneles.alpha = 0f;
            fondoNegroPaneles.blocksRaycasts = false;
            fondoNegroPaneles.gameObject.SetActive(false);
        }

        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelCreditos != null) panelCreditos.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);

        // 2. Configurar reproductor de música en 2D enlazado al Audio Mixer
        sourceMusica = gameObject.AddComponent<AudioSource>();
        sourceMusica.clip = musicaMenu;
        sourceMusica.loop = loopMusica;
        sourceMusica.playOnAwake = false;
        sourceMusica.spatialBlend = 0f;
        sourceMusica.volume = 0f;

        if (grupoMixerMusica != null)
        {
            sourceMusica.outputAudioMixerGroup = grupoMixerMusica;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (musicaMenu != null && sourceMusica != null)
        {
            sourceMusica.Play();
            if (duracionFadeEntrada > 0f)
            {
                rutinaMusica = StartCoroutine(FadeMusica(0f, volumenMusica, duracionFadeEntrada));
            }
            else
            {
                sourceMusica.volume = volumenMusica;
            }
        }
    }

    // 1. Botón JUGAR
    public void Jugar()
    {
        if (cargandoJuego) return;
        cargandoJuego = true;

        // Bajar suavemente la música del menú antes de cambiar de escena
        if (sourceMusica != null && sourceMusica.isPlaying)
        {
            if (rutinaMusica != null) StopCoroutine(rutinaMusica);
            StartCoroutine(FadeMusica(sourceMusica.volume, 0f, duracionFadeSalida));
        }

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.CargarEscena(nombreEscenaJuego);
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaJuego);
        }
    }

    // 2. Botón OPCIONES
    public void AbrirOpciones()
    {
        MostrarPanelConFondo(panelOpciones);
    }

    public void CerrarOpciones()
    {
        CerrarPanel(panelOpciones);
    }

    public void AbrirControles()
    {
        MostrarPanelConFondo(panelControles);
    }

    public void CerrarControles()
    {
        CerrarPanel(panelControles);
    }

    // 3. Botón CRÉDITOS
    public void AbrirCreditos()
    {
        MostrarPanelConFondo(panelCreditos);
    }

    public void CerrarCreditos()
    {
        CerrarPanel(panelCreditos);
    }

    public void MostrarPanelConFondo(GameObject panel)
    {
        if (panel == null) return;

        if (fondoNegroPaneles != null)
        {
            fondoNegroPaneles.gameObject.SetActive(true);
            fondoNegroPaneles.blocksRaycasts = true;

            int indicePanel = panel.transform.GetSiblingIndex();
            int indiceFondo = fondoNegroPaneles.transform.GetSiblingIndex();

            if (indiceFondo > indicePanel)
            {
                fondoNegroPaneles.transform.SetSiblingIndex(indicePanel);
            }

            AnimarFondoNegro(opacidadFondoNegro);
        }

        panel.SetActive(true);
    }

    public void CerrarPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);

        bool hayPanelAbierto = (panelOpciones != null && panelOpciones.activeSelf) ||
                               (panelCreditos != null && panelCreditos.activeSelf);

        if (!hayPanelAbierto && fondoNegroPaneles != null)
        {
            fondoNegroPaneles.blocksRaycasts = false;
            AnimarFondoNegro(0f);
        }
    }

    private void AnimarFondoNegro(float alphaObjetivo)
    {
        if (rutinaFadeFondo != null) StopCoroutine(rutinaFadeFondo);
        rutinaFadeFondo = StartCoroutine(RutinaFadeFondo(alphaObjetivo));
    }

    private IEnumerator RutinaFadeFondo(float objetivo)
    {
        while (!Mathf.Approximately(fondoNegroPaneles.alpha, objetivo))
        {
            fondoNegroPaneles.alpha = Mathf.MoveTowards(
                fondoNegroPaneles.alpha,
                objetivo,
                velocidadFadeFondo * Time.unscaledDeltaTime
            );
            yield return null;
        }

        fondoNegroPaneles.alpha = objetivo;

        if (objetivo <= 0f)
        {
            fondoNegroPaneles.gameObject.SetActive(false);
        }

        rutinaFadeFondo = null;
    }

    private IEnumerator FadeMusica(float volInicio, float volFin, float duracion)
    {
        float vel = 1f / Mathf.Max(0.05f, duracion);
        float t = 0f;

        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, vel * Time.unscaledDeltaTime);
            if (sourceMusica != null)
            {
                sourceMusica.volume = Mathf.Lerp(volInicio, volFin, t);
            }
            yield return null;
        }

        if (sourceMusica != null)
        {
            sourceMusica.volume = volFin;
            if (volFin <= 0f) sourceMusica.Stop();
        }
    }

    // 4. Botón SALIR
    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}