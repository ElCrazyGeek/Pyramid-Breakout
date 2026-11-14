using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class Player_rieles : MonoBehaviour
{
    [Header("Movimiento del jugador")]
    [SerializeField] public float velocidadLados = 15.0f;
    [SerializeField] Vector2 LimitesMovimiento = new Vector2(20f, 10f);
    [SerializeField] public float VelocidadAvance = 15f;
    [SerializeField] public float VelocidadActual;
    [SerializeField] public bool Frenado = false;

    [Header("Maniobra de giro")]
    [SerializeField, Range(0f, 90f)] private float inclinacionMaximaManiobra = 80f;
    [SerializeField, Min(0f)] private float velocidadInclinacionManiobra = 180f;
    [SerializeField, Min(1f)] private float multiplicadorVelocidadManiobra = 1.8f;

    [Header("Inclinacion")]
    public float InclinacionX = 15f;
    public float InclinacionZ = 15f;
    public float VelocidadInclinacion = 15f;

    [Header("Controles de jugador")]
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

    private bool inicializado;
    private bool Frenando = false;
    private bool FrenoBloqueado = false;
    private bool girandoDerecha;
    private bool girandoIzquierda;
    private InputAction accionGiroDerecha;
    private InputAction accionGiroIzquierda;
    private float InicioFrenado;
    private float FinCastigoFreno;
    private float rotacionManiobra;
    private float rotacionManiobraAplicada;

    [Header("Alineación con Mirilla")]
    [SerializeField] private MirillaUI mirillaUI; // Arrastra tu MirillaUI aquí
    [SerializeField] private float velocidadOrientacion = 14f;

    // Vector que nos enviará la Mirilla cuando llegue al tope del cuadro
    [HideInInspector] public Vector2 fuerzaArrastreMirilla = Vector2.zero;

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = enabled ? value.Get<Vector2>() : Vector2.zero;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError("Player_rieles requiere un PlayerInput con las acciones de giro configuradas.", this);
            return;
        }

        accionGiroDerecha = playerInput.actions.FindAction("GiroLateralDer", false);
        accionGiroIzquierda = playerInput.actions.FindAction("GiroLateralIzq", false);
        if (accionGiroDerecha == null || accionGiroIzquierda == null)
        {
            Debug.LogError("Faltan las acciones 'GiroLateralDer' o 'GiroLateralIzq' en PlayerInput.", this);
        }
    }

    void Start()
    {
        if (!inicializado) ResetForLevel(transform.position);
    }

    public void ResetForLevel(Vector3 origen)
    {
        origenRiel = origen;
        VelocidadActual = Mathf.Clamp(VelocidadAvance, VelocidadMinima, VelocidadMaxima);
        PlayerInput = Vector2.zero;
        fuerzaArrastreMirilla = Vector2.zero;
        Frenado = Frenando = FrenoBloqueado = false;
        girandoDerecha = girandoIzquierda = false;
        rotacionManiobra = 0f;
        InicioFrenado = FinCastigoFreno = 0;
        inicializado = true;
    }

    void OnDisable()
    {
        PlayerInput = Vector2.zero;
        Frenado = false;
        girandoDerecha = girandoIzquierda = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var obstacle = hit.collider.GetComponentInParent<Pyramid.Levels.ContactDamage>();
        if (obstacle) obstacle.Hit(gameObject);
    }

    void Update()
    {
        girandoDerecha = accionGiroDerecha != null && accionGiroDerecha.enabled && accionGiroDerecha.IsPressed();
        girandoIzquierda = accionGiroIzquierda != null && accionGiroIzquierda.enabled && accionGiroIzquierda.IsPressed();
        ActualizarManiobra();
        mover();
        avance();
        InclinacionJugador();
    }

    public void OnApuntar(InputValue value)
    {
        if (mirillaUI != null)
        {
            mirillaUI.OnApuntar(value);
        }
    }

    void mover()
    {
        // La maniobra acelera el movimiento hacia el lado del giro y en vertical.
        Vector2 movimiento = PlayerInput;
        int direccionManiobra = ObtenerDireccionManiobra();
        if (direccionManiobra != 0)
        {
            if (movimiento.x * direccionManiobra > 0f)
            {
                movimiento.x *= multiplicadorVelocidadManiobra;
            }

            if (Mathf.Abs(movimiento.y) > 0f)
            {
                movimiento.y *= multiplicadorVelocidadManiobra;
            }
        }

        Vector3 desplazamiento = (Vector3)movimiento * velocidadLados * Time.deltaTime;
        Vector3 posFutura = transform.position + desplazamiento;

        float xClamped = Mathf.Clamp(posFutura.x, origenRiel.x - LimitesMovimiento.x, origenRiel.x + LimitesMovimiento.x);
        float yClamped = Mathf.Clamp(posFutura.y, origenRiel.y - LimitesMovimiento.y, origenRiel.y + LimitesMovimiento.y);

        Vector3 movimientoReal = new Vector3(xClamped - transform.position.x, yClamped - transform.position.y, 0f);
        controller.Move(movimientoReal);
    }

    void OnFreno(InputValue value)
    {
        Frenado = enabled && value.isPressed;
    }

    void ActualizarManiobra()
    {
        int direccionManiobra = ObtenerDireccionManiobra();
        float inclinacionObjetivo = -direccionManiobra * inclinacionMaximaManiobra;
        rotacionManiobra = Mathf.MoveTowardsAngle(
            rotacionManiobra,
            inclinacionObjetivo,
            velocidadInclinacionManiobra * Time.deltaTime);
    }

    int ObtenerDireccionManiobra()
    {
        if (girandoDerecha == girandoIzquierda) return 0;
        return girandoDerecha ? 1 : -1;
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
        float ahora = Time.time;
        float duracionMaxima = TiempoFrenadoMaximoMs / 1000f;
        float duracionCastigo = TiempoCastigoFrenoMs / 1000f;

        if (FrenoBloqueado)
        {
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
        if (mirillaUI == null)
        {
            float cambioRoll = Mathf.DeltaAngle(rotacionManiobraAplicada, rotacionManiobra);
            transform.Rotate(0f, 0f, cambioRoll, Space.Self);
            rotacionManiobraAplicada = rotacionManiobra;
            return;
        }

        // 1. Vector que conecta la nave con el punto al que apunta la mirilla
        Vector3 direccionHaciaMirilla = (mirillaUI.PuntoMundialImpacto - transform.position).normalized;

        // 2. La nave rota su frente para alinearse en línea recta con la mira
        Quaternion rotacionAlineada = Quaternion.LookRotation(direccionHaciaMirilla, Vector3.up);

        // 3. Combinamos el balanceo de movimiento con el giro de la maniobra.
        float rollCosmetico = -PlayerInput.x * InclinacionZ;
        Quaternion rotacionFinal = rotacionAlineada * Quaternion.Euler(0f, 0f, rollCosmetico + rotacionManiobra);

        // 4. Suavizamos la rotación completa
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionFinal, velocidadOrientacion * Time.deltaTime);
        rotacionManiobraAplicada = rotacionManiobra;
    }

    float RampaDeAceleracionYDesaceleracion(float velocidadObjetivo)
    {
        velocidadObjetivo = Mathf.Clamp(velocidadObjetivo, VelocidadMinima, VelocidadMaxima);

        float velocidadDeCambio = velocidadObjetivo > VelocidadActual
            ? Mathf.Max(0f, Aceleracion)
            : Mathf.Max(0f, Desaceleracion);

        return Mathf.MoveTowards(VelocidadActual, velocidadObjetivo, velocidadDeCambio * Time.deltaTime);
    }
}