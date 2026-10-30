using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class ImpactFeedback : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El modelo 3D o contenedor visual de la nave que se sacudirá")]
    [SerializeField] private Transform modeloVisualNave;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Sacudida de la Nave (Jolt)")]
    [SerializeField] private float duracionSacudida = 0.18f;
    [SerializeField] private float intensidadSacudida = 0.6f;
    [SerializeField] private float retrocesoZ = -0.4f; // Pequeño empujón hacia atrás al recibir impacto

    [Header("Impulso de Cinemachine")]
    [SerializeField] private float fuerzaImpulsoCamara = 1.2f;

    private Vector3 posLocalOriginal;
    private Coroutine rutinaSacudida;

    void Awake()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        if (modeloVisualNave != null)
            posLocalOriginal = modeloVisualNave.localPosition;
    }

    public void EjecutarSacudidaImpacto()
    {
        // 1. Sacudida de pantalla con Cinemachine
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(fuerzaImpulsoCamara);
        }

        // 2. Sacudida visual de la propia nave
        if (modeloVisualNave != null && gameObject.activeInHierarchy)
        {
            if (rutinaSacudida != null) StopCoroutine(rutinaSacudida);
            rutinaSacudida = StartCoroutine(RutinaSacudidaNave());
        }
    }

    private IEnumerator RutinaSacudidaNave()
    {
        float tiempo = 0f;

        while (tiempo < duracionSacudida)
        {
            // Genera vibración aleatoria en los ejes X e Y + retroceso en Z
            float offsetAleatorioX = Random.Range(-intensidadSacudida, intensidadSacudida);
            float offsetAleatorioY = Random.Range(-intensidadSacudida, intensidadSacudida);

            // Va amortiguando la fuerza conforme se agota el tiempo
            float factorAtenuacion = 1f - (tiempo / duracionSacudida);

            Vector3 offsetFinal = new Vector3(
                offsetAleatorioX * factorAtenuacion,
                offsetAleatorioY * factorAtenuacion,
                retrocesoZ * factorAtenuacion
            );

            modeloVisualNave.localPosition = posLocalOriginal + offsetFinal;

            tiempo += Time.deltaTime;
            yield return null;
        }

        // Restaurar posición base exacta
        modeloVisualNave.localPosition = posLocalOriginal;
        rutinaSacudida = null;
    }

    private void OnDisable()
    {
        if (modeloVisualNave != null)
        {
            modeloVisualNave.localPosition = posLocalOriginal;
        }
    }
}