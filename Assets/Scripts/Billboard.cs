using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private bool useStaticBillboard = false; // Útil para cámaras ortográficas o si prefieres que solo mire en paralelo

    private Camera mainCamera;

    void Start()
    {
        // Cachear la cámara principal al inicio para mayor rendimiento
        mainCamera = Camera.main;

        // Comprobación de seguridad si no hay cámara principal taggeada como "MainCamera"
        if (mainCamera == null)
        {
            Debug.LogError("No se encontró una cámara con el tag 'MainCamera'. El script Billboard no funcionará.");
            enabled = false; // Desactivar el script si no hay cámara
        }
    }

    // Usamos LateUpdate para asegurarnos de que la cámara ya se ha movido en este frame
    void LateUpdate()
    {
        if (mainCamera == null) return;

        if (useStaticBillboard)
        {
            // El objeto siempre mirará exactamente en la misma dirección que la cámara
            // Esto es ideal para sprites en juegos 2D/3D híbridos o cámaras ortográficas
            transform.rotation = mainCamera.transform.rotation;
        }
        else
        {
            // El objeto siempre mirará hacia la posición de la cámara
            // Es la forma más realista para un objeto 3D o un sprite en un mundo 3D
            // El objeto rotará para encarar la cámara, pero mantendrá su orientación vertical (Up)

            // Creamos un vector desde el objeto hacia la cámara
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;

            // Si queremos que solo rote en el eje Y (para que no se incline arriba/abajo):
            // directionToCamera.y = 0; 

            // Si la dirección es válida (no es cero), rotamos el objeto
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }
}