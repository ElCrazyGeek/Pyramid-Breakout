using UnityEngine;
using UnityEngine.UI;

public class MirillaUI : MonoBehaviour
{
    [Header("Controladores")]
    [SerializeField] private Player_mundolibre playerLibre;
    [SerializeField] private Player_rieles playerRieles;

    [Header("Referencias de las 2 Mirillas")]
    [Tooltip("El recuadro exterior grande (donde convergen las balas)")]
    [SerializeField] private RectTransform mirillaLejana;
    [Tooltip("El recuadro interior pequeño (más cercano a la nave)")]
    [SerializeField] private RectTransform mirillaCercana;

    [Header("Rangos de Movimiento")]
    [SerializeField] private Vector2 rangoLejana = new Vector2(300f, 180f);
    [Tooltip("La cercana recorre un poco más para abrir el ángulo visual 3D")]
    [SerializeField] private Vector2 rangoCercana = new Vector2(360f, 220f);

    [Header("Velocidades y Retorno")]
    [SerializeField] private float velocidadMirilla = 18f;
    [Tooltip("Fuerza continua y sutil que siempre jala ambas miras hacia el centro")]
    [SerializeField] private float atraccionAlCentro = 2.5f;
    [Tooltip("Velocidad de recuperación cuando sueltas por completo el stick")]
    [SerializeField] private float velocidadRetornoSoltado = 10f;

    [Header("Detección de Enemigos (Feedback)")]
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private float distanciaDeteccion = 300f;
    [SerializeField] private float factorEscalaObjetivo = 1.3f;
    [SerializeField] private float velocidadEscala = 12f;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorFijado = Color.red;

    private Image imagenLejana;
    private Vector3 escalaBaseLejana;
    private Camera cam;

    public bool ApuntandoEnemigo { get; private set; } = false;

    // Coordenada en el mundo 3D hacia donde apunta la mirilla lejana en el horizonte
    public Vector3 PuntoMundialImpacto { get; private set; }

    void Awake()
    {
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
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;

        // 1. Obtener el input actual
        Vector2 input = Vector2.zero;
        if (playerLibre != null && playerLibre.enabled)
        {
            input = playerLibre.PlayerInput;
        }
        else if (playerRieles != null && playerRieles.enabled)
        {
            input = playerRieles.PlayerInput;
        }

        bool hayInput = input.sqrMagnitude > 0.01f;

        // 2. Destinos deseados
        Vector2 targetLejana = new Vector2(input.x * rangoLejana.x, input.y * rangoLejana.y);
        Vector2 targetCercana = new Vector2(input.x * rangoCercana.x, input.y * rangoCercana.y);

        if (hayInput)
        {
            // Ambas cruces se desplazan hacia donde empujas la palanca
            mirillaLejana.anchoredPosition = Vector2.Lerp(mirillaLejana.anchoredPosition, targetLejana, velocidadMirilla * Time.deltaTime);
            mirillaCercana.anchoredPosition = Vector2.Lerp(mirillaCercana.anchoredPosition, targetCercana, (velocidadMirilla * 1.2f) * Time.deltaTime);

            // JALÓN CONTINUO AL CENTRO (resistencia sutil)
            mirillaLejana.anchoredPosition = Vector2.MoveTowards(mirillaLejana.anchoredPosition, Vector2.zero, atraccionAlCentro * 100f * Time.deltaTime);
            mirillaCercana.anchoredPosition = Vector2.MoveTowards(mirillaCercana.anchoredPosition, Vector2.zero, atraccionAlCentro * 100f * Time.deltaTime);
        }
        else
        {
            // Retorno elástico suave al centro exacto (0, 0)
            mirillaLejana.anchoredPosition = Vector2.Lerp(mirillaLejana.anchoredPosition, Vector2.zero, velocidadRetornoSoltado * Time.deltaTime);
            mirillaCercana.anchoredPosition = Vector2.Lerp(mirillaCercana.anchoredPosition, Vector2.zero, velocidadRetornoSoltado * Time.deltaTime);
        }

        // 3. Proyectar el punto de mira al mundo 3D y comprobar enemigos
        ActualizarRayoMundial();
    }

    private void ActualizarRayoMundial()
    {
        if (cam == null || mirillaLejana == null) return;

        Ray rayo = RectTransformUtility.ScreenPointToRay(cam, mirillaLejana.position);
        ApuntandoEnemigo = Physics.Raycast(rayo, out RaycastHit hit, distanciaDeteccion, capaEnemigos);

        PuntoMundialImpacto = ApuntandoEnemigo ? hit.point : rayo.GetPoint(distanciaDeteccion);

        // Feedback visual
        Vector3 escalaObjetivo = ApuntandoEnemigo ? (escalaBaseLejana * factorEscalaObjetivo) : escalaBaseLejana;
        mirillaLejana.localScale = Vector3.Lerp(mirillaLejana.localScale, escalaObjetivo, velocidadEscala * Time.deltaTime);

        if (imagenLejana != null)
        {
            Color colorObjetivo = ApuntandoEnemigo ? colorFijado : colorNormal;
            imagenLejana.color = Color.Lerp(imagenLejana.color, colorObjetivo, velocidadEscala * Time.deltaTime);
        }
    }
}