using Unity.Cinemachine;
using UnityEngine;

[ExecuteAlways]
[SaveDuringPlay]
[AddComponentMenu("Cinemachine/Procedural/Extensions/Lock Camera Y")]
public class LockCameraY : CinemachineExtension
{
    [Header("Objetivo (Jugador)")]
    [Tooltip("Arrastra aquí el Transform de tu Jugador")]
    public Transform player;

    [Header("Holgura Vertical (Eje Y)")]
    [Tooltip("Altura fija que se suma a la base de seguimiento.")]
    public float offsetAltura = 3.5f;

    [Tooltip("Cuánto puede subir/bajar la nave libremente antes de que la cámara empiece a acompañarla.")]
    public float margenLibreY = 4f;

    [Tooltip("Suavidad/retraso con el que la cámara acompaña a la nave cuando rebasa el margen.")]
    public float suavidadSeguimientoY = 2f;

    [Header("Ángulo Vertical Fijo (Pitch)")]
    [Tooltip("Inclinación hacia abajo fija para tener perspectiva del suelo.")]
    public float fixedPitchAngle = 8f;

    private float baseYRef;
    private float currentCamY;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (player != null)
        {
            baseYRef = player.position.y;
            currentCamY = baseYRef + offsetAltura;
        }
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // 1. SEGUIMIENTO CON ZONA DE HOLGURA (Eje Y)
        if (stage == CinemachineCore.Stage.Body)
        {
            if (player == null) return;

            if (Application.isPlaying && deltaTime > 0f)
            {
                // Diferencia entre la altura actual de la nave y nuestra referencia
                float deltaY = player.position.y - baseYRef;

                // Si la nave se sale de la zona muerta/margen libre, arrastra la base suavemente
                if (Mathf.Abs(deltaY) > margenLibreY)
                {
                    float exceso = deltaY - (Mathf.Sign(deltaY) * margenLibreY);
                    baseYRef = Mathf.Lerp(baseYRef, baseYRef + exceso, suavidadSeguimientoY * deltaTime);
                }

                // La altura de la cámara sigue la base calculada + el offset
                currentCamY = Mathf.Lerp(currentCamY, baseYRef + offsetAltura, suavidadSeguimientoY * deltaTime);
            }
            else
            {
                // En modo edición en el Scene view
                baseYRef = player.position.y;
                currentCamY = baseYRef + offsetAltura;
            }

            Vector3 pos = state.RawPosition;
            pos.y = currentCamY;
            state.RawPosition = pos;
        }

        // 2. MIRADA HORIZONTAL LIMPIA (Evita el tirón en el centro)
        if (stage == CinemachineCore.Stage.Finalize)
        {
            Vector3 euler = state.RawOrientation.eulerAngles;
            euler.x = fixedPitchAngle; // Fija el ángulo vertical sin pelear con el centro de la pantalla
            state.RawOrientation = Quaternion.Euler(euler);
        }
    }
}