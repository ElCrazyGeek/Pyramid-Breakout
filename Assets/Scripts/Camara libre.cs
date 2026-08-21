using UnityEngine;

public class Camaralibre : MonoBehaviour
{
   [Header("Objetivo")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Player_mundolibre playerController;

    [Header("Distancia y Altura")]
    [SerializeField] private float distanciaDetras = 12f;
    [SerializeField] private float altura = 2f;

    [Header("Influencia Lateral (efecto de 'asomarse' al girar)")]
    [Range(0f, 1f)]
    [SerializeField] private float influenciaLateral = 0.15f;

    [Header("Suavizado (efecto encadenado)")]
    [SerializeField] private float smoothPosicion = 4f;   // más bajo = cadena más floja/lag notorio
    [SerializeField] private float smoothOrientacion = 3f; // qué tan rápido "gira" el rumbo de la cámara

    private Vector3 camaraForward; // el rumbo que la cámara cree que tiene la nave, con retraso

    void Start()
    {
        camaraForward = playerController != null ? playerController.CurrentForward : transform.forward;
    }

    void LateUpdate()
    {
        if (playerTarget == null || playerController == null) return;

        // 1. El "rumbo" de la cámara persigue al rumbo real de la nave con retraso -> efecto de cadena
        camaraForward = Vector3.Slerp(camaraForward, playerController.CurrentForward, smoothOrientacion * Time.deltaTime).normalized;

        // 2. Posición objetivo: detrás de la nave según ESE rumbo retrasado, no según ejes fijos del mundo
        Vector3 detras = playerTarget.position - camaraForward * distanciaDetras + Vector3.up * altura;

        // 3. Un empujón lateral extra usando el right de la nave, para que "se asome" al girar
        Vector3 rightNave = Vector3.Cross(Vector3.up, playerController.CurrentForward).normalized;
        Vector3 targetCameraPos = detras;
        // (opcional: sumar aquí un pequeño offset con rightNave * influenciaLateral si quieres más "peek")

        // 4. Suavizado final de posición
        transform.position = Vector3.Lerp(transform.position, targetCameraPos, smoothPosicion * Time.deltaTime);

        // 5. Sin rotar la cámara bruscamente, pero orientándola hacia donde vuela la nave (con el mismo retraso)
        Quaternion rotObjetivo = Quaternion.LookRotation(camaraForward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, smoothOrientacion * Time.deltaTime);
    }
}
