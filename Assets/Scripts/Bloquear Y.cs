using Unity.Cinemachine;
using UnityEngine;

[ExecuteAlways]
[SaveDuringPlay]
[AddComponentMenu("Cinemachine/Procedural/Extensions/Lock Camera Y")]
public class LockCameraY : CinemachineExtension
{
    [Header("Posición Vertical Fija")]
    [Tooltip("Altura fija mundial donde siempre estará la cámara.")]
    public float fixedCameraY = 10f;

    [Header("Ángulo Vertical Fijo (Pitch)")]
    [Tooltip("Inclinación hacia el suelo fija (ej. 8° a 12° para ver el suelo en perspectiva).")]
    public float fixedPitchAngle = 8f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // 1. BLOQUEO FÍSICO EN Y: La cámara no sube ni baja jamás con la nave
        if (stage == CinemachineCore.Stage.Body)
        {
            Vector3 pos = state.RawPosition;
            pos.y = fixedCameraY;
            state.RawPosition = pos;
        }

        // 2. BLOQUEO DE CABECEO: La cámara mantiene siempre el mismo ángulo mirando al frente
        if (stage == CinemachineCore.Stage.Finalize)
        {
            Vector3 euler = state.RawOrientation.eulerAngles;

            // Fija el pitch sin permitir que Cinemachine incline la mirada al subir la nave
            euler.x = fixedPitchAngle;
            state.RawOrientation = Quaternion.Euler(euler);
        }
    }
}