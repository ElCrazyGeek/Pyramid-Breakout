using UnityEngine;
using UnityEngine.InputSystem;

public class Player_FreeFlight_Controller : MonoBehaviour
{
    [Header("Giro Horizontal (Yaw)")]
    [SerializeField] public float turnSpeed = 15f; 
    [SerializeField] public float bankRoll = 45f;

    [Header("Movimiento Vertical (Subir / Bajar)")]
    [SerializeField] public float verticalSpeed = 12f;
    [SerializeField] public float limiteInferiorY = 25f;
    [SerializeField] public float limiteSuperiorY = 25f;
    [SerializeField] public float bankPitch = 25f; 

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
        float nuevoY = transform.position.y + (PlayerInput.y * verticalSpeed * Time.deltaTime);
        nuevoY = Mathf.Clamp(nuevoY, limiteInferiorY, limiteSuperiorY);

        Vector3 posActual = transform.position;
        posActual.y = nuevoY;
        transform.position = posActual;
    }

    void AplicarInclinacionVisual()
    {
         // Rotación de rumbo (hacia dónde vuela realmente)
    Quaternion headingRotation = Quaternion.Euler(0f, currentYaw, 0f);

    // Inclinaciones estéticas + offset de import del modelo (Y=90, pitch base -90)
    float pitch = (-PlayerInput.y * bankPitch) - 90f;
    float roll = -PlayerInput.x * bankRoll;
    Quaternion localCorrection = Quaternion.Euler(pitch, 90f, roll);

    // Aplica primero el rumbo mundial, luego la corrección local del modelo
    Quaternion targetRotation = headingRotation * localCorrection;

    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothTiltSpeed * Time.deltaTime);
    }
}