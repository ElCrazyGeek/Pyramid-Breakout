using UnityEngine;

public class MirillaUI : MonoBehaviour
{
    [Header("Controladores del Jugador")]
    [SerializeField] private Player_mundolibre playerLibre;
    [SerializeField] private Player_rieles playerRieles;

    [Header("Rango de Movimiento en Pantalla")]
    [Tooltip("Distancia máxima en píxeles desde el centro de la pantalla")]
    [SerializeField] private float rangoPixelesX = 350f;
    [SerializeField] private float rangoPixelesY = 220f;

    [Header("Comportamiento Star Fox")]
    [Tooltip("Rapidez con la que viaja la mira hacia las esquinas")]
    [SerializeField] private float velocidadApuntado = 18f;
    [Tooltip("Rapidez de regreso al centro al soltar el stick")]
    [SerializeField] private float velocidadRetorno = 12f;

    private RectTransform rectTransform;
    private Vector2 posicionTarget;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // 1. Obtener el input del modo que esté activo
        Vector2 input = Vector2.zero;

        if (playerLibre != null && playerLibre.enabled)
        {
            input = playerLibre.PlayerInput;
        }
        else if (playerRieles != null && playerRieles.enabled)
        {
            input = playerRieles.PlayerInput;
        }

        // 2. Destino dentro del área definida alrededor del centro (0, 0)
        posicionTarget = new Vector2(input.x * rangoPixelesX, input.y * rangoPixelesY);

        // 3. Suavizado asimétrico (viaje rápido, retorno elástico al soltar)
        float vel = (input.sqrMagnitude > 0.01f) ? velocidadApuntado : velocidadRetorno;
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, posicionTarget, vel * Time.deltaTime);
    }
}