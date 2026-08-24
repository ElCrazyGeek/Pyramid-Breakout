using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player_rieles : MonoBehaviour
{
    [Header("Movimiento del jugador")]

    [SerializeField]
    public float speed = 15.0f;
    [SerializeField]
    Vector2 LimitesMovimiento = new Vector2(20f, 10f);

    [SerializeField]
    public float VelocidadAvance = 15f;

    [SerializeField]
    public float VelocidadActual;

    [SerializeField] public bool Frenado = false;

    [Header("Incrinacion")]
    public float InclinacionX = 15f;
    public float InclinacionZ = 15f;

    public float VelocidadInclinacion = 15f;


    public Vector2 PlayerInput;
    public CharacterController controller;
    private Vector3 origenRiel;

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = value.Get<Vector2>();
    }
    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        origenRiel = transform.position; // el riel "central" queda anclado a donde arranca la nave
    }



    void Update()
    {
        mover();
        avance();
        InclinacionJugador();

    }


    void mover()
    {
        // Calcula hacia dónde "querrías" moverte según el input
        Vector3 desplazamiento = (Vector3)PlayerInput * speed * Time.deltaTime;

        // Predice la posición resultante para poder clampearla contra los límites del riel
        Vector3 posFutura = transform.position + desplazamiento;

        float xClamped = Mathf.Clamp(posFutura.x, origenRiel.x - LimitesMovimiento.x, origenRiel.x + LimitesMovimiento.x);
        float yClamped = Mathf.Clamp(posFutura.y, origenRiel.y - LimitesMovimiento.y, origenRiel.y + LimitesMovimiento.y);

        // Solo mueve la diferencia real (ya clampeada), no el desplazamiento crudo
        Vector3 movimientoReal = new Vector3(xClamped - transform.position.x, yClamped - transform.position.y, 0f);
        controller.Move(movimientoReal);
    }

    void OnFreno(InputValue value)
    {
        Frenado = value.isPressed;

    }
    void avance()
    {
        float velocidadActual = Frenado ? VelocidadAvance * 0.2f : VelocidadAvance;
        VelocidadActual = velocidadActual;

        float avanceZ = velocidadActual * Time.deltaTime;
        controller.Move(new Vector3(0f, 0f, avanceZ));
    }


    void InclinacionJugador()
    {
        float targetPitch = -PlayerInput.y * InclinacionX;
        float targetRoll = -PlayerInput.x * InclinacionZ;

        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, VelocidadInclinacion * Time.deltaTime);
    }
}
