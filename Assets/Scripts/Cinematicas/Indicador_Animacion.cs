using UnityEngine;

public class IndicadorAnimadoUI : MonoBehaviour
{
    [Header("Animación de Espera (Rebote)")]
    [SerializeField] private float velocidadRebote = 6.0f;
    [SerializeField] private float distanciaRebote = 6.0f; // Píxeles que sube y baja

    private RectTransform rectTransform;
    private Vector2 posicionBase;
    private bool animando = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) posicionBase = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (!animando || rectTransform == null) return;

        // Rebote vertical senoidal continuo
        float offsetY = Mathf.Sin(Time.unscaledTime * velocidadRebote) * distanciaRebote;
        rectTransform.anchoredPosition = new Vector2(posicionBase.x, posicionBase.y + offsetY);
    }

    public void MostrarYAnimar()
    {
        gameObject.SetActive(true);
        animando = true;
    }

    public void Ocultar()
    {
        animando = false;
        if (rectTransform != null) rectTransform.anchoredPosition = posicionBase;
        gameObject.SetActive(false);
    }
}