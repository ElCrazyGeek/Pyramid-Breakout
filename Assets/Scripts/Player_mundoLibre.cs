using UnityEngine;
using UnityEngine.InputSystem;

public class Player_FreeFlight_Controller : MonoBehaviour
{
    [Header("Giro Horizontal (Yaw)")]
    [SerializeField] public float turnSpeed = 15f; 
    [SerializeField] public float bankRoll = 45f;

    [Header("Movimiento Vertical (Subir / Bajar)")]
    [SerializeField] public float verticalSpeed = 12f;
    [SerializeField] public float limiteInferiorY = 12f;
    [SerializeField] public float limiteSuperiorY = 15f;
    [SerializeField] public float bankPitch = 15f; 

    [Header("Avance")]
    [SerializeField] public float VelocidadAvance = 20f;
    [SerializeField] public bool Frenado = false;
    [SerializeField] public float smoothTiltSpeed = 10f;

   
    public Vector2 PlayerInput;
    private float currentYaw = 0f;

    void Start()
    {
        currentYaw = transform.eulerAngles.y;
    }

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = value.Get<Vector2>();
    }

    public void OnFreno(InputValue value)
    {
        Frenado = value.isPressed;
    }

    void Update()
    {
        RotarYAvance();
        MoverVertical();
        AplicarInclinacionVisual();
    }

    void RotarYAvance()
    {
        // 1. Giro horizontal libre (PlayerInput.x gira el rumbo sobre el eje Y)
        currentYaw += PlayerInput.x * turnSpeed * Time.deltaTime;

        // 2. Avance continuo en la dirección que apunta el rumbo horizontal
        float velocidadActual = Frenado ? (VelocidadAvance * 0.2f) : VelocidadAvance;
        Vector3 forwardHorizontal = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;

        transform.position += forwardHorizontal * velocidadActual * Time.deltaTime;
    }

    void MoverVertical()
    {
        // 3. Subir y bajar en Y con límites fijos
        float nuevoX = transform.position.x + (PlayerInput.y * verticalSpeed * Time.deltaTime);
        nuevoX = Mathf.Clamp(nuevoX, limiteInferiorY, limiteSuperiorY);

        Vector3 posActual = transform.position;
        posActual.x = nuevoX;
        transform.position = posActual;
    }

    void AplicarInclinacionVisual()
    {
        // 4. Inclinaciones estéticas sin desviar la trayectoria
        float pitch = (-PlayerInput.y * bankPitch) -90;
        float roll = -PlayerInput.x * bankRoll;

        // Combina el rumbo actual con las inclinaciones y el offset de tu modelo
        Quaternion targetRotation = Quaternion.Euler(pitch, 90, roll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothTiltSpeed * Time.deltaTime);
    }
}