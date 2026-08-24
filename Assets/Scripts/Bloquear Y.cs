using Unity.Cinemachine;
using UnityEngine;

[ExecuteAlways]
public class LockCameraY : CinemachineExtension
{
    [Header("Rango de Movimiento Vertical (Mundo)")]
    [Tooltip("La altura central base de la cámara.")]
    public float baseCameraY = 0f;
    [Tooltip("Cuánto puede alejarse la cámara hacia arriba o hacia abajo de la altura base.")]
    public float maxVerticalDeviation = 5f;

    [Header("Rango de Rotación Vertical (Ángulo)")]
    [Tooltip("Ángulo mínimo de inclinación (mirar hacia abajo).")]
    public float minPitchAngle = -10f;
    [Tooltip("Ángulo máximo de inclinación (mirar hacia arriba).")]
    public float maxPitchAngle = 10f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // 1. LIMITAR MOVIMIENTO FÍSICO (EJE Y)
        if (stage == CinemachineCore.Stage.Body)
        {
            Vector3 pos = state.RawPosition;

            // Restringimos la posición Y final calculada dentro del rango permitido
            pos.y = Mathf.Clamp(pos.y, baseCameraY - maxVerticalDeviation, baseCameraY + maxVerticalDeviation);

            state.RawPosition = pos;
        }

        // 2. LIMITAR ROTACIÓN VERTICAL (CABECEO / PITCH)
        if (stage == CinemachineCore.Stage.Finalize)
        {
            Quaternion rot = state.RawOrientation;
            Vector3 euler = rot.eulerAngles;

            // Convertir el ángulo de 0-360 a valores relativos (-180 a 180) para el Clamping
            float pitch = euler.x;
            if (pitch > 180) pitch -= 360;

            // Restringimos la rotación vertical entre los ángulos deseados
            pitch = Mathf.Clamp(pitch, minPitchAngle, maxPitchAngle);

            // Recomponer la rotación de forma segura
            euler.x = pitch;
            state.RawOrientation = Quaternion.Euler(euler);
        }
    }

 }