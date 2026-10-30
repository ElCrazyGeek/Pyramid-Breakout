using UnityEngine;

public class Mirilla : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Player_mundolibre player;
    [SerializeField] private Camera camaraPrincipal;

    [Header("Rango en Pantalla (0 a 0.5)")]
    [Tooltip("0.40 a 0.45 permite llegar a los bordes sin salirse de la vista")]
    [SerializeField] private float rangoEncuadreX = 0.42f;
    [SerializeField] private float rangoEncuadreY = 0.35f;

    [Header("Comportamiento Star Fox")]
    [Tooltip("Distancia proyectada en Z por delante de la cámara")]
    [SerializeField] private float distanciaFrontalZ = 45f;
    [Tooltip("Qué tan rápido vuela la mirilla al mover la palanca (más rápido que la nave)")]
    [SerializeField] private float velocidadApuntado = 18f;
    [Tooltip("Qué tan rápido regresa la mirilla al centro al soltar el stick")]
    [SerializeField] private float velocidadRetornoCentro = 12f;

    // Coordenadas internas en Viewport (0.5 es el centro de pantalla)
    private Vector2 viewportActual = new Vector2(0.5f, 0.5f);

    void Start()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }
        viewportActual = new Vector2(0.5f, 0.5f);
    }

    void LateUpdate()
    {
        if (player == null || camaraPrincipal == null) return;

        // 1. Obtener el input analógico del jugador (-1 a 1)
        Vector2 input = player.PlayerInput;

        // 2. Calcular el destino en Viewport Space (Centro = 0.5)
        float targetX = 0.5f + (input.x * rangoEncuadreX);
        float targetY = 0.5f + (input.y * rangoEncuadreY);

        // Limitar dentro del marco de la pantalla (deja un 4% de margen de seguridad)
        targetX = Mathf.Clamp(targetX, 0.04f, 0.96f);
        targetY = Mathf.Clamp(targetY, 0.04f, 0.96f);

        Vector2 targetViewport = new Vector2(targetX, targetY);

        // 3. Respuesta asimétrica estilo Star Fox:
        // Si hay input, la mira viaja rápido a las esquinas; si se suelta (input 0), regresa al centro
        float velocidadActual = (input.sqrMagnitude > 0.01f) ? velocidadApuntado : velocidadRetornoCentro;
        viewportActual = Vector2.Lerp(viewportActual, targetViewport, velocidadActual * Time.deltaTime);

        // 4. Proyectar la coordenada de pantalla hacia el mundo 3D
        Vector3 targetMundial = camaraPrincipal.ViewportToWorldPoint(
            new Vector3(viewportActual.x, viewportActual.y, distanciaFrontalZ)
        );

        transform.position = targetMundial;

        // 5. Mirar de frente a la pantalla (Billboard plano)
        transform.rotation = Quaternion.LookRotation(transform.position - camaraPrincipal.transform.position);
    }
}