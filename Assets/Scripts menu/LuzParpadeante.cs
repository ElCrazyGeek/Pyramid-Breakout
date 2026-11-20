using UnityEngine;
using UnityEngine.UI;

public class LuzParpadeante : MonoBehaviour
{
    [Header("Elemento UI")]
    [Tooltip("Acepta Image, TextMeshProUGUI o cualquier componente Graphic")]
    public Graphic elementoAiluminar;

    [Header("Rango de Opacidad (Alpha)")]
    [Range(0f, 1f)] public float alfaMinimo = 0.5f;
    [Range(0f, 1f)] public float alfaMaximo = 1.0f;

    [Header("Suavizado del Parpadeo")]
    [Tooltip("Qué tan rápido transiciona hacia el nuevo brillo")]
    [Min(0.1f)] public float velocidadLerp = 12f;
    [Tooltip("Cada cuántos segundos elige un nuevo nivel de brillo aleatorio")]
    [Min(0.01f)] public float intervaloCambio = 0.08f;

    private float alfaObjetivo;
    private float temporizador;

    private void Start()
    {
        if (elementoAiluminar == null)
            elementoAiluminar = GetComponent<Graphic>();

        ElegirNuevoObjetivo();
    }

    private void Update()
    {
        if (elementoAiluminar == null) return;

        // 1. Actualizar objetivo aleatorio cada cierto intervalo
        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            ElegirNuevoObjetivo();
            temporizador = intervaloCambio;
        }

        // 2. Interpolar suavemente el alfa actual hacia el objetivo
        Color colorActual = elementoAiluminar.color;
        colorActual.a = Mathf.Lerp(colorActual.a, alfaObjetivo, velocidadLerp * Time.deltaTime);
        elementoAiluminar.color = colorActual;
    }

    private void ElegirNuevoObjetivo()
    {
        float min = Mathf.Min(alfaMinimo, alfaMaximo);
        float max = Mathf.Max(alfaMinimo, alfaMaximo);
        alfaObjetivo = Random.Range(min, max);
    }
}