using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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

    [Header ("Controles de jugador")]
    public float VelocidadMaxima = 50;
    public float VelocidadMinima = 5;
    public float Aceleracion = 10;
    public float Desaceleracion = 20;

    [Header("Control del freno (milisegundos)")]
    [FormerlySerializedAs("TiempoFrenadoMaximo")]
    [Min(0f)] public float TiempoFrenadoMaximoMs = 4000f;
    [FormerlySerializedAs("TiempoCastigoFrenadp")]
    [Min(0f)] public float TiempoCastigoFrenoMs = 8000f;
    public Vector2 PlayerInput;
    public CharacterController controller;
    private Vector3 origenRiel;

    private bool Frenando = false;
    private bool FrenoBloqueado = false;
    private float InicioFrenado;
    private float FinCastigoFreno;
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
        VelocidadActual = Mathf.Clamp(VelocidadAvance, VelocidadMinima, VelocidadMaxima);
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
        bool frenoActivo = ActualizarEstadoFreno();
        float velocidadObjetivo = frenoActivo ? VelocidadMinima : VelocidadMaxima;

        VelocidadActual = RampaDeAceleracionYDesaceleracion(velocidadObjetivo);

        float avanceZ = VelocidadActual * Time.deltaTime;
        controller.Move(new Vector3(0f, 0f, avanceZ));
    }

    bool ActualizarEstadoFreno()
    {
        float ahora = Time.realtimeSinceStartup;
        float duracionMaxima = TiempoFrenadoMaximoMs / 1000f;
        float duracionCastigo = TiempoCastigoFrenoMs / 1000f;

        if (FrenoBloqueado)
        {
            // Aunque termine el castigo, el jugador debe soltar el botón
            // antes de poder iniciar otro frenado.
            if (ahora >= FinCastigoFreno && !Frenado)
            {
                FrenoBloqueado = false;
            }

            return false;
        }

        if (!Frenado)
        {
            Frenando = false;
            return false;
        }

        if (!Frenando)
        {
            Frenando = true;
            InicioFrenado = ahora;
        }

        if (ahora - InicioFrenado < duracionMaxima)
        {
            return true;
        }

        Frenando = false;
        FrenoBloqueado = true;
        FinCastigoFreno = ahora + duracionCastigo;
        return false;
    }

    void InclinacionJugador()
    {
        float targetPitch = -PlayerInput.y * InclinacionX;
        float targetRoll = -PlayerInput.x * InclinacionZ;

        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, VelocidadInclinacion * Time.deltaTime);
    }

    float RampaDeAceleracionYDesaceleracion(float velocidadObjetivo)
    {
        velocidadObjetivo = Mathf.Clamp(velocidadObjetivo, VelocidadMinima, VelocidadMaxima);

        // Al aumentar la velocidad usa Aceleracion; al reducirla usa Desaceleracion.
        float velocidadDeCambio = velocidadObjetivo > VelocidadActual
            ? Mathf.Max(0f, Aceleracion)
            : Mathf.Max(0f, Desaceleracion);

        return Mathf.MoveTowards(
            VelocidadActual,
            velocidadObjetivo,
            velocidadDeCambio * Time.deltaTime
        );
    }
}
