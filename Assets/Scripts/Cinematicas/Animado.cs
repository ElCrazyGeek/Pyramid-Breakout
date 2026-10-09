using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RetratoAnimadoUI : MonoBehaviour
{
    [Header("Respiración / Flotación en Bucle")]
    [SerializeField] private bool animarEnBucle = true;
    [SerializeField] private float velocidadBucle = 2.5f;
    [SerializeField] private float amplitudFlotacionY = 8.0f; // Píxeles que sube y baja

    [Header("Respuesta al Hablar (Énfasis)")]
    [SerializeField] private float escalaHablando = 1.05f;   // Se agranda ligeramente
    [SerializeField] private float escalaReposo = 0.95f;     // Se encoge ligeramente al callar

    private RectTransform rectTransform;
    private Image imagen;
    private Vector2 posicionBase;
    private bool estaHablando = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        imagen = GetComponent<Image>();
        posicionBase = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // 1. Animación continua en bucle (flotación vertical)
        if (animarEnBucle && gameObject.activeSelf)
        {
            float desfaseY = Mathf.Sin(Time.unscaledTime * velocidadBucle) * amplitudFlotacionY;
            rectTransform.anchoredPosition = new Vector2(posicionBase.x, posicionBase.y + desfaseY);
        }

        // 2. Transición suave de escala según si habla o escucha
        float targetScale = estaHablando ? escalaHablando : escalaReposo;
        float escalaActual = Mathf.Lerp(rectTransform.localScale.x, targetScale, Time.unscaledDeltaTime * 10f);
        rectTransform.localScale = new Vector3(escalaActual, escalaActual, 1f);
    }

    // Activar al personaje cuando le toca hablar
    public void PonerEnPrimerPlano(Sprite nuevoSprite)
    {
        estaHablando = true;
        if (nuevoSprite != null) imagen.sprite = nuevoSprite;

        // Color 100% brillante y visible
        imagen.color = Color.white;

        // Pequeño salto hacia arriba al empezar la frase
        rectTransform.anchoredPosition = new Vector2(posicionBase.x, posicionBase.y + 15f);
    }

    // Atenuar al personaje cuando escucha
    public void Atenuar()
    {
        estaHablando = false;

        // Se oscurece al 40% para dar protagonismo al otro
        imagen.color = new Color(0.4f, 0.4f, 0.4f, 1f);
    }
}