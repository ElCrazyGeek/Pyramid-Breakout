using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.Cinemachine;


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
    [Tooltip("Segundos de espera antes de empezar a recargar tras soltar o vaciar la barra")]
    [Min(0f)] public float retardoInicioRecargaSeg = 0.4f;
    [Tooltip("Si está activo, tras vaciar por completo la barra exige llegar al 100% para volver a usarla")]
    [SerializeField] private bool exigirBarraLlenaTrasAgotar = true;

    [Header("UI Barra de Propulsión (Visibilidad Dinámica)")]
    [SerializeField] private Image barraPropulsionUI;
    [Tooltip("CanvasGroup del contenedor padre de la barra (fondo + relleno) para hacer fade al aparecer/desaparecer")]
    [SerializeField] private CanvasGroup contenedorBarraUI;
    [SerializeField] private float velocidadFadeBarra = 6f;
    [SerializeField] private float retardoOcultarBarraLlena = 0.6f;
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
    private float temporizadorRetardoRecarga = 0f;
    private float temporizadorOcultarUI = 0f;

    // Acciones directas del Input System
    private InputAction accionGiroDerecha;
    private InputAction accionGiroIzquierda;
    private InputAction accionFreno;
    private InputAction accionAcelerar;

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

    [Header("Feedback de Cámara (Boost / Freno)")]
    [SerializeField] private CinemachineCamera camaraRielCine;
    [SerializeField] private CinemachineFollow followRielCine;
    [Tooltip("Cuánto se aleja hacia atrás en Z la cámara al hacer Boost")]
    [SerializeField] private float retrocesoZBoost = 8f;
    [Tooltip("Cuánto se acerca hacia adelante en Z la cámara al frenar")]
    [SerializeField] private float acercamientoZFreno = 6f;
    [SerializeField] private float fovNormal = 60f;
    [SerializeField] private float fovBoost = 74f;
    [SerializeField] private float fovFreno = 53f;
    [SerializeField] private float suavizadoCamaraVelocidad = 6f;

    [Header("Feedback Partículas de Motores")]
    [Tooltip("Arrastra aquí los Particle Systems de las turbinas (Motor_Glow / estelas)")]
    [SerializeField] private ParticleSystem[] particulasMotores;
    [SerializeField] private float escalaParticulaNormal = 1f;
    [SerializeField] private float escalaParticulaBoost = 1.85f;
    [SerializeField] private float escalaParticulaFreno = 0.25f;
    [SerializeField] private float suavizadoParticulas = 8f;

    private float offsetZBase;
    private float factorParticulaActual = 1f;

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = enabled ? value.Get<Vector2>() : Vector2.zero;
    }

    public void OnFreno(InputValue value)
    {
        Frenado = enabled && value.isPressed;
    }

    public void OnAcelerar(InputValue value)
    {
        AcelerandoBoost = enabled && value.isPressed;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        ConfigurarAudio();

        var playerInputComp = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInputComp != null && playerInputComp.actions != null)
        {
            accionGiroDerecha = playerInputComp.actions.FindAction("GiroLateralDer", false);
            accionGiroIzquierda = playerInputComp.actions.FindAction("GiroLateralIzq", false);
            accionFreno = playerInputComp.actions.FindAction("Freno", false);
            accionAcelerar = playerInputComp.actions.FindAction("Acelerar", false);
            if (accionAcelerar == null)
            {
                accionAcelerar = playerInputComp.actions.FindAction("Boost", false);
            }
        }

        if (camaraRielCine != null && followRielCine == null)
        {
            followRielCine = camaraRielCine.GetComponent<CinemachineFollow>();
        }

        if (followRielCine != null)
        {
            offsetZBase = followRielCine.FollowOffset.z;
        }
    }

    private void ConfigurarAudio()
    {
        sourceSFX = gameObject.AddComponent<AudioSource>();
        sourceSFX.playOnAwake = false;
        sourceSFX.spatialBlend = 0f;
        if (grupoMixerJugador != null)
        {
            sourceSFX.outputAudioMixerGroup = grupoMixerJugador;
        }

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

        if (contenedorBarraUI != null)
        {
            contenedorBarraUI.alpha = 0f;
            contenedorBarraUI.gameObject.SetActive(false);
        }
        else if (barraPropulsionUI != null)
        {
            barraPropulsionUI.gameObject.SetActive(false);
        }

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
        temporizadorRetardoRecarga = 0f;
        temporizadorOcultarUI = 0f;
        girandoDerecha = girandoIzquierda = false;
        maniobraActivaAnterior = false;
        rotacionManiobra = 0f;
        inicializado = true;

        if (contenedorBarraUI != null)
        {
            contenedorBarraUI.alpha = 0f;
            contenedorBarraUI.gameObject.SetActive(false);
        }
        else if (barraPropulsionUI != null)
        {
            barraPropulsionUI.gameObject.SetActive(false);
        }

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
        // Lectura directa en tiempo real para evitar que botones se queden pegados
        girandoDerecha = accionGiroDerecha != null && accionGiroDerecha.enabled && accionGiroDerecha.IsPressed();
        girandoIzquierda = accionGiroIzquierda != null && accionGiroIzquierda.enabled && accionGiroIzquierda.IsPressed();

        if (accionFreno != null && accionFreno.enabled)
            Frenado = accionFreno.IsPressed();

        if (accionAcelerar != null && accionAcelerar.enabled)
            AcelerandoBoost = accionAcelerar.IsPressed();

        ActualizarManiobra();
        mover();
        avance();
        InclinacionJugador();
        ActualizarSonidoMotor();
        ActualizarUIPropulsion();
        ActualizarEfectosVelocidad();
    }

    private void ActualizarManiobra()
    {
        int direccionManiobra = ObtenerDireccionManiobra();
        bool maniobraActiva = (direccionManiobra != 0);

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

        // 1. Si la barra se agotó por completo y está en recarga obligatoria
        if (PropulsorBloqueado)
        {
            usandoFreno = false;
            usandoBoost = false;

            if (temporizadorRetardoRecarga > 0f)
            {
                temporizadorRetardoRecarga -= dt;
                return;
            }

            EnergiaPropulsion = Mathf.MoveTowards(EnergiaPropulsion, 1f, (1f / tiempoRecargaCompletaSeg) * dt);
            if (EnergiaPropulsion >= 1f)
            {
                EnergiaPropulsion = 1f;
                // Se desbloquea en cuanto llega al 100%
                PropulsorBloqueado = false;
            }
            return;
        }

        // 2. Uso activo de Boost (vaciando)
        if (AcelerandoBoost && EnergiaPropulsion > 0f)
        {
            if (!usandoBoost && sourceSFX != null && sfxBoostActivado != null)
            {
                sourceSFX.pitch = 1f;
                sourceSFX.PlayOneShot(sfxBoostActivado, volumenMotor);
            }

            usandoBoost = true;
            usandoFreno = false;
            temporizadorRetardoRecarga = retardoInicioRecargaSeg;
            EnergiaPropulsion -= (1f / duracionMaxBoostSeg) * dt;

            if (EnergiaPropulsion <= 0f)
            {
                EnergiaPropulsion = 0f;
                usandoBoost = false;
                if (exigirBarraLlenaTrasAgotar) PropulsorBloqueado = true;
            }
            return;
        }

        // 3. Uso activo de Freno (vaciando)
        if (Frenado && EnergiaPropulsion > 0f)
        {
            usandoFreno = true;
            usandoBoost = false;
            temporizadorRetardoRecarga = retardoInicioRecargaSeg;
            EnergiaPropulsion -= (1f / duracionMaxFrenoSeg) * dt;

            if (EnergiaPropulsion <= 0f)
            {
                EnergiaPropulsion = 0f;
                usandoFreno = false;
                if (exigirBarraLlenaTrasAgotar) PropulsorBloqueado = true;
            }
            return;
        }

        // 4. Ningún botón presionado: recuperar energía tras un breve retardo
        usandoBoost = false;
        usandoFreno = false;

        if (EnergiaPropulsion < 1f)
        {
            if (temporizadorRetardoRecarga > 0f)
            {
                temporizadorRetardoRecarga -= dt;
                return;
            }

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

        // Determinar si debe estar visible: se está usando (vaciando) O no está llena (llenando)
        bool enUsoOIncompleta = usandoBoost || usandoFreno || EnergiaPropulsion < 0.999f;

        if (enUsoOIncompleta)
        {
            temporizadorOcultarUI = retardoOcultarBarraLlena;
        }
        else if (temporizadorOcultarUI > 0f)
        {
            temporizadorOcultarUI -= Time.deltaTime;
        }

        bool debeMostrarse = enUsoOIncompleta || temporizadorOcultarUI > 0f;

        // Si asignaste un CanvasGroup en el padre de la barra, hacemos fade suave
        if (contenedorBarraUI != null)
        {
            if (debeMostrarse && !contenedorBarraUI.gameObject.activeSelf)
            {
                contenedorBarraUI.gameObject.SetActive(true);
            }

            float alphaObjetivo = debeMostrarse ? 1f : 0f;
            contenedorBarraUI.alpha = Mathf.MoveTowards(
                contenedorBarraUI.alpha,
                alphaObjetivo,
                velocidadFadeBarra * Time.deltaTime
            );

            if (!debeMostrarse && contenedorBarraUI.alpha <= 0.01f && contenedorBarraUI.gameObject.activeSelf)
            {
                contenedorBarraUI.alpha = 0f;
                contenedorBarraUI.gameObject.SetActive(false);
            }
        }
        else
        {
            // Fallback directo si no se usó CanvasGroup
            if (barraPropulsionUI.gameObject.activeSelf != debeMostrarse)
            {
                barraPropulsionUI.gameObject.SetActive(debeMostrarse);
            }
        }
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

    private void ActualizarEfectosVelocidad()
    {
        float dt = Time.deltaTime;

        // 1. Calcular factor de velocidad normalizado: -1 (Freno), 0 (Crucero), +1 (Boost)
        float factorVelocidad = 0f;
        if (VelocidadActual > VelocidadMaxima)
        {
            factorVelocidad = Mathf.InverseLerp(VelocidadMaxima, VelocidadBoost, VelocidadActual);
        }
        else if (VelocidadActual < VelocidadMaxima)
        {
            factorVelocidad = -Mathf.InverseLerp(VelocidadMaxima, VelocidadMinima, VelocidadActual);
        }

        // 2. Ajustar distancia Z y FOV de la cámara Cinemachine
        if (followRielCine != null)
        {
            float desplazamientoZ = factorVelocidad >= 0f
                ? -retrocesoZBoost * factorVelocidad       // Más negativo = cámara más atrás
                : acercamientoZFreno * (-factorVelocidad); // Menos negativo = cámara más cerca ("encogida")

            Vector3 offsetActual = followRielCine.FollowOffset;
            float targetZ = offsetZBase + desplazamientoZ;
            offsetActual.z = Mathf.Lerp(offsetActual.z, targetZ, suavizadoCamaraVelocidad * dt);
            followRielCine.FollowOffset = offsetActual;
        }

        if (camaraRielCine != null)
        {
            float targetFov = fovNormal;
            if (factorVelocidad > 0f)
                targetFov = Mathf.Lerp(fovNormal, fovBoost, factorVelocidad);
            else if (factorVelocidad < 0f)
                targetFov = Mathf.Lerp(fovNormal, fovFreno, -factorVelocidad);

            camaraRielCine.Lens.FieldOfView = Mathf.Lerp(
                camaraRielCine.Lens.FieldOfView,
                targetFov,
                suavizadoCamaraVelocidad * dt
            );
        }

        // 3. Escalar y modular las partículas de los propulsores
        if (particulasMotores != null && particulasMotores.Length > 0)
        {
            float targetEscala = escalaParticulaNormal;
            if (factorVelocidad > 0f)
                targetEscala = Mathf.Lerp(escalaParticulaNormal, escalaParticulaBoost, factorVelocidad);
            else if (factorVelocidad < 0f)
                targetEscala = Mathf.Lerp(escalaParticulaNormal, escalaParticulaFreno, -factorVelocidad);

            factorParticulaActual = Mathf.Lerp(factorParticulaActual, targetEscala, suavizadoParticulas * dt);

            foreach (var ps in particulasMotores)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.startSizeMultiplier = factorParticulaActual;
                main.startSpeedMultiplier = factorParticulaActual;
            }
        }
    }
}

