using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MirillaUI : MonoBehaviour
{
    [Header("Controladores del Jugador")]
    [SerializeField] private Player_mundolibre playerLibre;
    [SerializeField] private Player_rieles playerRieles;

    [Header("Referencias UI")]
    [Tooltip("La cruz/recuadro exterior grande (donde convergen los disparos)")]
    [SerializeField] private RectTransform mirillaLejana;
    [Tooltip("El recuadro interior pequeño")]
    [SerializeField] private RectTransform mirillaCercana;

    [Header("Límites de Pantalla (Píxeles desde el centro)")]
    [Tooltip("Rango máximo en píxeles que puede alejarse del centro (0, 0)")]
    [SerializeField] private Vector2 rangoLejana = new Vector2(360f, 220f);

    [Header("Sensibilidad Input System (Mouse y Stick Derecho)")]
    [Tooltip("Sensibilidad al mover el ratón (Delta)")]
    [SerializeField] private float sensibilidadMouse = 0.8f;
    [Tooltip("Sensibilidad para el Stick Derecho (-1 a 1 continuo)")]
    [SerializeField] private float sensibilidadStick = 500f;
    [SerializeField] private float suavizadoMirilla = 20f;

    [Header("Retraso y Recentrado al Centro")]
    [Tooltip("Tiempo en segundos que la mirilla espera quieta antes de empezar a regresar al centro")]
    [SerializeField] private float tiempoEsperaRecentrado = 0.35f;
    [Tooltip("Fuerza continua muy sutil hacia el centro mientras estás apuntando")]
    [SerializeField] private float atraccionAlCentro = 1.2f;
    [Tooltip("Velocidad de recuperación elástica una vez expira el tiempo de espera")]
    [SerializeField] private float velocidadRetornoSoltado = 8f;

    [Header("Geometría Star Fox (Mirilla Cercana)")]
    [Range(0.1f, 0.9f)]
    [Tooltip("0.5 la coloca exactamente a mitad de camino entre la nave y la mira exterior")]
    [SerializeField] private float factorIntermedio = 0.5f;

    [Header("Detección de Enemigos (Feedback)")]
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private float distanciaDeteccion = 300f;
    [SerializeField] private float factorEscalaObjetivo = 1.35f;
    [SerializeField] private float velocidadEscala = 12f;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorFijado = Color.red;

    private Canvas canvasPadre;
    private Image imagenLejana;
    private Vector3 escalaBaseLejana;
    private Camera cam;

    private Vector2 posTargetLejana = Vector2.zero;
    private Vector2 inputApuntadoActual = Vector2.zero;

    // Cronómetro interno para el retardo de recentrado
    private float tiempoSinInput = 0f;

    public bool ApuntandoEnemigo { get; private set; } = false;
    public Vector3 PuntoMundialImpacto { get; private set; }

    void Awake()
    {
        canvasPadre = GetComponentInParent<Canvas>();
        if (mirillaLejana != null)
        {
            imagenLejana = mirillaLejana.GetComponent<Image>();
            escalaBaseLejana = mirillaLejana.localScale;
        }
    }

    void Start()
    {
        cam = Camera.main;
        if (imagenLejana != null) imagenLejana.color = colorNormal;
        posTargetLejana = Vector2.zero;
    }

    public void OnApuntar(InputValue value)
    {
        inputApuntadoActual = value.Get<Vector2>();
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;

        // 1. Obtener la nave activa según el modo
        Transform naveActiva = null;
        if (playerLibre != null && playerLibre.enabled)
        {
            naveActiva = playerLibre.transform;
        }
        else if (playerRieles != null && playerRieles.enabled)
        {
            naveActiva = playerRieles.transform;
        }

        // 2. Comprobar actividad de entrada
        bool hayInput = inputApuntadoActual.sqrMagnitude > 0.001f;

        if (hayInput)
        {
            // Resetear el cronómetro de inactividad mientras el jugador mueva el control
            tiempoSinInput = 0f;

            if (inputApuntadoActual.sqrMagnitude <= 1.05f)
            {
                posTargetLejana += inputApuntadoActual * sensibilidadStick * Time.deltaTime;
            }
            else
            {
                posTargetLejana += inputApuntadoActual * sensibilidadMouse;
            }

            // Clamping dentro del cuadro de pantalla
            posTargetLejana.x = Mathf.Clamp(posTargetLejana.x, -rangoLejana.x, rangoLejana.x);
            posTargetLejana.y = Mathf.Clamp(posTargetLejana.y, -rangoLejana.y, rangoLejana.y);

            // Resistencia continua muy leve mientras apuntas
            if (atraccionAlCentro > 0f)
            {
                posTargetLejana = Vector2.MoveTowards(posTargetLejana, Vector2.zero, atraccionAlCentro * 100f * Time.deltaTime);
            }
        }
        else
        {
            // Acumular tiempo de inactividad
            tiempoSinInput += Time.deltaTime;

            // Solo después de superar el margen de gracia, la mirilla empieza a viajar al centro
            if (tiempoSinInput >= tiempoEsperaRecentrado)
            {
                posTargetLejana = Vector2.Lerp(posTargetLejana, Vector2.zero, velocidadRetornoSoltado * Time.deltaTime);
            }
        }

        // 3. Suavizado en la mirilla lejana
        if (mirillaLejana != null)
        {
            mirillaLejana.anchoredPosition = Vector2.Lerp(mirillaLejana.anchoredPosition, posTargetLejana, suavizadoMirilla * Time.deltaTime);
        }

        // 4. Mirilla Cercana (Perspectiva Star Fox)
        if (naveActiva != null && mirillaCercana != null && canvasPadre != null && cam != null)
        {
            Vector3 screenPosNave = cam.WorldToScreenPoint(naveActiva.position);
            RectTransform canvasRect = canvasPadre.transform as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosNave,
                canvasPadre.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out Vector2 localPosNave
            );

            Vector2 posIntermedia = Vector2.Lerp(localPosNave, mirillaLejana.anchoredPosition, factorIntermedio);
            mirillaCercana.anchoredPosition = posIntermedia;
        }

        // 5. Raycast de impacto
        ActualizarRayoMundial();
    }

    private void ActualizarRayoMundial()
    {
        if (cam == null || mirillaLejana == null) return;

        Ray rayo = RectTransformUtility.ScreenPointToRay(cam, mirillaLejana.position);
        ApuntandoEnemigo = Physics.Raycast(rayo, out RaycastHit hit, distanciaDeteccion, capaEnemigos);

        PuntoMundialImpacto = ApuntandoEnemigo ? hit.point : rayo.GetPoint(distanciaDeteccion);

        Vector3 escalaObjetivo = ApuntandoEnemigo ? (escalaBaseLejana * factorEscalaObjetivo) : escalaBaseLejana;
        mirillaLejana.localScale = Vector3.Lerp(mirillaLejana.localScale, escalaObjetivo, velocidadEscala * Time.deltaTime);

        if (imagenLejana != null)
        {
            Color colorObjetivo = ApuntandoEnemigo ? colorFijado : colorNormal;
            imagenLejana.color = Color.Lerp(imagenLejana.color, colorObjetivo, velocidadEscala * Time.deltaTime);
        }
    }
}