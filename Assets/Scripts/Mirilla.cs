using UnityEngine;

public class Mirilla : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Player_mundolibre player;
    [SerializeField] private Camera camaraPrincipal;

    [Header("Alcance en Pantalla ")]
    [SerializeField] private float rangoEncuadreX = 0.45f;
    [SerializeField] private float rangoEncuadreY = 0.45f;
    [SerializeField] private float distanciaFrontalZ = 40f;
    [SerializeField] private float suavizado = 20f;

    void Start()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (player == null || camaraPrincipal == null) return;

     
        float inputX = player.PlayerInput.x;
        float inputY = player.PlayerInput.y;


        float targetViewportX = Mathf.Clamp(0.5f + (inputX * rangoEncuadreX), 0.03f, 0.97f);
        float targetViewportY = Mathf.Clamp(0.5f + (inputY * rangoEncuadreY), 0.03f, 0.97f);


        Vector3 targetMundial = camaraPrincipal.ViewportToWorldPoint(
            new Vector3(targetViewportX, targetViewportY, distanciaFrontalZ)
        );

        transform.position = Vector3.Lerp(transform.position, targetMundial, suavizado * Time.deltaTime);


        transform.rotation = Quaternion.LookRotation(transform.position - camaraPrincipal.transform.position);
    }
}
