using UnityEngine;

public class Mirilla : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Player_mundolibre player;
    [SerializeField] private Transform naveVisual;

    [Header("Configuración en Pantalla")]
    [SerializeField] private float distanciaFrontalZ = 45f; // Distancia 3D en Z frente a la cámara
    [SerializeField] private float rangoEncuadreX = 0.35f;  // Cuánto viaja del centro (0.5) hacia el borde (0 a 1)
    [SerializeField] private float rangoEncuadreY = 0.25f;
    [SerializeField] private float suavizado = 15f;

    [Header("Rotación")]
    [SerializeField] private float suavizadoRotacion = 10f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (player == null || cam == null) return;

 
        float inputX = player.PlayerInput.x;
        float inputY = player.PlayerInput.y;


        float targetViewportX = 0.5f + (inputX * rangoEncuadreX);
        float targetViewportY = 0.5f + (inputY * rangoEncuadreY);


        Vector3 targetMundial = cam.ViewportToWorldPoint(new Vector3(targetViewportX, targetViewportY, distanciaFrontalZ));


        transform.position = Vector3.Lerp(transform.position, targetMundial, suavizado * Time.deltaTime);


        Quaternion rotacionCamara = cam.transform.rotation;
        if (naveVisual != null)
        {
            float rollNave = naveVisual.localEulerAngles.z;
            rotacionCamara *= Quaternion.Euler(0f, 0f, rollNave);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionCamara, suavizadoRotacion * Time.deltaTime);
    }
}
