using UnityEngine;
using UnityEngine.UI;

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

    [Header("Rango de la Mirilla Lejana (Pantalla)")]
    [Tooltip("Rango en píxeles desde el centro de la pantalla (0, 0)")]
    [SerializeField] private Vector2 rangoLejana = new Vector2(320f, 180f);
    [SerializeField] private float velocidadMirilla = 18f;

    [Header("Recentrado al Centro de la Pantalla")]
    [Tooltip("Fuerza lenta y sutil que jala la mira constantemente hacia el centro")]
    [SerializeField] private float atraccionAlCentro = 2.0f;
    [Tooltip("Velocidad elástica de retorno cuando sueltas el stick")]
    [SerializeField] private float velocidadRetornoSoltado = 12f;

    [Header("Geometría Star Fox (Mirilla Cercana)")]
    [Range(0.1f, 0.9f)]
    [Tooltip("0.5 la coloca exactamente en la mitad de la línea entre la nave y la mira exterior")]
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
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;

        // 1. Obtener input activo y Transform de la nave
        Vector2 input = Vector2.zero;
        Transform naveActiva = null;

        if (playerLibre != null && playerLibre.enabled)
        {
            input = playerLibre.PlayerInput;
            naveActiva = playerLibre.transform;
        }
        else if (playerRieles != null && playerRieles.enabled)
        {
            input = playerRieles.PlayerInput;
            naveActiva = playerRieles.transform;
        }

        bool hayInput = input.sqrMagnitude > 0.01f;

        // 2. Mover la MIRILLA LEJANA respecto al centro de pantalla (0, 0)
        Vector2 targetLejana = new Vector2(input.x * rangoLejana.x, input.y * rangoLejana.y);

        if (hayInput)
        {
            mirillaLejana.anchoredPosition = Vector2.Lerp(mirillaLejana.anchoredPosition, targetLejana, velocidadMirilla * Time.deltaTime);
            // Atracción constante y suave al origen
            mirillaLejana.anchoredPosition = Vector2.MoveTowards(mirillaLejana.anchoredPosition, Vector2.zero, atraccionAlCentro * 100f * Time.deltaTime);
        }
        else
        {
            // Retorno fluido al soltar el stick
            mirillaLejana.anchoredPosition = Vector2.Lerp(mirillaLejana.anchoredPosition, Vector2.zero, velocidadRetornoSoltado * Time.deltaTime);
        }

        // 3. CALCULAR MIRILLA CERCANA: Punto intermedio entre la Nave y la Mirilla Lejana
        if (naveActiva != null && mirillaCercana != null && canvasPadre != null)
        {
            // Convertimos la posición 3D de la nave a coordenadas locales de Canvas (UI)
            Vector2 screenPosNave = cam.WorldToScreenPoint(naveActiva.position);
            RectTransform canvasRect = canvasPadre.transform as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosNave,
                canvasPadre.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out Vector2 localPosNave
            );

            // La posición de la mirilla cercana es la interpolación exacta entre la nave y la mira exterior
            Vector2 posIntermedia = Vector2.Lerp(localPosNave, mirillaLejana.anchoredPosition, factorIntermedio);
            mirillaCercana.anchoredPosition = posIntermedia;
        }

        // 4. Raycast de impacto al mundo 3D y detección de enemigos
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