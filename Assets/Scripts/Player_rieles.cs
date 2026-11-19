using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class Player_rieles : MonoBehaviour
{
    [Header("Movimiento del jugador")]
    [SerializeField] public float velocidadLados = 15.0f;
    [SerializeField] Vector2 LimitesMovimiento = new Vector2(20f, 10f);
    [SerializeField] public float VelocidadAvance = 15f;
    [SerializeField] public float VelocidadActual;
    [SerializeField] public bool Frenado = false;
    [SerializeField] public bool AcelerandoBoost = false;

    [Header("Maniobra de Giro (L / R)")]
    [SerializeField, Range(0f, 90f)] private float inclinacionMaximaManiobra = 80f;
    [SerializeField, Min(0f)] private float velocidadInclinacionManiobra = 180f;
    [SerializeField, Min(1f)] private float multiplicadorVelocidadManiobra = 1.8f;

    [Header("Inclinación Normal")]
    public float InclinacionX = 15f;
    public float InclinacionZ = 15f;
    public float VelocidadInclinacion = 15f;

    [Header("Velocidades (Freno / Crucero / Boost)")]
    public float VelocidadBoost = 85f;
    public float VelocidadMaxima = 50f;
    public float VelocidadMinima = 5f;
    public float Aceleracion = 10f;
    public float AceleracionBoost = 35f;
    public float Desaceleracion = 20f;

    [Header("Barra Compartida de Freno e Impulsor")]
    [Min(0.1f)] public float duracionMaxFrenoSeg = 4.0f;
    [Min(0.1f)] public float duracionMaxBoostSeg = 2.5f;
    [Min(0.1f)] public float tiempoRecargaCompletaSeg = 5.0f;
    [SerializeField] private bool exigirBarraLlenaTrasUso = true;

    [Header("UI Barra de Propulsión (Opcional)")]
    [SerializeField] private Image barraPropulsionUI;
    [SerializeField] private Color colorListo = new Color(0.2f, 0.9f, 1f);
    [SerializeField] private Color colorRecargando = new Color(1f, 0.35f, 0.2f);

    [Header("Audio Mixer y Motor")]
    [SerializeField] private bool activarSonidoMotor = true;
    [Tooltip("Arrastra aquí el grupo SFX o Jugador de tu Audio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixerJugador;
    [SerializeField] private AudioClip sfxMotorLoop;
    [SerializeField] private AudioClip sfxBoostActivado;
    [Range(0f, 1f)][SerializeField] private float volumenMotor = 0.5f;
    [SerializeField] private float pitchMinimoFreno = 0.75f;
    [SerializeField] private float pitchNormalCrucero = 1.0f;
    [SerializeField] private float pitchMaximoBoost = 1.45f;
    [SerializeField] private float suavizadoAudioMotor = 6f;

    [Header("Sonido Maniobra de Giro (Solo L / R)")]
    [Tooltip("Suena levemente únicamente al presionar GiroLateralIzq o GiroLateralDer")]
    [SerializeField] private AudioClip sfxManiobraGiro;
    [Range(0f, 1f)][SerializeField] private float volumenManiobra = 0.35f;

    public Vector2 PlayerInput;
    public CharacterController controller;
    private Vector3 origenRiel;

    public float EnergiaPropulsion { get; private set; } = 1f;
    public bool PropulsorBloqueado { get; private set; } = false;

    private bool inicializado;
    private bool usandoFreno = false;
    private bool usandoBoost = false;

    // Acciones y estado de maniobra de giro (L / R)
    private InputAction accionGiroDerecha;
    private InputAction accionGiroIzquierda;
    private bool girandoDerecha = false;
    private bool girandoIzquierda = false;
    private bool maniobraActivaAnterior = false;
    private float rotacionManiobra = 0f;

    [Header("Alineación con Mirilla")]
    [SerializeField] private MirillaUI mirillaUI;
    [SerializeField] private float velocidadOrientacion = 14f;

    [HideInInspector] public Vector2 fuerzaArrastreMirilla = Vector2.zero;

    // Fuentes de audio
    private AudioSource sourceMotor;
    private AudioSource sourceSFX;

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = enabled ? value.Get<Vector2>() : Vector2.zero;
    }

    public void OnFreno(InputValue value)
    {
        Frenado = enabled && value.isPressed;
    }

    public void OnBoost(InputValue value)
    {
        AcelerandoBoost = enabled && value.isPressed;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        ConfigurarAudio();

        // Recuperar tus acciones exactas del Input System para el giro lateral (L / R)
        var playerInputComp = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInputComp != null && playerInputComp.actions != null)
        {
            accionGiroDerecha = playerInputComp.actions.FindAction("GiroLateralDer", false);
            accionGiroIzquierda = playerInputComp.actions.FindAction("GiroLateralIzq", false);
        }
    }

    private void ConfigurarAudio()
    {
        // 1. Fuente para efectos puntuales (Maniobra L/R y Boost)
        sourceSFX = gameObject.AddComponent<AudioSource>();
        sourceSFX.playOnAwake = false;
        sourceSFX.spatialBlend = 0f;
        if (grupoMixerJugador != null)
        {
            sourceSFX.outputAudioMixerGroup = grupoMixerJugador;
        }

        // 2. Fuente continua en bucle para el motor
        if (activarSonidoMotor && sfxMotorLoop != null)
        {
            sourceMotor = gameObject.AddComponent<AudioSource>();
            sourceMotor.clip = sfxMotorLoop;
            sourceMotor.loop = true;
            sourceMotor.playOnAwake = false;
            sourceMotor.volume = volumenMotor;
            sourceMotor.pitch = pitchNormalCrucero;
            sourceMotor.spatialBlend = 0.2f;
            if (grupoMixerJugador != null)
            {
                sourceMotor.outputAudioMixerGroup = grupoMixerJugador;
            }
        }
    }

    void Start()
    {
        if (!inicializado) ResetForLevel(transform.position);
    }

    void OnEnable()
    {
        if (activarSonidoMotor && sourceMotor != null && !sourceMotor.isPlaying)
        {
            sourceMotor.Play();
        }
    }

    void OnDisable()
    {
        PlayerInput = Vector2.zero;
        Frenado = false;
        AcelerandoBoost = false;
        usandoFreno = false;
        usandoBoost = false;
        girandoDerecha = false;
        girandoIzquierda = false;
        maniobraActivaAnterior = false;

        if (sourceMotor != null && sourceMotor.isPlaying)
        {
            sourceMotor.Stop();
        }
    }

    public void ResetForLevel(Vector3 origen)
    {
        origenRiel = origen;
        VelocidadActual = Mathf.Clamp(VelocidadAvance, VelocidadMinima, VelocidadMaxima);
        PlayerInput = Vector2.zero;
        fuerzaArrastreMirilla = Vector2.zero;

        EnergiaPropulsion = 1f;
        PropulsorBloqueado = false;
        Frenado = AcelerandoBoost = false;
        usandoFreno = usandoBoost = false;
        girandoDerecha = girandoIzquierda = false;
        maniobraActivaAnterior = false;
        rotacionManiobra = 0f;
        inicializado = true;

        ActualizarUIPropulsion();

        if (activarSonidoMotor && sourceMotor != null && !sourceMotor.isPlaying && enabled)
        {
            sourceMotor.Play();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var puerta = hit.collider.GetComponentInParent<PuertaContacto>();
        if (puerta != null)
        {
            var saludJugador = GetComponent<Pyramid.Levels.Health>();
            if (saludJugador == null)
                saludJugador = GetComponentInChildren<Pyramid.Levels.Health>();

            puerta.ProcesarContacto(saludJugador);
            return;
        }

        var obstacle = hit.collider.GetComponentInParent<Pyramid.Levels.ContactDamage>();
        if (obstacle) obstacle.Hit(gameObject);
    }

    void Update()
    {
        // Leer directamente las acciones L / R del PlayerInput
        girandoDerecha = accionGiroDerecha != null && accionGiroDerecha.enabled && accionGiroDerecha.IsPressed();
        girandoIzquierda = accionGiroIzquierda != null && accionGiroIzquierda.enabled && accionGiroIzquierda.IsPressed();

        ActualizarManiobra();
        mover();
        avance();
        InclinacionJugador();
        ActualizarSonidoMotor();
        ActualizarUIPropulsion();
    }

    private void ActualizarManiobra()
    {
        int direccionManiobra = ObtenerDireccionManiobra();
        bool maniobraActiva = (direccionManiobra != 0);

        // Suena únicamente en el instante en que presionas L o R, no al moverte normal
        if (maniobraActiva && !maniobraActivaAnterior)
        {
            if (sfxManiobraGiro != null && sourceSFX != null)
            {
                sourceSFX.pitch = Random.Range(0.96f, 1.04f);
                sourceSFX.PlayOneShot(sfxManiobraGiro, volumenManiobra);
            }
        }
        maniobraActivaAnterior = maniobraActiva;

        float inclinacionObjetivo = -direccionManiobra * inclinacionMaximaManiobra;
        rotacionManiobra = Mathf.MoveTowardsAngle(
            rotacionManiobra,
            inclinacionObjetivo,
            velocidadInclinacionManiobra * Time.deltaTime
        );
    }

    private int ObtenerDireccionManiobra()
    {
        if (girandoDerecha == girandoIzquierda) return 0;
        return girandoDerecha ? 1 : -1;
    }

    private void ActualizarEstadoPropulsion()
    {
        float dt = Time.deltaTime;

        if (PropulsorBloqueado)
        {
            usandoFreno = false;
            usandoBoost = false;

            EnergiaPropulsion += (1f / tiempoRecargaCompletaSeg) * dt;
            if (EnergiaPropulsion >= 1f)
            {
                EnergiaPropulsion = 1f;
                if (!Frenado && !AcelerandoBoost)
                {
                    PropulsorBloqueado = false;
                }
            }
            return;
        }

        if (AcelerandoBoost && EnergiaPropulsion > 0f)
        {
            if (!usandoBoost && sourceSFX != null && sfxBoostActivado != null)
            {
                sourceSFX.pitch = 1f;
                sourceSFX.PlayOneShot(sfxBoostActivado, volumenMotor);
            }

            usandoBoost = true;
            usandoFreno = false;
            EnergiaPropulsion -= (1f / duracionMaxBoostSeg) * dt;

            if (EnergiaPropulsion <= 0f)
            {
                EnergiaPropulsion = 0f;
                usandoBoost = false;
                PropulsorBloqueado = true;
            }
            return;
        }

        if (Frenado && EnergiaPropulsion > 0f)
        {
            usandoFreno = true;
            usandoBoost = false;
            EnergiaPropulsion -= (1f / duracionMaxFrenoSeg) * dt;

            if (EnergiaPropulsion <= 0f)
            {
                EnergiaPropulsion = 0f;
                usandoFreno = false;
                PropulsorBloqueado = true;
            }
            return;
        }

        if ((usandoBoost || usandoFreno) && exigirBarraLlenaTrasUso && EnergiaPropulsion < 1f)
        {
            PropulsorBloqueado = true;
        }

        usandoBoost = false;
        usandoFreno = false;

        if (EnergiaPropulsion < 1f)
        {
            EnergiaPropulsion = Mathf.MoveTowards(EnergiaPropulsion, 1f, (1f / tiempoRecargaCompletaSeg) * dt);
        }
    }

    void avance()
    {
        ActualizarEstadoPropulsion();

        float velocidadObjetivo = VelocidadMaxima;
        if (usandoBoost) velocidadObjetivo = VelocidadBoost;
        else if (usandoFreno) velocidadObjetivo = VelocidadMinima;

        VelocidadActual = RampaDeAceleracionYDesaceleracion(velocidadObjetivo);
        float avanceZ = VelocidadActual * Time.deltaTime;

        if (controller != null && controller.enabled)
            controller.Move(new Vector3(0f, 0f, avanceZ));
    }

    private void ActualizarUIPropulsion()
    {
        if (barraPropulsionUI == null) return;

        barraPropulsionUI.fillAmount = EnergiaPropulsion;
        barraPropulsionUI.color = PropulsorBloqueado ? colorRecargando : colorListo;
    }

    private void ActualizarSonidoMotor()
    {
        if (!activarSonidoMotor || sourceMotor == null) return;

        if (Time.timeScale <= 0f)
        {
            if (sourceMotor.isPlaying) sourceMotor.Pause();
            return;
        }
        else if (!sourceMotor.isPlaying)
        {
            sourceMotor.UnPause();
        }

        float pitchObjetivo = pitchNormalCrucero;
        if (usandoBoost)
        {
            pitchObjetivo = pitchMaximoBoost;
        }
        else if (usandoFreno)
        {
            pitchObjetivo = pitchMinimoFreno;
        }

        float extraManiobra = PlayerInput.magnitude * 0.08f;
        sourceMotor.pitch = Mathf.Lerp(sourceMotor.pitch, pitchObjetivo + extraManiobra, suavizadoAudioMotor * Time.deltaTime);
        sourceMotor.volume = volumenMotor;
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
        // Restaurado: la maniobra L / R acelera el desplazamiento hacia ese lado y en vertical
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
        if (controller != null && controller.enabled)
            controller.Move(movimientoReal);
    }

    void InclinacionJugador()
    {
        if (mirillaUI == null)
        {
            float rollDirecto = (-PlayerInput.x * InclinacionZ) + rotacionManiobra;
            Quaternion rotSinMirilla = Quaternion.Euler(-PlayerInput.y * InclinacionX, 0f, rollDirecto);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotSinMirilla, velocidadOrientacion * Time.deltaTime);
            return;
        }

        Vector3 vectorObjetivo = mirillaUI.PuntoMundialImpacto - transform.position;
        if (vectorObjetivo.z < 15f)
        {
            vectorObjetivo = new Vector3(vectorObjetivo.x, vectorObjetivo.y, 15f);
        }

        Vector3 direccionHaciaMirilla = vectorObjetivo.normalized;
        float maxAnguloRadianes = 40f * Mathf.Deg2Rad;
        Vector3 direccionSegura = Vector3.RotateTowards(Vector3.forward, direccionHaciaMirilla, maxAnguloRadianes, 0f);

        Quaternion rotacionAlineada = Quaternion.LookRotation(direccionSegura, Vector3.up);

        float rollCosmetico = -PlayerInput.x * InclinacionZ;
        Quaternion rotacionFinal = rotacionAlineada * Quaternion.Euler(0f, 0f, rollCosmetico + rotacionManiobra);

        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionFinal, velocidadOrientacion * Time.deltaTime);
    }

    float RampaDeAceleracionYDesaceleracion(float velocidadObjetivo)
    {
        velocidadObjetivo = Mathf.Clamp(velocidadObjetivo, VelocidadMinima, VelocidadBoost);

        float velocidadDeCambio;
        if (usandoBoost)
            velocidadDeCambio = Mathf.Max(0f, AceleracionBoost);
        else if (velocidadObjetivo > VelocidadActual)
            velocidadDeCambio = Mathf.Max(0f, Aceleracion);
        else
            velocidadDeCambio = Mathf.Max(0f, Desaceleracion);

        return Mathf.MoveTowards(VelocidadActual, velocidadObjetivo, velocidadDeCambio * Time.deltaTime);
    }
}