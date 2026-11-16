using UnityEngine;

public class PuntoTrayectoriaCamara : MonoBehaviour
{
    [Header("Referencias (Opcional en Prefabs - Se asigna solo)")]
    [SerializeField] private Camera_Rieles camaraRiel;
    [Tooltip("Objeto vacío hijo dentro del mismo Prefab que marca el punto exacto al que irá la cámara.")]
    [SerializeField] private Transform puntoDestino;

    [Header("Rotación en Y")]
    [SerializeField] private bool usarAnguloManualY = true;
    [SerializeField] private float anguloYEnZona = 15f;

    [Header("Transición y Regreso")]
    [SerializeField] private float velocidadTransicion = 4f;
    [SerializeField] private bool autoRegresarAlRebasarZ = true;
    [SerializeField] private bool restaurarAlSalirTrigger = true;

    private void Awake()
    {
        ObtenerCamaraRiel();
    }

    private Camera_Rieles ObtenerCamaraRiel()
    {
        if (camaraRiel == null)
        {
            camaraRiel = Camera_Rieles.Instance != null
                ? Camera_Rieles.Instance
                : FindFirstObjectByType<Camera_Rieles>();
        }
        return camaraRiel;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other)) return;

        Camera_Rieles cam = ObtenerCamaraRiel();
        if (cam == null) return;

        float destinoX = puntoDestino != null ? puntoDestino.position.x : transform.position.x;
        float destinoZ = puntoDestino != null ? puntoDestino.position.z : transform.position.z;
        float rotY = usarAnguloManualY
            ? anguloYEnZona
            : (puntoDestino != null ? puntoDestino.eulerAngles.y : transform.eulerAngles.y);

        cam.IrAPuntoTrayectoria(destinoX, destinoZ, rotY, velocidadTransicion, autoRegresarAlRebasarZ);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!restaurarAlSalirTrigger || !EsJugador(other)) return;

        Camera_Rieles cam = ObtenerCamaraRiel();
        if (cam != null)
        {
            cam.SalirDePuntoTrayectoria();
        }
    }

    private bool EsJugador(Collider other)
    {
        return other.CompareTag("Player") ||
               other.GetComponentInParent<Player_rieles>() != null;
    }
}