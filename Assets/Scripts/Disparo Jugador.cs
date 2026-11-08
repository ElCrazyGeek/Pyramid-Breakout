using UnityEngine;
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

    [Header("Audio Lock-on")]
    [SerializeField] private AudioClip sfxLockOn;
    [SerializeField] private AudioClip sfxCargaLista;
    [Range(0f, 1f)][SerializeField] private float volumenSfx = 0.8f;

    [Header("Estado")]
    public bool puedeDisparar = true;

    // Control interno de carga
    private float tiempoSiguienteDisparo = 0f;
    private float tiempoPresionado = 0f;
    private bool estaPresionando = false;
    private bool cargaCompleta = false;
    private bool audioCargaReproducido = false;

    private Transform blancoFijado = null;

    // Mensaje automático del Input System
    public void OnDisparo(InputValue value)
    {
        if (!enabled || !puedeDisparar || Time.timeScale <= 0) return;
        if (GameManager.Instance != null && GameManager.Instance.ModoActual == ModoVuelo.Carrera) return;

        bool pulsado = value.isPressed;

        if (pulsado)
        {
            IniciarPulsacion();
        }
        else
        {
            LiberarPulsacion();
        }
    }

    void Update()
    {
        // Respaldo directo de hardware (asegura detección de release aunque Send Messages falle)
        bool teclaEspacioSostenida = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        bool gamepadBotonSostenido = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed; // A / Cruz

        if (teclaEspacioSostenida || gamepadBotonSostenido)
        {
            if (!estaPresionando)
            {
                IniciarPulsacion();
            }
        }
        else if (estaPresionando)
        {
            LiberarPulsacion();
        }

        // Proceso de carga continua
        if (estaPresionando)
        {
            tiempoPresionado += Time.deltaTime;

            if (tiempoPresionado >= tiempoNecesarioCarga)
            {
                if (!cargaCompleta)
                {
                    cargaCompleta = true;

                    // Encender partículas de carga
                    if (efectoCarga != null)
                    {
                        efectoCarga.gameObject.SetActive(true);
                        if (!efectoCarga.isPlaying) efectoCarga.Play();
                    }

                    // Sonido de listo
                    if (sfxCargaLista != null && !audioCargaReproducido)
                    {
                        AudioSource.PlayClipAtPoint(sfxCargaLista, transform.position, volumenSfx);
                        audioCargaReproducido = true;
                    }
                }

                // Buscar objetivo para lock-on mientras esté cargado
                EscanearLockOn();
            }
        }
    }

    private void IniciarPulsacion()
    {
        estaPresionando = true;
        tiempoPresionado = 0f;
        cargaCompleta = false;
        audioCargaReproducido = false;
        blancoFijado = null;

        // Disparo normal inmediato al primer toque
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

        // Si se mantuvo el tiempo necesario, dispara el ataque cargado
        if (cargaCompleta)
        {
            DispararCargado();
        }

        // Apagar efectos y limpiar estado
        cargaCompleta = false;
        blancoFijado = null;

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
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Transform nuevoObjetivo = ((Component)damageable).transform;
                if (blancoFijado != nuevoObjetivo)
                {
                    blancoFijado = nuevoObjetivo;
                    if (sfxLockOn != null)
                        AudioSource.PlayClipAtPoint(sfxLockOn, transform.position, volumenSfx);
                }
            }
        }
    }

    private void DispararCargado()
    {
        if (prefabProyectilCargado == null) return;

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

        var compCargado = proyectil.GetComponent<ProyectilCargado>();
        if (compCargado != null)
        {
            compCargado.origen = transform.root.gameObject;
            compCargado.tagOrigen = "Player";
            compCargado.objetivoFijado = blancoFijado;
        }

        if (LevelSession.Active != null)
            LevelSession.Active.Objects.Track(proyectil);
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
    }
}