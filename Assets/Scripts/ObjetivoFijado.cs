using UnityEngine;
using UnityEngine.UI;

public class LockOnTargetUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camaraPrincipal;

    [Header("Sprites y Colores de Estado")]
    [Tooltip("Sprite mientras escanea/analiza (opcional; si lo dejas vacío usa el mismo)")]
    [SerializeField] private Sprite spriteEscaneo;
    [SerializeField] private Color colorEscaneo = new Color(1f, 0.85f, 0f, 0.85f);

    [Tooltip("Sprite cuando ya fijó al enemigo")]
    [SerializeField] private Sprite spriteFijado;
    [SerializeField] private Color colorFijado = Color.red;

    [Header("Estilo Visual")]
    [Tooltip("Velocidad del parpadeo del recuadro")]
    [SerializeField] private float velocidadParpadeo = 12f;
    [Tooltip("Offset vertical en el mundo para que el recuadro quede centrado en el enemigo")]
    [SerializeField] private float offsetAlturaMundo = 0f;
    [SerializeField] private float escalaEscaneo = 0.9f;
    [SerializeField] private float escalaFijado = 1.2f;

    private RectTransform rectTransform;
    private Image imagenRecuadro;
    private Transform enemigoActual;
    private RectTransform mirillaSeguimiento;

    private bool visible;
    private bool estaFijado;

    private void AsegurarComponentes()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (imagenRecuadro == null) imagenRecuadro = GetComponent<Image>();
    }

    void Awake()
    {
        AsegurarComponentes();
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
        if (!visible) return;
        AsegurarComponentes();

        // MODO 1: Escaneando / Analizando (sigue a la mirilla en pantalla antes de fijar)
        if (!estaFijado)
        {
            if (mirillaSeguimiento != null && rectTransform != null)
            {
                rectTransform.position = mirillaSeguimiento.position;
            }

            if (imagenRecuadro != null)
            {
                float alfa = Mathf.PingPong(Time.time * (velocidadParpadeo * 0.6f), 0.5f) + 0.4f;
                Color c = colorEscaneo;
                c.a = alfa;
                imagenRecuadro.color = c;
            }
            return;
        }

        // MODO 2: Objetivo Fijado en el mundo 3D
        if (enemigoActual == null)
        {
            Ocultar();
            return;
        }

        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (camaraPrincipal == null) return;

        Vector3 puntoEnemigo = enemigoActual.position + (Vector3.up * offsetAlturaMundo);
        Vector3 screenPos = camaraPrincipal.WorldToScreenPoint(puntoEnemigo);

        if (screenPos.z > 0)
        {
            if (rectTransform != null)
                rectTransform.position = new Vector3(screenPos.x, screenPos.y, 0f);

            if (imagenRecuadro != null)
            {
                float alfa = Mathf.PingPong(Time.time * velocidadParpadeo, 1f);
                Color c = colorFijado;
                c.a = Mathf.Clamp(alfa, 0.35f, 1f);
                imagenRecuadro.color = c;
            }
        }
        else
        {
            if (imagenRecuadro != null)
            {
                Color c = imagenRecuadro.color;
                c.a = 0f;
                imagenRecuadro.color = c;
            }
        }
    }

    public void IniciarEscaneo(RectTransform mirilla)
    {
        AsegurarComponentes();

        mirillaSeguimiento = mirilla;
        enemigoActual = null;
        estaFijado = false;
        visible = true;

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * escalaEscaneo;
            if (mirillaSeguimiento != null)
            {
                rectTransform.position = mirillaSeguimiento.position;
            }
        }

        if (imagenRecuadro != null)
        {
            if (spriteEscaneo != null) imagenRecuadro.sprite = spriteEscaneo;
            imagenRecuadro.color = colorEscaneo;
        }

        gameObject.SetActive(true);
    }

    public void FijarObjetivo(Transform target)
    {
        if (target == null)
        {
            Ocultar();
            return;
        }

        AsegurarComponentes();

        enemigoActual = target;
        estaFijado = true;
        visible = true;

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * escalaFijado;
        }

        if (imagenRecuadro != null)
        {
            if (spriteFijado != null) imagenRecuadro.sprite = spriteFijado;
            imagenRecuadro.color = colorFijado;
        }

        gameObject.SetActive(true);
    }

    public void Ocultar()
    {
        visible = false;
        estaFijado = false;
        enemigoActual = null;
        mirillaSeguimiento = null;

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}