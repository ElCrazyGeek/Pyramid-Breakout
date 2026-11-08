using UnityEngine;
using TMPro;

public class MedidorFPS : MonoBehaviour
{
    [Header("Referencia UI")]
    [SerializeField] private TextMeshProUGUI textoFps;

    [Header("Configuración")]
    [Tooltip("Cada cuántos segundos se refresca el valor en pantalla")]
    [SerializeField] private float intervaloActualizacion = 0.5f;

    private int conteoFrames = 0;
    private float tiempoAcumulado = 0f;
    private float tiempoRestante = 0f;

    void Awake()
    {
        if (textoFps == null)
            textoFps = GetComponent<TextMeshProUGUI>();

        tiempoRestante = intervaloActualizacion;
    }

    void Update()
    {
        conteoFrames++;
        tiempoAcumulado += Time.unscaledDeltaTime;
        tiempoRestante -= Time.unscaledDeltaTime;

        // Se calcula el promedio acumulado dentro del intervalo
        if (tiempoRestante <= 0f)
        {
            float fps = conteoFrames / tiempoAcumulado;
            float milisegundos = (tiempoAcumulado / conteoFrames) * 1000f;

            if (textoFps != null)
            {
                textoFps.text = string.Format("{0:0.} FPS ({1:0.0} ms)", fps, milisegundos);

                // Feedback visual por color:
                if (fps >= 55f)
                    textoFps.color = Color.green;      // Rendimiento óptimo
                else if (fps >= 30f)
                    textoFps.color = Color.yellow;     // Caída moderada
                else
                    textoFps.color = Color.red;        // Problemas de rendimiento
            }

            conteoFrames = 0;
            tiempoAcumulado = 0f;
            tiempoRestante = intervaloActualizacion;
        }
    }
}