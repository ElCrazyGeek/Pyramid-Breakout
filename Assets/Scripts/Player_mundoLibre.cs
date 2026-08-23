using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class Player_mundolibre : MonoBehaviour 
{
    [Header("Giro Horizontal (Izquierda o derecha)")]
    [SerializeField] public float turnSpeed = 15f;
    [SerializeField] public float bankRoll = 45f;
    [SerializeField] public float maxYawOffset = 45f;       // cuánto puedes "inclinarte" del rumbo actual
    [SerializeField] public float baseYawFollowSpeed = 1.2f; // qué tan rápido el rumbo real sigue ese offset
    public float baseYaw = 0f;   // rumbo real de vuelo
    public float yawOffset = 0f; // desviación actual (clamped), es tu "banking"
    [Header("Movimiento Vertical (Subir / Bajar)")]
    
    [SerializeField] public float verticalSpeed = 12f;
    [SerializeField] public float limiteInferiorY = 25f;
    [SerializeField] public float limiteSuperiorY = 25f;
    [SerializeField] public float bankPitch = 25f; 

    [Header("Avance")]
    [SerializeField] public float VelocidadAvance = 20f;
    [SerializeField] public bool Frenado = false;
    [SerializeField] public float smoothTiltSpeed = 10f;
    public CharacterController controller;


    public Vector2 PlayerInput;
    private float currentYaw = 0f;
    public Vector3 CurrentForward { get; private set; } = Vector3.forward;


    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        baseYaw = transform.eulerAngles.y;
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
    // 1. El offset responde al input pero nunca pasa de maxYawOffset
    float targetOffset = Mathf.Clamp(PlayerInput.x, -1f, 1f) * maxYawOffset;
    yawOffset = Mathf.MoveTowards(yawOffset, targetOffset, turnSpeed * Time.deltaTime);

    // 2. El rumbo base se deja "guiar" lentamente por ese offset -> giros amplios sin spin instantáneo
    baseYaw += yawOffset * baseYawFollowSpeed * Time.deltaTime;

    currentYaw = baseYaw + yawOffset;

    float velocidadActual = Frenado ? (VelocidadAvance * 0.2f) : VelocidadAvance;
    Vector3 forwardHorizontal = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;
    CurrentForward = forwardHorizontal;

        controller.Move(forwardHorizontal * velocidadActual * Time.deltaTime);
    }

    void MoverVertical()
    {
        float deltaY = PlayerInput.y * verticalSpeed * Time.deltaTime;
        // 3. Subir y bajar en Y con límites fijos
        float nuevoY = Mathf.Clamp(transform.position.y + deltaY, limiteInferiorY, limiteSuperiorY);
        float movimientoReal = nuevoY - transform.position.y;

        controller.Move(Vector3.up * movimientoReal);
    }

        void AplicarInclinacionVisual()
    { 

            // Rotación de rumbo (hacia dónde vuela realmente)
            Quaternion headingRotation = Quaternion.Euler(0f, currentYaw, 0f);
            Quaternion localTilt = Quaternion.Euler(-PlayerInput.y * bankPitch, 0f, -PlayerInput.x * bankRoll);
            // Aplica primero el rumbo mundial, luego la corrección local del modelo
            Quaternion targetRotation = headingRotation * localTilt;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothTiltSpeed * Time.deltaTime);
    }
}