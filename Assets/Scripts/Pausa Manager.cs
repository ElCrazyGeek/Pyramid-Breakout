using UnityEngine;
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

    [Header("Ajustes de Escena")]
    [SerializeField] private string nombreEscenaMenu = "MenuPrincipal";

    public bool EstaPausado { get; private set; } = false;

    private InputAction accionPausa;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panelPausa != null) panelPausa.SetActive(false);

        if (botonReanudar != null) botonReanudar.onClick.AddListener(ReanudarJuego);
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(ReiniciarNivel);
        if (botonMenuPrincipal != null) botonMenuPrincipal.onClick.AddListener(IrAlMenuPrincipal);

        if (assetControles != null)
        {
            accionPausa = assetControles.FindAction("Player/Pausa");
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
    }

    public void ReanudarJuego()
    {
        EstaPausado = false;
        if (panelPausa != null) panelPausa.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.CargarEscena(nombreEscenaMenu);
        else
            SceneManager.LoadScene(nombreEscenaMenu);
    }
}