using UnityEngine;
using UnityEngine.Audio;
using Pyramid.Levels;

public class ProyectilCargado : MonoBehaviour
{
    [Header("Parámetros de Vuelo")]
    [SerializeField] public float velocidad = 90f;
    [SerializeField] public float velocidadGiroHoming = 25f;
    [SerializeField] public float tiempoVida = 4f;

    [Header("Detonación por Proximidad (Anti-Órbita)")]
    [SerializeField] private float distanciaDetonacionCercana = 2.5f;

    [Header("Explosión y Daño en Área (AoE)")]
    [SerializeField] public float danioDirecto = 10f;
    [SerializeField] public float radioExplosion = 8f;
    [SerializeField] public GameObject prefabExplosion;

    [Header("Audio y Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixer;
    [SerializeField] private AudioClip sfxLanzamiento;
    [SerializeField] private AudioClip sfxExplosion;
    [Range(0f, 1f)][SerializeField] private float volumen = 0.9f;

    [HideInInspector] public Transform objetivoFijado;
    [HideInInspector] public string tagOrigen = "Player";
    [HideInInspector] public GameObject origen;
    [HideInInspector] public LockOnTargetUI recuadroUI; // Referencia al cuadro rojo

    private Rigidbody rb;
    private bool detonado = false;
    private float distanciaAnterior = float.MaxValue;

    void OnDestroy()
    {
        // Si el proyectil muere por cualquier razón, apagar el recuadro
        if (recuadroUI != null)
        {
            recuadroUI.Ocultar();
        }

        if (LevelSession.Active != null)
            LevelSession.Active.Objects.Forget(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (sfxLanzamiento != null)
        {
            ReproducirAudio(sfxLanzamiento, transform.position);
        }

        Invoke(nameof(DetonarExplosion), tiempoVida);
    }

    void Update()
    {
        if (detonado) return;

        if (objetivoFijado != null)
        {
            Vector3 vectorAlObjetivo = objetivoFijado.position - transform.position;
            float distanciaActual = vectorAlObjetivo.magnitude;

            if (distanciaActual <= distanciaDetonacionCercana)
            {
                DetonarExplosion();
                return;
            }

            if (distanciaActual < (distanciaDetonacionCercana * 1.8f) && distanciaActual > distanciaAnterior)
            {
                DetonarExplosion();
                return;
            }

            distanciaAnterior = distanciaActual;

            Vector3 direccionDeseada = vectorAlObjetivo.normalized;
            transform.forward = Vector3.RotateTowards(transform.forward, direccionDeseada, velocidadGiroHoming * Time.deltaTime, 0f);
        }
        else
        {
            // Si el enemigo fue destruido en el camino, apagar el cuadro de inmediato
            if (recuadroUI != null)
            {
                recuadroUI.Ocultar();
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = transform.forward * velocidad;
        }
        else
        {
            transform.position += transform.forward * velocidad * Time.deltaTime;
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

    private void ProcesarImpacto(GameObject colObj, Collider col)
    {
        if (detonado) return;

        if (!string.IsNullOrEmpty(tagOrigen) && col.CompareTag(tagOrigen)) return;
        if (origen != null && (col.transform == origen.transform || col.transform.IsChildOf(origen.transform) || origen.transform.IsChildOf(col.transform))) return;

        if (col.isTrigger && colObj.GetComponentInParent<IDamageable>() == null && colObj.GetComponentInChildren<IDamageable>() == null)
            return;

        DetonarExplosion();
    }

    private void DetonarExplosion()
    {
        if (detonado) return;
        detonado = true;
        CancelInvoke(nameof(DetonarExplosion));

        // Apagar el recuadro rojo en el instante exacto de la detonación
        if (recuadroUI != null)
        {
            recuadroUI.Ocultar();
        }

        if (sfxExplosion != null)
        {
            ReproducirAudio(sfxExplosion, transform.position);
        }

        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);
        }

        Collider[] impactados = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (Collider hit in impactados)
        {
            if (!string.IsNullOrEmpty(tagOrigen) && hit.CompareTag(tagOrigen)) continue;

            var damageable = hit.GetComponentInParent<IDamageable>() ?? hit.GetComponentInChildren<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(danioDirecto);
            }
        }

        Destroy(gameObject);
    }

    private void ReproducirAudio(AudioClip clip, Vector3 posicion)
    {
        GameObject audioTemp = new GameObject("SfxTemp_" + clip.name);
        audioTemp.transform.position = posicion;

        AudioSource src = audioTemp.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volumen;
        src.spatialBlend = 0.85f;
        src.minDistance = 6f;
        src.maxDistance = 100f;

        if (grupoMixer != null)
        {
            src.outputAudioMixerGroup = grupoMixer;
        }

        src.Play();
        Destroy(audioTemp, clip.length);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDetonacionCercana);
    }
}