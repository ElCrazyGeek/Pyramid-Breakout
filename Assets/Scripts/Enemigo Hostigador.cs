using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class EnemigoSeguidor : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetivo;
    public Transform boca;
    public Transform visual; // si la malla está en un hijo

    [Header("Animación de Entrada")]
    public bool usarAnimacionEntrada = true;
    [Tooltip("Distancia extra desde donde arranca la entrada en (X lateral, Y vertical, Z profundidad).")]
    public Vector3 magnitudEntrada = new Vector3(18f, 10f, 20f);
    public float duracionEntrada = 1.2f;
    [Tooltip("Inclinación lateral (roll) que hace la nave al entrar en picada o curva.")]
    public float inclinacionEntrada = 45f;

    [Header("Movimiento")]
    [Tooltip("Distancia delante del jugador a la que se posiciona el enemigo.")]
    public float distanciaSeguimiento = 20f;
    [Tooltip("Si es true, usa la X y Y exacta con la que apareció en el .level como su puesto en formación.")]
    public bool usarPosicionSpawnComoFormacion = true;
    [Tooltip("Puesto manual en pantalla (solo se usa si 'Usar Posicion Spawn Como Formacion' está desmarcado).")]
    public Vector2 offsetCombateXY = new Vector2(0f, 1.5f);
    public float velocidadRespuesta = 5f;

    [Header("Apuntado")]
    public float velocidadRotacion = 6f;
    public float cadenciaDisparo = 1.5f;
    public GameObject prefabProyectil;
    public Vector3 proyectilRotationOffsetEuler = Vector3.zero;

    [Header("Ciclo de vida")]
    public float tiempoVida = 12f;
    public float velocidadRetirada = 8f;
    public float tiempoRetirada = 2f;

    [Header("Audio y Mezcla (Audio Mixer)")]
    [Tooltip("Arrastra aquí el grupo SFX o Enemigos de tu Audio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixer;

    [Header("Sonido de Motor")]
    [SerializeField] private bool activarSonidoMotor = true;
    [SerializeField] private AudioClip sfxMotorLoop;
    [Range(0f, 1f)][SerializeField] private float volumenMotor = 0.45f;
    [SerializeField] private float pitchMotorCrucero = 1.0f;
    [SerializeField] private float pitchMotorManiobra = 1.3f;

    [Header("Sonido Maniobra de Giro / Entrada")]
    [Tooltip("Sonido de propulsor o silbido al hacer la entrada en curva o al retirarse")]
    [SerializeField] private AudioClip sfxManiobraGiro;
    [Range(0f, 1f)][SerializeField] private float volumenManiobra = 0.7f;

    [Header("Debug")]
    public bool debugVisuals = false;
    public bool debugUseLineRenderer = true;
    public float debugLength = 100f;

    // estado
    Vector3 posicionInicial;
    float timerVida;
    float temporizadorDisparo;
    bool estaEntrando;
    bool estaRetirandose;

    // debug
    LineRenderer lrDeseada, lrReal;
    bool visualAutoAdjusted;

    private Vector2 puestoFormacionActual;
    private Vector3 direccionEntradaCalculada;

    // Fuentes de audio
    private AudioSource sourceMotor;
    private AudioSource sourceSFX;

    void Awake()
    {
        ConfigurarAudio();
    }

    private void ConfigurarAudio()
    {
        // 1. AudioSource para efectos puntuales (maniobra de giro / retirada)
        sourceSFX = gameObject.AddComponent<AudioSource>();
        sourceSFX.playOnAwake = false;
        sourceSFX.spatialBlend = 0.85f; // Audio 3D espacial
        sourceSFX.minDistance = 5f;
        sourceSFX.maxDistance = 80f;
        if (grupoMixer != null) sourceSFX.outputAudioMixerGroup = grupoMixer;

        // 2. AudioSource en bucle para el motor continuo
        if (activarSonidoMotor && sfxMotorLoop != null)
        {
            sourceMotor = gameObject.AddComponent<AudioSource>();
            sourceMotor.clip = sfxMotorLoop;
            sourceMotor.loop = true;
            sourceMotor.playOnAwake = false;
            sourceMotor.volume = volumenMotor;
            sourceMotor.pitch = pitchMotorCrucero;
            sourceMotor.spatialBlend = 0.85f;
            sourceMotor.minDistance = 5f;
            sourceMotor.maxDistance = 80f;
            if (grupoMixer != null) sourceMotor.outputAudioMixerGroup = grupoMixer;
        }
    }

    void Start()
    {
        posicionInicial = transform.position;
        timerVida = tiempoVida;

        if (activarSonidoMotor && sourceMotor != null && !sourceMotor.isPlaying)
        {
            sourceMotor.Play();
        }

        // 1. Asignar objetivo si no se definió en el Inspector
        if (objetivo == null)
        {
            if (Pyramid.Levels.LevelSession.Active != null && Pyramid.Levels.LevelSession.Active.player != null)
            {
                Transform playerRoot = Pyramid.Levels.LevelSession.Active.player;
                Transform centro = playerRoot.Find("CentroDisparo");
                objetivo = (centro != null) ? centro : playerRoot;
            }
            else
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null)
                {
                    Transform playerRoot = go.transform.root;
                    Transform centro = playerRoot.Find("CentroDisparo");
                    objetivo = (centro != null) ? centro : playerRoot;
                }
            }
        }

        // 2. Conservar la posición exacta con la que nació en el .level respecto al jugador
        if (objetivo != null)
        {
            if (usarPosicionSpawnComoFormacion)
            {
                Vector3 deltaSpawn = transform.position - objetivo.position;
                float xRelativo = Vector3.Dot(deltaSpawn, objetivo.right);
                float yRelativo = deltaSpawn.y;

                puestoFormacionActual = new Vector2(xRelativo, yRelativo);
            }
            else
            {
                puestoFormacionActual = offsetCombateXY;
            }

            float signoX = (puestoFormacionActual.x >= 0f) ? 1f : -1f;
            float signoY = (puestoFormacionActual.y >= 0f) ? 1f : -1f;
            direccionEntradaCalculada = new Vector3(
                magnitudEntrada.x * signoX,
                magnitudEntrada.y * signoY,
                magnitudEntrada.z
            );
        }

        // 3. Auto-ajuste si la boca apunta en dirección opuesta al objetivo
        if (!visualAutoAdjusted && objetivo != null)
        {
            Vector3 origen = (boca != null) ? boca.position : transform.position;
            Vector3 desired = (objetivo.position - origen);
            if (desired.sqrMagnitude > 0.0001f)
            {
                desired.Normalize();
                Vector3 real = (boca != null) ? boca.forward.normalized : transform.forward.normalized;
                if (Vector3.Dot(desired, real) < 0f)
                {
                    if (visual != null) visual.Rotate(0f, 180f, 0f, Space.Self);
                    else transform.Rotate(0f, 180f, 0f, Space.Self);
                    visualAutoAdjusted = true;
                    if (debugVisuals) Debug.Log($"Auto-ajuste aplicado al {(visual != null ? "visual" : "root")}");
                }
            }
        }

        // 4. Ejecutar animación de entrada si está activada
        if (usarAnimacionEntrada && objetivo != null)
        {
            StartCoroutine(EntradaCoroutine());
        }
    }

    void Update()
    {
        ActualizarAudioMotor();

        // Si está entrando o retirándose, cedemos el control total a sus respectivas corrutinas
        if (estaEntrando || estaRetirandose || objetivo == null) return;

        MovimientoFijo();
        GestionarCombate();
        UpdateDebugVisuals();
        GestionarTiempoVida();
    }

    private void ActualizarAudioMotor()
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

        // Sube el tono del motor cuando hace la curva de entrada o la maniobra de retirada
        float pitchObjetivo = (estaEntrando || estaRetirandose) ? pitchMotorManiobra : pitchMotorCrucero;
        sourceMotor.pitch = Mathf.Lerp(sourceMotor.pitch, pitchObjetivo, Time.deltaTime * 6f);
        sourceMotor.volume = volumenMotor;
    }

    private void ReproducirSonidoManiobra()
    {
        if (sfxManiobraGiro != null && sourceSFX != null)
        {
            sourceSFX.pitch = Random.Range(0.95f, 1.05f);
            sourceSFX.PlayOneShot(sfxManiobraGiro, volumenManiobra);
        }
    }

    IEnumerator EntradaCoroutine()
    {
        estaEntrando = true;
        ReproducirSonidoManiobra();

        Transform modelo = (visual != null) ? visual : transform;
        Quaternion rotacionBaseVisual = modelo.localRotation;
        Transform ancla = objetivo;

        Vector3 offsetMundial = ancla.right * direccionEntradaCalculada.x +
                                Vector3.up * direccionEntradaCalculada.y +
                                ancla.forward * direccionEntradaCalculada.z;

        transform.position = CalcularPosicionCombate(ancla) + offsetMundial;

        float tiempo = 0f;
        float signoInclinacion = direccionEntradaCalculada.x >= 0 ? 1f : -1f;

        while (tiempo < duracionEntrada)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionEntrada);
            float curvaSuave = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 destinoActual = CalcularPosicionCombate(ancla);
            Vector3 inicioActual = destinoActual + offsetMundial;

            transform.position = Vector3.Lerp(inicioActual, destinoActual, curvaSuave);
            RotarHaciaObjetivo();

            float anguloRoll = Mathf.Lerp(inclinacionEntrada * signoInclinacion, 0f, curvaSuave);
            if (visual != null)
            {
                visual.localRotation = rotacionBaseVisual * Quaternion.Euler(0f, 0f, anguloRoll);
            }

            yield return null;
        }

        if (visual != null) visual.localRotation = rotacionBaseVisual;
        estaEntrando = false;
    }

    Vector3 CalcularPosicionCombate(Transform ancla)
    {
        Vector3 forward = ancla.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        return ancla.position
               + (forward * distanciaSeguimiento)
               + (right * puestoFormacionActual.x)
               + (Vector3.up * puestoFormacionActual.y);
    }

    void MovimientoFijo()
    {
        if (objetivo == null) return;

        Vector3 destino = CalcularPosicionCombate(objetivo);
        transform.position = Vector3.Lerp(transform.position, destino, Time.deltaTime * velocidadRespuesta);
    }

    void GestionarCombate()
    {
        RotarHaciaObjetivo();
        temporizadorDisparo -= Time.deltaTime;
        if (temporizadorDisparo <= 0f)
        {
            Disparar();
            temporizadorDisparo = 1f / Mathf.Max(0.0001f, cadenciaDisparo);
        }
    }

    void RotarHaciaObjetivo()
    {
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Vector3 dirRaw = (objetivo.position - origen);
        if (dirRaw.sqrMagnitude < 0.0001f) return;
        Vector3 dir = dirRaw.normalized;
        Quaternion objetivoRot = Quaternion.LookRotation(dir);

        Quaternion nueva = Quaternion.RotateTowards((visual != null) ? visual.rotation : transform.rotation, objetivoRot, velocidadRotacion * Time.deltaTime);
        if (visual != null) visual.rotation = nueva; else transform.rotation = nueva;
    }

    void Disparar()
    {
        if (prefabProyectil == null) return;
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Quaternion rot = boca != null ? boca.rotation * Quaternion.Euler(proyectilRotationOffsetEuler) : Quaternion.LookRotation((objetivo != null ? (objetivo.position - origen).normalized : transform.forward)) * Quaternion.Euler(proyectilRotationOffsetEuler);
        var instancia = Instantiate(prefabProyectil, origen, rot);
        var proj = instancia.GetComponent<Proyectil>(); if (proj != null) { proj.tagOrigen = "Enemy"; proj.origen = this.gameObject; }
        var rb = instancia.GetComponent<Rigidbody>();
        float vel = (proj != null) ? proj.velocidad : 60f;
        if (rb != null) rb.linearVelocity = instancia.transform.forward * vel;
        if (debugVisuals) Debug.Log($"Disparar: origen={origen}, instancia.forward={instancia.transform.forward}, vel={vel}");
    }

    void UpdateDebugVisuals()
    {
        if (!debugVisuals)
        {
            if (lrDeseada != null) Destroy(lrDeseada.gameObject);
            if (lrReal != null) Destroy(lrReal.gameObject);
            lrDeseada = lrReal = null;
            return;
        }
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Vector3 desired = (objetivo != null ? (objetivo.position - origen).normalized : (boca != null ? boca.forward : transform.forward));
        Vector3 real = (boca != null ? boca.forward : transform.forward).normalized;
        Debug.DrawRay(origen, desired * debugLength, Color.cyan);
        Debug.DrawRay(origen, real * debugLength, Color.magenta);
        if (!debugUseLineRenderer) return;
        if (lrDeseada == null) lrDeseada = CreateLine("LR_Des", Color.cyan);
        if (lrReal == null) lrReal = CreateLine("LR_Real", Color.magenta);
        SetLine(lrDeseada, origen, origen + desired * debugLength);
        SetLine(lrReal, origen, origen + real * debugLength);
    }

    LineRenderer CreateLine(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2; lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = lr.endWidth = 0.15f; lr.startColor = lr.endColor = color;
        lr.numCapVertices = 8; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
        return lr;
    }

    void SetLine(LineRenderer lr, Vector3 a, Vector3 b) { if (lr == null) return; lr.SetPosition(0, a); lr.SetPosition(1, b); }

    void OnDisable()
    {
        if (lrDeseada != null) Destroy(lrDeseada.gameObject);
        if (lrReal != null) Destroy(lrReal.gameObject);
        lrDeseada = lrReal = null;

        if (sourceMotor != null && sourceMotor.isPlaying)
        {
            sourceMotor.Stop();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (boca != null)
        {
            Gizmos.color = Color.yellow; Gizmos.DrawSphere(boca.position, 0.12f);
            Gizmos.color = Color.magenta; Gizmos.DrawLine(boca.position, boca.position + boca.forward * 2f);
        }
        else
        {
            Gizmos.color = Color.yellow; Gizmos.DrawSphere(transform.position, 0.12f);
            Gizmos.color = Color.magenta; Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
        if (objetivo != null)
        {
            Gizmos.color = Color.cyan; var o = (boca != null) ? boca.position : transform.position; Gizmos.DrawLine(o, objetivo.position);
        }
    }

    public Vector3 ObtenerDireccionDisparo() { if (boca != null) return boca.forward.normalized; return (transform.rotation * Vector3.forward).normalized; }

    void GestionarTiempoVida() { timerVida -= Time.deltaTime; if (timerVida <= 0f) StartCoroutine(RetirarCoroutine()); }

    IEnumerator RetirarCoroutine()
    {
        estaRetirandose = true;
        ReproducirSonidoManiobra();

        Vector3 dir = transform.right;
        if (objetivo != null)
        {
            float dot = Vector3.Dot((objetivo.position - transform.position).normalized, transform.right);
            dir = (dot > 0f) ? -transform.right : transform.right;
        }
        float t = 0f; while (t < tiempoRetirada) { transform.position += dir.normalized * velocidadRetirada * Time.deltaTime; t += Time.deltaTime; yield return null; }
        Destroy(gameObject);
    }
}