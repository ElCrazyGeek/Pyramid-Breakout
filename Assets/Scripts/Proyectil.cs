using UnityEngine;
using UnityEngine.Audio;
using Pyramid.Levels;

public class Proyectil : MonoBehaviour
{
    [Header("Parámetros de Vuelo")]
    [SerializeField] public float velocidad = 140f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public float danio = 1;

    [Header("Efectos de Audio y Mixer")]
    [Tooltip("Arrastra aquí el grupo SFX, Jugador o Enemigos del AudioMixer")]
    [SerializeField] private AudioMixerGroup grupoMixer;
    [Tooltip("Clip de sonido que se reproducirá al ser disparado")]
    [SerializeField] private AudioClip sfxDisparo;
    [Range(0f, 1f)][SerializeField] private float volumenSfx = 0.8f;

    [Header("Origen del proyectil (Anti-Friendly Fire)")]
    public string tagOrigen = "Player";
    public GameObject origen;
    public bool debugImpact = false;

    private Rigidbody rb;
    private bool consumido;

    void OnDestroy()
    {
        if (LevelSession.Active != null)
            LevelSession.Active.Objects.Forget(gameObject);
    }

    void Start()
    {
        // Reproducir sonido integrado con el AudioMixer
        ReproducirAudioDisparo();

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * velocidad;
        }

        Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        if (rb == null)
            transform.position += transform.forward * velocidad * Time.deltaTime;
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

        // 1. Ignorar disparos contra el propio lanzador
        if (!string.IsNullOrEmpty(tagOrigen) && col.CompareTag(tagOrigen)) return;

        if (origen != null)
        {
            var t = col.transform;
            if (t == origen.transform || t.IsChildOf(origen.transform) || origen.transform.IsChildOf(t)) return;
        }

        // 2. Ignorar triggers de UI, mirillas o sensores sin daño
        if (col.isTrigger && objetivo.GetComponentInParent<Pyramid.Levels.IDamageable>() == null && objetivo.GetComponentInChildren<Pyramid.Levels.IDamageable>() == null)
        {
            return;
        }

        // 3. Aplicar daño
        var dmg = objetivo.GetComponentInParent<Pyramid.Levels.IDamageable>()
               ?? objetivo.GetComponentInChildren<Pyramid.Levels.IDamageable>();

        if (dmg != null)
        {
            consumido = true;
            if (debugImpact) Debug.Log($"Proyectil impactó a {objetivo.name}. Aplicando daño: {danio}");
            dmg.TakeDamage(danio);
            Destroy(gameObject);
        }
        else if (objetivo.CompareTag("Escenario") || objetivo.layer == LayerMask.NameToLayer("Default"))
        {
            consumido = true;
            Destroy(gameObject);
        }
    }

    private void ReproducirAudioDisparo()
    {
        if (sfxDisparo == null) return;

        // Objeto temporal que reproduce en la posición 3D sin depender del ciclo de vida del proyectil
        GameObject audioTemp = new GameObject("SfxDisparoTemp");
        audioTemp.transform.position = transform.position;

        AudioSource src = audioTemp.AddComponent<AudioSource>();
        src.clip = sfxDisparo;
        src.volume = volumenSfx;
        src.spatialBlend = 0.8f; // Audio 3D espacial
        src.minDistance = 5f;
        src.maxDistance = 70f;

        // Enlazar al canal del Audio Mixer
        if (grupoMixer != null)
        {
            src.outputAudioMixerGroup = grupoMixer;
        }

        src.Play();
        Destroy(audioTemp, sfxDisparo.length);
    }
}