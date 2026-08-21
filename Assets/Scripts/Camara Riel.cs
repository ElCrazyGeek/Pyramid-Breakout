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

    void LateUpdate()
    {
        if (playerTarget == null) return;

        // 1. La cámara se ancla al centro (offset.x, offset.y) y solo toma una fracción minúscula de la posición de la nave
        float posX = offset.x + (playerTarget.position.x * influenciaX);
        float posY = offset.y + (playerTarget.position.y * influenciaY);

        // 2. Sigue el avance continuo en Z de forma exacta
        float posZ = playerTarget.position.z + offset.z;

        Vector3 targetCameraPos = new Vector3(posX, posY, posZ);

        // 3. Aplica el seguimiento suave sin rotar jamás la cámara
        transform.position = Vector3.Lerp(transform.position, targetCameraPos, smoothSpeed * Time.deltaTime);
    }
}
