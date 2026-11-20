using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemigoSeguidor : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetivo;
    public Transform boca;
    public Transform visual;

    [Header("Animación de Entrada")]
    public bool usarAnimacionEntrada = true;
    public Vector3 magnitudEntrada = new Vector3(18f, 10f, 20f);
    public float duracionEntrada = 1.2f;
    public float inclinacionEntrada = 45f;

    [Header("Movimiento y Formación (Independiente de Rotación)")]
    public float distanciaSeguimiento = 20f;
    public bool usarPosicionSpawnComoFormacion = true;
    public Vector2 offsetCombateXY = new Vector2(0f, 1.5f);
    [Tooltip("Qué tan rápido mantienen la distancia en Z frente al jugador.")]
    public float velocidadSeguimientoZ = 20f;
    [Tooltip("Qué tan suave se deslizan en X e Y (evita movimientos bruscos).")]
    public float velocidadSuavizadoXY = 2.5f;

    [Header("Evasión al Mouse (Controlada y Suave)")]
    public bool activarEvasionMouse = true;
    [Tooltip("Radio en píxeles alrededor de la nave para detectar que el mouse le apunta.")]
    public float radioDeteccionMousePx = 90f;
    [Tooltip("Tiempo de espera obligatorio entre una evasión y la siguiente.")]
    public float cooldownEvasion = 3f;
    [Tooltip("Distancia máxima que se desplaza en X o en Y al esquivar.")]
    public float rangoEvasion = 4.0f;
    [Tooltip("Cuánto tiempo mantiene la posición esquivada antes de volver a su puesto.")]
    public float duracionEsquive = 1.5f;
    public float inclinacionEvasion = 25f;

    [Header("Apuntado y Combate")]
    public float velocidadRotacion = 6f;
    public float cadenciaDisparo = 1.5f;
    public GameObject prefabProyectil;
    public Vector3 proyectilRotationOffsetEuler = Vector3.zero;

    [Header("Ciclo de Vida y Retirada")]
    public float tiempoVida = 12f;
    public float velocidadRetirada = 12f;
    public float tiempoRetirada = 2.5f;

    [Header("Audio")]
    public bool activarSonidoMotor = true;
    public AudioSource sourceMotor;
    public float pitchMotorCrucero = 1.0f;
    public float pitchMotorManiobra = 1.3f;
    [Range(0f, 1f)] public float volumenMotor = 0.6f;
    public AudioSource sourceSFX;
    public AudioClip sfxManiobraGiro;
    [Range(0f, 1f)] public float volumenManiobra = 0.8f;

    [Header("Debug")]
    public bool debugVisuals = false;
    public bool debugUseLineRenderer = true;
    public float debugLength = 100f;

    // Estado interno
    private float timerVida;
    private float temporizadorDisparo;
    private bool estaEntrando;
    private bool estaRetirandose;

    // Estado de formación y evasión (en ejes fijos del riel/mundo)
    private Vector2 puestoFormacionBase;
    private Vector2 offsetEvasionActual = Vector2.zero;
    private Vector2 offsetEvasionSuavizado = Vector2.zero;
    private float timerCooldownEvasion = 0f;
    private float timerDuracionEsquive = 0f;
    private float rollExtraEvasion = 0f;

    private Vector3 direccionEntradaCalculada;
    private LineRenderer lrDeseada, lrReal;
    private bool visualAutoAdjusted;
    private Camera camPrincipal;

    void Start()
    {
        timerVida = tiempoVida;
        camPrincipal = Camera.main;

        // 1. Buscar objetivo (Jugador)
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

        // 2. Guardar la posición inicial X/Y respecto al jugador usando ejes fijos (sin rotación)
        if (objetivo != null)
        {
            if (usarPosicionSpawnComoFormacion)
            {
                float xRelativo = transform.position.x - objetivo.position.x;
                float yRelativo = transform.position.y - objetivo.position.y;
                puestoFormacionBase = new Vector2(xRelativo, yRelativo);
            }
            else
            {
                puestoFormacionBase = offsetCombateXY;
            }

            float signoX = (puestoFormacionBase.x >= 0f) ? 1f : -1f;
            float signoY = (puestoFormacionBase.y >= 0f) ? 1f : -1f;
            direccionEntradaCalculada = new Vector3(
                magnitudEntrada.x * signoX,
                magnitudEntrada.y * signoY,
                magnitudEntrada.z
            );
        }

        // 3. Auto-ajuste frontal
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
                }
            }
        }

        // 4. Iniciar entrada
        if (usarAnimacionEntrada && objetivo != null)
        {
            StartCoroutine(EntradaCoroutine());
        }
    }

    void Update()
    {
        ActualizarAudioMotor();

        if (estaEntrando || estaRetirandose || objetivo == null) return;

        GestionarEvasionMouse();
        MovimientoFijo();
        GestionarCombate();
        UpdateDebugVisuals();
        GestionarTiempoVida();
    }

    private void GestionarEvasionMouse()
    {
        if (!activarEvasionMouse) return;

        if (timerCooldownEvasion > 0f)
            timerCooldownEvasion -= Time.deltaTime;

        if (timerDuracionEsquive > 0f)
        {
            timerDuracionEsquive -= Time.deltaTime;
            if (timerDuracionEsquive <= 0f)
            {
                offsetEvasionActual = Vector2.zero;
                rollExtraEvasion = 0f;
            }
            return;
        }

        if (timerCooldownEvasion > 0f || timerVida <= 1.5f) return;

        if (camPrincipal == null) camPrincipal = Camera.main;
        if (camPrincipal == null) return;

        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        Vector3 screenPos = camPrincipal.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0f) return;

        float distanciaMousePx = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), mousePos);

        if (distanciaMousePx <= radioDeteccionMousePx)
        {
            timerCooldownEvasion = cooldownEvasion;
            timerDuracionEsquive = duracionEsquive;
            ReproducirSonidoManiobra();

            bool moverEnX = Random.value > 0.5f;
            float direccion = (Random.value > 0.5f) ? 1f : -1f;

            if (moverEnX)
            {
                offsetEvasionActual = new Vector2(rangoEvasion * direccion, 0f);
                rollExtraEvasion = -direccion * inclinacionEvasion;
            }
            else
            {
                offsetEvasionActual = new Vector2(0f, rangoEvasion * direccion);
                rollExtraEvasion = 0f;
            }
        }
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

        bool enManiobra = estaEntrando || estaRetirandose || timerDuracionEsquive > 0f;
        float pitchObjetivo = enManiobra ? pitchMotorManiobra : pitchMotorCrucero;
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

        // Usa los ejes fijos del mundo (right, up, forward) para no girar con la nave del jugador
        Vector3 offsetMundial = Vector3.right * direccionEntradaCalculada.x +
                                Vector3.up * direccionEntradaCalculada.y +
                                Vector3.forward * direccionEntradaCalculada.z;

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
        // Ejes fijos del riel (ignorando la rotación de la nave del jugador)
        Vector2 puestoFinal = puestoFormacionBase + offsetEvasionSuavizado;

        return ancla.position
               + (Vector3.forward * distanciaSeguimiento)
               + (Vector3.right * puestoFinal.x)
               + (Vector3.up * puestoFinal.y);
    }

    void MovimientoFijo()
    {
        if (objetivo == null) return;

        // Suavizamos el desplazamiento de evasión en X/Y para que no sea brusco
        offsetEvasionSuavizado = Vector2.Lerp(offsetEvasionSuavizado, offsetEvasionActual, Time.deltaTime * velocidadSuavizadoXY);

        Vector3 destino = CalcularPosicionCombate(objetivo);

        // Separamos el suavizado de X/Y (suave) del seguimiento en Z (firme)
        float nuevoX = Mathf.Lerp(transform.position.x, destino.x, Time.deltaTime * (velocidadSuavizadoXY * 2f));
        float nuevoY = Mathf.Lerp(transform.position.y, destino.y, Time.deltaTime * (velocidadSuavizadoXY * 2f));
        float nuevoZ = Mathf.Lerp(transform.position.z, destino.z, Time.deltaTime * velocidadSeguimientoZ);

        transform.position = new Vector3(nuevoX, nuevoY, nuevoZ);
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
        Quaternion objetivoRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, rollExtraEvasion);

        Quaternion nueva = Quaternion.RotateTowards((visual != null) ? visual.rotation : transform.rotation, objetivoRot, velocidadRotacion * Time.deltaTime);
        if (visual != null) visual.rotation = nueva; else transform.rotation = nueva;
    }

    void Disparar()
    {
        if (prefabProyectil == null) return;
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Vector3 dirHaciaObjetivo = (objetivo != null) ? (objetivo.position - origen).normalized : transform.forward;
        Quaternion rot = Quaternion.LookRotation(dirHaciaObjetivo) * Quaternion.Euler(proyectilRotationOffsetEuler);

        var instancia = Instantiate(prefabProyectil, origen, rot);
        var proj = instancia.GetComponent<Proyectil>();
        if (proj != null) { proj.tagOrigen = "Enemy"; proj.origen = this.gameObject; }
        var rb = instancia.GetComponent<Rigidbody>();
        float vel = (proj != null) ? proj.velocidad : 60f;
        if (rb != null) rb.linearVelocity = instancia.transform.forward * vel;
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

    public Vector3 ObtenerDireccionDisparo() { if (boca != null) return boca.forward.normalized; return (transform.rotation * Vector3.forward).normalized; }

    void GestionarTiempoVida()
    {
        timerVida -= Time.deltaTime;
        if (timerVida <= 0f) StartCoroutine(RetirarCoroutine());
    }

    IEnumerator RetirarCoroutine()
    {
        estaRetirandose = true;
        ReproducirSonidoManiobra();

        // Elegir lado de salida en ejes fijos (izquierda o derecha del túnel)
        float signoSalida = (transform.position.x >= (objetivo != null ? objetivo.position.x : 0f)) ? 1f : -1f;
        Vector3 dirSalida = Vector3.right * signoSalida;

        float t = 0f;
        while (t < tiempoRetirada)
        {
            float dt = Time.deltaTime;
            t += dt;

            // Sigue avanzando en Z mientras se desplaza suavemente hacia afuera para que puedas dispararle
            Vector3 avanceZ = Vector3.forward * 15f;
            transform.position += (dirSalida * velocidadRetirada + avanceZ) * dt;

            if (visual != null)
            {
                visual.Rotate(0f, 0f, -150f * signoSalida * dt, Space.Self);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}