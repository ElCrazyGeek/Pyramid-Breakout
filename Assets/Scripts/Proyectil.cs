using UnityEngine;
using Pyramid.Levels;

public class Proyectil : MonoBehaviour
{
    [Header("Parámetros de Vuelo")]
    [SerializeField] public float velocidad = 140f; // Velocidad alta para conservar la línea estética
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public float danio = 1;

    [Header("Efectos de Audio")]
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
        if (LevelSession.Active) LevelSession.Active.Objects.Forget(gameObject);
    }

    void Start()
    {
        if (sfxDisparo != null)
        {
            AudioSource.PlayClipAtPoint(sfxDisparo, transform.position, volumenSfx);
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Forzamos el avance directo en su eje frontal
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

        // 1. Ignorar disparos contra uno mismo o piezas de la misma nave
        if (!string.IsNullOrEmpty(tagOrigen) && col.CompareTag(tagOrigen)) return;

        if (origen != null)
        {
            var t = col.transform;
            if (t == origen.transform || t.IsChildOf(origen.transform) || origen.transform.IsChildOf(t)) return;
        }

        // 2. Ignorar triggers de interfaz o sensores que no deban bloquear disparos
        if (col.isTrigger && objetivo.GetComponentInParent<Pyramid.Levels.IDamageable>() == null)
        {
            return;
        }

        // 3. Aplicar daño si es un objetivo destructible
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
            // Chocó con un muro, asteroide o suelo sólido del nivel
            consumido = true;
            Destroy(gameObject);
        }
    }
}