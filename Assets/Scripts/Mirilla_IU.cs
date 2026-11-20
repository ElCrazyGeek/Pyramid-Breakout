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
    [SerializeField] private Vector2 rangoLejana = new Vector2(450f, 260f);

    [Header("Control con Mouse (Posición directa)")]
    [SerializeField] private bool usarPosicionDirectaMouse = true;
    [SerializeField] private float suavizadoMouse = 35f;

    [Header("Control con Stick Derecho (Gamepad)")]
    [SerializeField] private float sensibilidadStick = 500f;
    [SerializeField] private float tiempoEsperaRecentrado = 0.35f;
    [SerializeField] private float velocidadRetornoSoltado = 8f;

    [Header("Geometría Star Fox (Mirilla Cercana)")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float factorIntermedio = 0.5f;

    [Header("Detección de Enemigos (Feedback)")]
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private float distanciaDeteccion = 300f;
    [SerializeField] private float factorEscalaObjetivo = 1.35f;
    [SerializeField] private float velocidadEscala = 12f;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorFijado = Color.red;

    private Canvas canvasPadre;
    private RectTransform canvasRect;
    private Image imagenLejana;
    private Vector3 escalaBaseLejana;
    private Camera cam;

    private Vector2 posTargetLejana = Vector2.zero;
    private Vector2 inputStickActual = Vector2.zero;
    private Vector2 ultimoMousePos;
    private bool usandoMouse = true;
    private float tiempoSinInputStick = 0f;

    public bool ApuntandoEnemigo { get; private set; } = false;
    public Vector3 PuntoMundialImpacto { get; private set; }

    void Awake()
    {
        canvasPadre = GetComponentInParent<Canvas>();
        if (canvasPadre != null)
        {
            canvasRect = canvasPadre.transform as RectTransform;
        }

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

        if (Mouse.current != null)
        {
            ultimoMousePos = Mouse.current.position.ReadValue();
        }
    }

    void OnEnable()
    {
        // Al activarse la mirilla en gameplay, oculta el cursor del sistema y lo confina a la ventana
        if (Time.timeScale > 0f)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

    public void OnApuntar(InputValue value)
    {
        Vector2 lectura = value.Get<Vector2>();
        if (lectura.sqrMagnitude <= 1.05f && lectura.sqrMagnitude > 0.01f)
        {
            inputStickActual = lectura;
            usandoMouse = false;
        }
        else if (lectura.sqrMagnitude <= 0.01f)
        {
            inputStickActual = Vector2.zero;
        }
    }

    void Update()
    {
        // Si el juego está pausado o hay un panel de Game Over / Final / Pausa abierto, dejar el cursor libre y visible
        bool menuAbierto = (Time.timeScale <= 0f)
                           || (MenuPausaManager.Instance != null && MenuPausaManager.Instance.EstaPausado)
                           || (PanelFinalUI.Instance != null && PanelFinalUI.Instance.gameObject.activeInHierarchy)
                           || (GameOverManager.Instance != null && GameOverManager.Instance.EstaActivo);

        if (menuAbierto)
        {
            if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            return;
        }

        // Asegurar que durante el gameplay el puntero del ratón permanezca invisible y confinado
        if (Cursor.visible || Cursor.lockState != CursorLockMode.Confined)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
        // Si el juego está en pausa o menú (Time.timeScale == 0), no actualizar la mirilla ni ocultar el mouse
        if (Time.timeScale <= 0f) return;

        // Asegurar que durante el gameplay el puntero del ratón permanezca invisible y dentro de la pantalla
        if (Cursor.visible || Cursor.lockState != CursorLockMode.Confined)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }

        if (cam == null) cam = Camera.main;

        Transform naveActiva = null;
        if (playerLibre != null && playerLibre.enabled) naveActiva = playerLibre.transform;
        else if (playerRieles != null && playerRieles.enabled) naveActiva = playerRieles.transform;

        // 1. Detectar si el jugador movió el mouse
        if (usarPosicionDirectaMouse && Mouse.current != null && canvasRect != null)
        {
            Vector2 mousePosActual = Mouse.current.position.ReadValue();
            if ((mousePosActual - ultimoMousePos).sqrMagnitude > 0.5f)
            {
                usandoMouse = true;
                ultimoMousePos = mousePosActual;
            }

            if (usandoMouse)
            {
                // Convertir la posición real del mouse en pantalla a coordenadas locales del Canvas
                Camera camCanvas = (canvasPadre.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePosActual, camCanvas, out Vector2 localMousePos))
                {
                    posTargetLejana.x = Mathf.Clamp(localMousePos.x, -rangoLejana.x, rangoLejana.x);
                    posTargetLejana.y = Mathf.Clamp(localMousePos.y, -rangoLejana.y, rangoLejana.y);
                }
            }
        }

        // 2. Si no está usando el mouse, procesar el stick derecho con recentrado retardado
        if (!usandoMouse)
        {
            if (inputStickActual.sqrMagnitude > 0.01f)
            {
                tiempoSinInputStick = 0f;
                posTargetLejana += inputStickActual * sensibilidadStick * Time.deltaTime;
                posTargetLejana.x = Mathf.Clamp(posTargetLejana.x, -rangoLejana.x, rangoLejana.x);
                posTargetLejana.y = Mathf.Clamp(posTargetLejana.y, -rangoLejana.y, rangoLejana.y);
            }
            else
            {
                tiempoSinInputStick += Time.deltaTime;
                if (tiempoSinInputStick >= tiempoEsperaRecentrado)
                {
                    posTargetLejana = Vector2.Lerp(posTargetLejana, Vector2.zero, velocidadRetornoSoltado * Time.deltaTime);
                }
            }
        }

        // 3. Posicionar Mirilla Lejana (justo en el centro del puntero invisibilizado)
        if (mirillaLejana != null)
        {
            float vel = usandoMouse ? suavizadoMouse : 20f;
            mirillaLejana.anchoredPosition = Vector2.Lerp(mirillaLejana.anchoredPosition, posTargetLejana, vel * Time.deltaTime);
        }

        // 4. Mirilla Cercana (Perspectiva Star Fox entre la nave y la mirilla exterior)
        if (naveActiva != null && mirillaCercana != null && canvasRect != null && cam != null)
        {
            Vector3 screenPosNave = cam.WorldToScreenPoint(naveActiva.position);
            Camera camCanvas = (canvasPadre.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosNave,
                camCanvas,
                out Vector2 localPosNave
            );

            mirillaCercana.anchoredPosition = Vector2.Lerp(localPosNave, mirillaLejana.anchoredPosition, factorIntermedio);
        }

        // 5. Raycast de impacto y feedback de enemigos
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