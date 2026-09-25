using UnityEngine;

[DisallowMultipleComponent]
public class SaludNave : MonoBehaviour
{
    [Header("Salud")]
    [SerializeField, Range(1, 100)] private int saludActual = 100;
    public int SaludActual => saludActual;

    [Header("Regeneración")]
    [Tooltip("Puntos de salud recuperados por segundo, tras 10 segundos sin recibir daño.")]
    [Min(0f)] public float regeneracionPorSegundo = 10f;

    private const float EsperaRegeneracion = 10f;
    private float tiempoSinDanio;
    private float regeneracionAcumulada;

    void Update()
    {
        float tiempoAnterior = tiempoSinDanio;
        tiempoSinDanio += Time.deltaTime;

        if (saludActual >= 100)
        {
            regeneracionAcumulada = 0f;
            return;
        }

        // Cuenta únicamente la porción del frame posterior a los 10 segundos.
        float tiempoRegenerando = Mathf.Max(0f, tiempoSinDanio - EsperaRegeneracion)
            - Mathf.Max(0f, tiempoAnterior - EsperaRegeneracion);
        regeneracionAcumulada += tiempoRegenerando * Mathf.Max(0f, regeneracionPorSegundo);

        int puntos = Mathf.FloorToInt(regeneracionAcumulada);
        saludActual = Mathf.Clamp(saludActual + puntos, 1, 100);
        regeneracionAcumulada = saludActual == 100 ? 0f : regeneracionAcumulada - puntos;
    }

    public void RecibirDanio(int cantidad)
    {
        if (cantidad <= 0) return;

        saludActual = Mathf.Clamp(saludActual - cantidad, 1, 100);
        tiempoSinDanio = 0f;
        regeneracionAcumulada = 0f;
    }

    void OnValidate()
    {
        saludActual = Mathf.Clamp(saludActual, 1, 100);
        regeneracionPorSegundo = Mathf.Max(0f, regeneracionPorSegundo);
    }
}
