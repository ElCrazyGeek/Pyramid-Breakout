using UnityEngine;
using UnityEngine.UI;

public class LockOnTargetUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camaraPrincipal;

    [Header("Estilo Visual")]
    [Tooltip("Velocidad del parpadeo del recuadro rojo")]
    [SerializeField] private float velocidadParpadeo = 12f;
    [Tooltip("Offset vertical en el mundo para que el recuadro quede centrado en el enemigo")]
    [SerializeField] private float offsetAlturaMundo = 0f;

    private RectTransform rectTransform;
    private Image imagenRecuadro;
    private Transform enemigoActual;
    private bool visible;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        imagenRecuadro = GetComponent<Image>();
    }

    void Start()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }
        Ocultar();
    }

    void LateUpdate()
    {
        if (!visible || enemigoActual == null)
        {
            if (visible) Ocultar();
            return;
        }

        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (camaraPrincipal == null) return;

        // 1. Convertir la posición 3D del enemigo a píxeles de pantalla
        Vector3 puntoEnemigo = enemigoActual.position + (Vector3.up * offsetAlturaMundo);
        Vector3 screenPos = camaraPrincipal.WorldToScreenPoint(puntoEnemigo);

        // 2. Comprobar si el objetivo está frente a la cámara (Z > 0)
        if (screenPos.z > 0)
        {
            rectTransform.position = new Vector3(screenPos.x, screenPos.y, 0f);

            // 3. Efecto parpadeo sutil estilo arcade
            if (imagenRecuadro != null)
            {
                float alfa = Mathf.PingPong(Time.time * velocidadParpadeo, 1f);
                Color c = imagenRecuadro.color;
                c.a = Mathf.Clamp(alfa, 0.35f, 1f);
                imagenRecuadro.color = c;
            }
        }
        else
        {
            // Si el enemigo queda a espaldas de la cámara, ocultar temporalmente
            if (imagenRecuadro != null)
            {
                Color c = imagenRecuadro.color;
                c.a = 0f;
                imagenRecuadro.color = c;
            }
        }
    }

    /// <summary>
    /// Enlaza y muestra el recuadro rojo sobre el objetivo fijado.
    /// </summary>
    public void FijarObjetivo(Transform target)
    {
        if (target == null)
        {
            Ocultar();
            return;
        }

        enemigoActual = target;
        visible = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Oculta el recuadro cuando se suelta el disparo o el enemigo muere.
    /// </summary>
    public void Ocultar()
    {
        visible = false;
        enemigoActual = null;
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}