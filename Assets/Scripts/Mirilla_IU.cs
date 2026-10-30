using UnityEngine;
using UnityEngine.UI;

public class MirillaUI : MonoBehaviour
{
    [Header("Controladores del Jugador")]
    [SerializeField] private Player_mundolibre playerLibre;
    [SerializeField] private Player_rieles playerRieles;

    [Header("Rango de Movimiento en Pantalla")]
    [Tooltip("Distancia máxima en píxeles desde el centro de la pantalla")]
    [SerializeField] private float rangoPixelesX = 350f;
    [SerializeField] private float rangoPixelesY = 220f;

    [Header("Comportamiento Star Fox")]
    [SerializeField] private float velocidadApuntado = 18f;
    [SerializeField] private float velocidadRetorno = 12f;

    [Header("Detección de Enemigos (Feedback)")]
    [SerializeField] private LayerMask capaEnemigos;      // Selecciona la Layer 'Enemigo'
    [SerializeField] private float distanciaDeteccion = 300f;
    [SerializeField] private float factorEscalaObjetivo = 1.35f; // Crece un 35% sobre el enemigo
    [SerializeField] private float velocidadEscala = 12f;

    [Header("Colores de Apuntado (Opcional)")]
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorFijado = Color.red;

    // Componentes internos
    private RectTransform rectTransform;
    private Image imagenMirilla;
    private Camera cam;

    private Vector2 posicionTarget;
    private Vector3 escalaBase;
    public bool ApuntandoEnemigo { get; private set; } = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        imagenMirilla = GetComponent<Image>();
        escalaBase = rectTransform.localScale;
    }

    void Start()
    {
        cam = Camera.main;
        if (imagenMirilla != null)
        {
            imagenMirilla.color = colorNormal;
        }
    }

    void Update()
    {
        // 1. Obtener input del modo activo
        Vector2 input = Vector2.zero;
        if (playerLibre != null && playerLibre.enabled)
        {
            input = playerLibre.PlayerInput;
        }
        else if (playerRieles != null && playerRieles.enabled)
        {
            input = playerRieles.PlayerInput;
        }

        // 2. Mover la mirilla por la pantalla
        posicionTarget = new Vector2(input.x * rangoPixelesX, input.y * rangoPixelesY);
        float vel = (input.sqrMagnitude > 0.01f) ? velocidadApuntado : velocidadRetorno;
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, posicionTarget, vel * Time.deltaTime);

        // 3. Comprobar si hay un enemigo bajo la mirilla
        VerificarEnemigoEnMira();
    }

    private void VerificarEnemigoEnMira()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Trazamos un rayo desde la cámara a través del centro de la mirilla en la pantalla
        Ray rayo = RectTransformUtility.ScreenPointToRay(cam, rectTransform.position);

        // Comprobamos si el rayo atraviesa un objeto en la capa Enemigo
        ApuntandoEnemigo = Physics.Raycast(rayo, out RaycastHit hit, distanciaDeteccion, capaEnemigos);

        // 4. Feedback visual: Escala (crecer suavemente)
        Vector3 escalaObjetivo = ApuntandoEnemigo ? (escalaBase * factorEscalaObjetivo) : escalaBase;
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, escalaObjetivo, velocidadEscala * Time.deltaTime);

        // 5. Feedback visual: Color (opcional)
        if (imagenMirilla != null)
        {
            Color colorObjetivo = ApuntandoEnemigo ? colorFijado : colorNormal;
            imagenMirilla.color = Color.Lerp(imagenMirilla.color, colorObjetivo, velocidadEscala * Time.deltaTime);
        }
    }
}