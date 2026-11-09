using UnityEngine;
using Pyramid.Levels;

public class ProyectilCargado : MonoBehaviour
{
    [Header("Parámetros de Vuelo")]
    [SerializeField] public float velocidad = 90f;
    [SerializeField] public float velocidadGiroHoming = 18f;
    [SerializeField] public float tiempoVida = 4f;

    [Header("Explosión y Daño en Área (AoE)")]
    [SerializeField] public float danioDirecto = 10f;
    [SerializeField] public float radioExplosion = 8f;
    [SerializeField] public GameObject prefabExplosion;

    [Header("Efectos de Audio")]
    [SerializeField] private AudioClip sfxLanzamiento;
    [SerializeField] private AudioClip sfxExplosion;
    [Range(0f, 1f)][SerializeField] private float volumen = 0.9f;

    [HideInInspector] public Transform objetivoFijado;
    [HideInInspector] public string tagOrigen = "Player";
    [HideInInspector] public GameObject origen;

    private Rigidbody rb;
    private bool detonado = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (sfxLanzamiento != null)
        {
            AudioSource.PlayClipAtPoint(sfxLanzamiento, transform.position, volumen);
        }

        Invoke(nameof(DetonarExplosion), tiempoVida);
    }

    void Update()
    {
        // 1. Curvar la trayectoria hacia el blanco fijado si aún existe
        if (objetivoFijado != null)
        {
            Vector3 direccionDeseada = (objetivoFijado.position - transform.position).normalized;
            transform.forward = Vector3.RotateTowards(transform.forward, direccionDeseada, velocidadGiroHoming * Time.deltaTime, 0f);
        }

        // 2. Movimiento continuo
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

        // Evitar auto-daño con la propia nave
        if (!string.IsNullOrEmpty(tagOrigen) && col.CompareTag(tagOrigen)) return;
        if (origen != null && (col.transform == origen.transform || col.transform.IsChildOf(origen.transform))) return;
        if (col.isTrigger && colObj.GetComponentInParent<IDamageable>() == null) return;

        DetonarExplosion();
    }

    private void DetonarExplosion()
    {
        detonado = true;

        if (sfxExplosion != null)
        {
            AudioSource.PlayClipAtPoint(sfxExplosion, transform.position, volumen);
        }

        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);
        }

        // Daño de área a todos los enemigos en el radio
        Collider[] impactados = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (Collider hit in impactados)
        {
            if (hit.CompareTag(tagOrigen)) continue;

            var damageable = hit.GetComponentInParent<IDamageable>() ?? hit.GetComponentInChildren<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(danioDirecto);
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}