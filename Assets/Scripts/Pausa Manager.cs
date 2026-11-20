using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuPausaManager : MonoBehaviour
{
    public static MenuPausaManager Instance { get; private set; }

    [Header("Input Asset")]
    [SerializeField] private InputActionAsset assetControles;

    [Header("Panel Principal")]
    [SerializeField] private GameObject panelPausa;

    [Header("Botones")]
    [SerializeField] private Button botonReanudar;
    [SerializeField] private Button botonReiniciar;
    [SerializeField] private Button botonMenuPrincipal;

    [Header("Sonido al Pausar / Reanudar")]
    [Tooltip("Arrastra aquí el grupo 'UI' o 'SFX' de tu Audio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixerUI;
    [Tooltip("Sonido que se reproduce al abrir el menú de pausa")]
    [SerializeField] private AudioClip sfxAbrirPausa;
    [Tooltip("Sonido al cerrar el menú de pausa (si se deja vacío, reutiliza el de abrir)")]
    [SerializeField] private AudioClip sfxCerrarPausa;
    [Range(0f, 1f)][SerializeField] private float volumenSfxPausa = 0.8f;

    [Header("Ajustes de Escena")]
    [SerializeField] private string nombreEscenaMenu = "MenuPrincipal";

    public bool EstaPausado { get; private set; } = false;

    private InputAction accionPausa;
    private AudioSource sourceUIPausa;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Configurar reproductor 2D que funcione aunque Time.timeScale = 0
        sourceUIPausa = gameObject.AddComponent<AudioSource>();
        sourceUIPausa.playOnAwake = false;
        sourceUIPausa.spatialBlend = 0f;
        sourceUIPausa.ignoreListenerPause = true;
        if (grupoMixerUI != null)
        {
            sourceUIPausa.outputAudioMixerGroup = grupoMixerUI;
        }

        if (panelPausa != null) panelPausa.SetActive(false);

        if (botonReanudar != null) botonReanudar.onClick.AddListener(ReanudarJuego);
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(ReiniciarNivel);
        if (botonMenuPrincipal != null) botonMenuPrincipal.onClick.AddListener(IrAlMenuPrincipal);

        if (assetControles != null)
        {
            accionPausa = assetControles.FindAction("Interfaz/Pausa", false)
                          ?? assetControles.FindAction("Player/Pausa", false);
        }
    }

    private void OnEnable()
    {
        if (accionPausa != null)
        {
            accionPausa.Enable();
            accionPausa.performed += OnPausaPerformed;
        }
    }

    private void OnDisable()
    {
        if (accionPausa != null)
        {
            accionPausa.performed -= OnPausaPerformed;
            accionPausa.Disable();
        }
    }

    private void OnPausaPerformed(InputAction.CallbackContext ctx)
    {
        AlternarPausa();
    }

    public void AlternarPausa()
    {
        if (EstaPausado) ReanudarJuego();
        else PausarJuego();
    }

    public void PausarJuego()
    {
        if (Time.timeScale == 0f && !EstaPausado) return;

        EstaPausado = true;
        if (panelPausa != null) panelPausa.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reproducir sonido de apertura de pausa
        ReproducirSonidoPausa(sfxAbrirPausa);

        if (NivelMusicManager.Instance != null)
        {
            NivelMusicManager.Instance.EntrarEnPausa();
        }
    }

    public void ReanudarJuego()
    {
        EstaPausado = false;
        if (panelPausa != null) panelPausa.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        // Reproducir sonido de cierre de pausa (si no hay clip específico, usa el de abrir)
        AudioClip clipReanudar = (sfxCerrarPausa != null) ? sfxCerrarPausa : sfxAbrirPausa;
        ReproducirSonidoPausa(clipReanudar);

        if (NivelMusicManager.Instance != null)
        {
            NivelMusicManager.Instance.SalirDePausa();
        }
    }

    private void ReproducirSonidoPausa(AudioClip clip)
    {
        if (clip == null || sourceUIPausa == null) return;
        sourceUIPausa.pitch = 1f;
        sourceUIPausa.PlayOneShot(clip, volumenSfxPausa);
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.ReiniciarNivel();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.CargarEscena(nombreEscenaMenu);
        else
            SceneManager.LoadScene(nombreEscenaMenu);
    }
}