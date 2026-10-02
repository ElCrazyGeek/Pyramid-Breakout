using UnityEngine;

public class CamaraRiel : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform playerTarget;

    [Header("Offset y Posición Central")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -12f); // Altura y distancia detrás del centro del riel

    [Header("Influencia de la Nave en la Cámara")]
    [Range(0f, 0.3f)]
    [SerializeField] private float influenciaX = 0.08f; // Qué tanto se asoma la cámara cuando la nave va a los lados
    [Range(0f, 0.3f)]
    [SerializeField] private float influenciaY = 0.05f; // Qué tanto acompaña cuando sube/baja

    [Header("Suavizado")]
    [SerializeField] private float smoothSpeed = 5f;

    private Vector3 origenRiel;

    public void ResetForLevel(Vector3 origen)
    {
        origenRiel = origen;
        if (playerTarget) transform.position = PosicionObjetivo();
    }

    private Vector3 PosicionObjetivo()
    {
        Vector3 local = playerTarget.position - origenRiel;
        return new Vector3(origenRiel.x + offset.x + local.x * influenciaX,
            origenRiel.y + offset.y + local.y * influenciaY, playerTarget.position.z + offset.z);
    }

    void LateUpdate()
    {
        if (playerTarget == null) return;

        Vector3 targetCameraPos = PosicionObjetivo();

        // 3. Aplica el seguimiento suave sin rotar jamás la cámara
        transform.position = Vector3.Lerp(transform.position, targetCameraPos, smoothSpeed * Time.deltaTime);
    }
}
