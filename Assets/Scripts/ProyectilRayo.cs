using UnityEngine;
using UnityEngine.Audio;
using Pyramid.Levels;

public class ProyectilRayo : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    [SerializeField] public float velocidad = 60f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public float danio = 1f;

    [Header("Efectos de Audio y Mixer")]
    [Tooltip("Arrastra aquí el grupo SFX o Enemigos/Jugador")]
    [SerializeField] private AudioMixerGroup grupoMixer;
    [Tooltip("Clip de sonido que se reproducirá al ser disparado")]
    [SerializeField] private AudioClip sfxDisparo;
    [Range(0f, 1f)][SerializeField] private float volumenSfx = 0.9f;

    [Header("Origen (Anti Friendly-Fire)")]
    public string tagOrigen = "Player";
    public GameObject origen;
    public bool debugImpact = false;

    [Header("Crecimiento por Tiempo")]
    [Tooltip("Porcentaje de crecimiento por segundo respecto a su tamaño base")]
    public float porcentajeCrecimientoPorSegundo = 300f;
    [Tooltip("Multiplicador máximo de escala relativa")]
    public float multiplicadorEscalaMaxima = 5f;

    private Rigidbody rb;
    private bool consumido;
    private Vector3 escalaInicial;
    private float factorEscalaActual = 1f;

    void OnDestroy()
    {
        if (LevelSession.Active != null)
            LevelSession.Active.Objects.Forget(gameObject);
    }

    void Start()
    {
        // 1. Audio compatible con Audio Mixer
        ReproducirAudioDisparo();

        // 2. Físicas y movimiento
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * velocidad;
        }

        Destroy(gameObject, tiempoVida);
        escalaInicial = transform.localScale;
    }

    void FixedUpdate()
    {
        // Incremento proporcional respecto a la escala base
        if (factorEscalaActual < multiplicadorEscalaMaxima)
        {
            float incremento = (porcentajeCrecimientoPorSegundo / 100f) * Time.fixedDeltaTime;
            factorEscalaActual = Mathf.Min(factorEscalaActual + incremento, multiplicadorEscalaMaxima);

            // Conserva las proporciones originales del modelo del rayo
            transform.localScale = escalaInicial * factorEscalaActual;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcesarImpacto(other.gameObject, other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcesarImpacto(collision.gameObject, collision.collider);
    }

    private void ProcesarImpacto(GameObject objetivo, Collider col)
    {
        if (consumido) return;

        // 1. Evitar fuego amigo por Tag
        if (!string.IsNullOrEmpty(tagOrigen) && col.CompareTag(tagOrigen)) return;

        // 2. Evitar impacto contra quien disparó o sus componentes hijos
        if (origen != null)
        {
            var t = col.transform;
            if (t == origen.transform || t.IsChildOf(origen.transform) || origen.transform.IsChildOf(t))
                return;
        }

        // 3. Ignorar triggers que sean sensores o mirillas sin componente de salud
        if (col.isTrigger && objetivo.GetComponentInParent<IDamageable>() == null && objetivo.GetComponentInChildren<IDamageable>() == null)
        {
            return;
        }

        consumido = true;

        // 4. Búsqueda exhaustiva de daño (padres o hijos)
        var dmg = objetivo.GetComponentInParent<IDamageable>()
               ?? objetivo.GetComponentInChildren<IDamageable>();

        if (dmg != null)
        {
            dmg.TakeDamage(danio);
        }

        Destroy(gameObject);
    }

    private void ReproducirAudioDisparo()
    {
        if (sfxDisparo == null) return;

        // Creamos un emisor temporal conectado al canal del Audio Mixer
        GameObject audioTemp = new GameObject("SfxDisparoRayoTemp");
        audioTemp.transform.position = transform.position;

        AudioSource src = audioTemp.AddComponent<AudioSource>();
        src.clip = sfxDisparo;
        src.volume = volumenSfx;
        src.spatialBlend = 0.8f; // Audio 3D posicional
        src.minDistance = 5f;
        src.maxDistance = 70f;

        if (grupoMixer != null)
        {
            src.outputAudioMixerGroup = grupoMixer;
        }

        src.Play();
        Destroy(audioTemp, sfxDisparo.length);
    }
}