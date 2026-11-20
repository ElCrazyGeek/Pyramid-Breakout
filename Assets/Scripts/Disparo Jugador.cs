using UnityEngine;
using UnityEngine.Audio; // <-- Necesario para AudioMixerGroup
using Pyramid.Levels;
using UnityEngine.InputSystem;

public class DisparoJugador : MonoBehaviour
{
    [Header("Disparo Normal")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform[] puntosDisparo;
    [SerializeField] public RectTransform mirillaLejanaUI;
    [SerializeField] private float delayEntreDisparos = 0.09f;
    [SerializeField] private float distanciaHorizonte = 500f;
    [SerializeField] private LayerMask capasImpacto = ~0;

    [Header("Disparo Cargado (Star Fox Lock-on)")]
    [SerializeField] private GameObject prefabProyectilCargado;
    [SerializeField] private Transform puntoSalidaCargado;
    [SerializeField] private ParticleSystem efectoCarga;
    [SerializeField] private float tiempoNecesarioCarga = 0.9f;
    [Tooltip("Porcentaje del tiempo de carga (0.1 a 0.9) en el que empieza el análisis visual y sonoro")]
    [Range(0.1f, 0.9f)][SerializeField] private float umbralInicioAnalisis = 0.45f;

    [Header("Audio Lock-on y Mixer")]
    [Tooltip("Arrastra aquí el grupo de tu Audio Mixer (ej. UI, SFX o Jugador)")]
    [SerializeField] private AudioMixerGroup grupoMixerLockOn;
    [Tooltip("Sonido de escaneo/análisis que suena justo antes de fijar objetivo")]
    [SerializeField] private AudioClip sfxAnalisis;
    [Tooltip("Sonido de confirmación cuando el cuadro rojo engancha al enemigo")]
    [SerializeField] private AudioClip sfxLockOn;
    [Range(0f, 1f)][SerializeField] private float volumenSfx = 1f;

    [Header("Estado")]
    public bool puedeDisparar = true;

    [Header("UI Lock-On")]
    [SerializeField] private LockOnTargetUI recuadroLockOn;

    // Fuente de audio conectada al Mixer
    private AudioSource sourceLockOn;

    // Control interno de carga
    private float tiempoSiguienteDisparo = 0f;
    private float tiempoPresionado = 0f;
    private bool estaPresionando = false;
    private bool analisisIniciado = false;
    private bool cargaCompleta = false;

    private GameObject proyectilCargadoActivo;
    private Transform blancoFijado = null;

    private void Awake()
    {
        // Configurar AudioSource en 2D para que no pierda volumen por distancia y respete el Mixer
        sourceLockOn = gameObject.AddComponent<AudioSource>();
        sourceLockOn.playOnAwake = false;
        sourceLockOn.spatialBlend = 0f;

        if (grupoMixerLockOn != null)
        {
            sourceLockOn.outputAudioMixerGroup = grupoMixerLockOn;
        }
    }

    public void OnDisparo(InputValue value)
    {
        if (!enabled || !puedeDisparar || Time.timeScale <= 0) return;
        if (GameManager.Instance != null && GameManager.Instance.ModoActual == ModoVuelo.Carrera) return;

        bool pulsado = value.isPressed;

        if (pulsado)
        {
            if (!estaPresionando) IniciarPulsacion();
        }
        else
        {
            if (estaPresionando) LiberarPulsacion();
        }
    }

    void Update()
    {
        bool teclaEspacioSostenida = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        bool mouseClicSostenido = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool gamepadTriggerSostenido = Gamepad.current != null && Gamepad.current.rightTrigger.isPressed;
        bool gamepadBotonSostenido = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;

        bool algunInputDisparoActivo = teclaEspacioSostenida || mouseClicSostenido || gamepadTriggerSostenido || gamepadBotonSostenido;

        if (algunInputDisparoActivo)
        {
            if (!estaPresionando) IniciarPulsacion();
        }
        else if (estaPresionando)
        {
            LiberarPulsacion();
        }

        if (estaPresionando)
        {
            tiempoPresionado += Time.deltaTime;

            // FASE 1: Pre-carga / Análisis (suena el escáner y aparece el cuadro previo en la mira)
            if (!analisisIniciado && tiempoPresionado >= (tiempoNecesarioCarga * umbralInicioAnalisis))
            {
                analisisIniciado = true;

                if (sfxAnalisis != null && sourceLockOn != null)
                {
                    sourceLockOn.PlayOneShot(sfxAnalisis, volumenSfx);
                }

                if (proyectilCargadoActivo == null && recuadroLockOn != null && blancoFijado == null)
                {
                    recuadroLockOn.IniciarEscaneo(mirillaLejanaUI);
                }
            }

            // FASE 2: Carga completa -> Escaneo activo para enganchar objetivo
            if (tiempoPresionado >= tiempoNecesarioCarga)
            {
                if (!cargaCompleta)
                {
                    cargaCompleta = true;

                    if (efectoCarga != null)
                    {
                        efectoCarga.gameObject.SetActive(true);
                        if (!efectoCarga.isPlaying) efectoCarga.Play();
                    }
                }

                EscanearLockOn();
            }
        }
    }

    private void IniciarPulsacion()
    {
        estaPresionando = true;
        tiempoPresionado = 0f;
        analisisIniciado = false;
        cargaCompleta = false;

        if (proyectilCargadoActivo == null)
        {
            blancoFijado = null;
            if (recuadroLockOn != null) recuadroLockOn.Ocultar();
        }

        if (Time.time >= tiempoSiguienteDisparo)
        {
            DispararNormal();
            tiempoSiguienteDisparo = Time.time + delayEntreDisparos;
        }
    }

    private void LiberarPulsacion()
    {
        if (!estaPresionando) return;

        estaPresionando = false;

        if (cargaCompleta)
        {
            DispararCargado();
        }
        else
        {
            if (proyectilCargadoActivo == null && recuadroLockOn != null)
            {
                recuadroLockOn.Ocultar();
            }
        }

        analisisIniciado = false;
        cargaCompleta = false;

        if (efectoCarga != null)
        {
            efectoCarga.Stop();
            efectoCarga.gameObject.SetActive(false);
        }
    }

    private void EscanearLockOn()
    {
        Camera cam = Camera.main;
        if (cam == null || mirillaLejanaUI == null) return;

        Ray rayo = RectTransformUtility.ScreenPointToRay(cam, mirillaLejanaUI.position);
        if (Physics.Raycast(rayo, out RaycastHit hit, distanciaHorizonte, capasImpacto))
        {
            var damageable = hit.collider.GetComponentInParent<IDamageable>()
                          ?? hit.collider.GetComponentInChildren<IDamageable>();

            if (damageable != null)
            {
                Transform nuevoObjetivo = ((Component)damageable).transform;

                if (blancoFijado != nuevoObjetivo)
                {
                    blancoFijado = nuevoObjetivo;

                    if (recuadroLockOn != null)
                    {
                        recuadroLockOn.FijarObjetivo(blancoFijado);
                    }

                    if (sfxLockOn != null && sourceLockOn != null)
                    {
                        sourceLockOn.PlayOneShot(sfxLockOn, volumenSfx);
                    }
                }
            }
        }
    }

    private void DispararCargado()
    {
        if (prefabProyectilCargado == null) return;
        if (proyectilCargadoActivo != null) return;

        Transform spawnPoint = (puntoSalidaCargado != null) ? puntoSalidaCargado : transform;
        Camera cam = Camera.main;

        Vector3 direccion;
        if (blancoFijado != null)
        {
            direccion = (blancoFijado.position - spawnPoint.position).normalized;
        }
        else if (cam != null && mirillaLejanaUI != null)
        {
            Ray rayo = RectTransformUtility.ScreenPointToRay(cam, mirillaLejanaUI.position);
            direccion = (rayo.GetPoint(distanciaHorizonte) - spawnPoint.position).normalized;
        }
        else
        {
            direccion = spawnPoint.forward;
        }

        Quaternion rotacion = Quaternion.LookRotation(direccion);
        GameObject proyectil = Instantiate(prefabProyectilCargado, spawnPoint.position, rotacion);
        proyectilCargadoActivo = proyectil;

        var compCargado = proyectil.GetComponent<ProyectilCargado>();
        if (compCargado != null)
        {
            compCargado.origen = transform.root.gameObject;
            compCargado.tagOrigen = "Player";
            compCargado.objetivoFijado = blancoFijado;
            compCargado.recuadroUI = recuadroLockOn;
        }

        if (LevelSession.Active != null)
            LevelSession.Active.Objects.Track(proyectil);

        if (blancoFijado == null && recuadroLockOn != null)
        {
            recuadroLockOn.Ocultar();
        }

        blancoFijado = null;
    }

    private void DispararNormal()
    {
        if (prefabProyectil == null || puntosDisparo.Length == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray rayo = (mirillaLejanaUI != null)
            ? RectTransformUtility.ScreenPointToRay(cam, mirillaLejanaUI.position)
            : new Ray(cam.transform.position, cam.transform.forward);

        Vector3 puntoImpacto = Physics.Raycast(rayo, out RaycastHit hit, distanciaHorizonte, capasImpacto)
            ? hit.point
            : rayo.GetPoint(distanciaHorizonte);

        foreach (Transform punto in puntosDisparo)
        {
            Vector3 dir = (puntoImpacto - punto.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);

            GameObject bala = Instantiate(prefabProyectil, punto.position, rot);
            var comp = bala.GetComponent<Proyectil>();
            if (comp != null)
            {
                comp.origen = transform.root.gameObject;
                comp.tagOrigen = "Player";
            }

            if (LevelSession.Active != null)
                LevelSession.Active.Objects.Track(bala);
        }
    }

    public void ResetForLevel()
    {
        tiempoSiguienteDisparo = 0;
        LiberarPulsacion();
        if (recuadroLockOn != null) recuadroLockOn.Ocultar();
    }
}