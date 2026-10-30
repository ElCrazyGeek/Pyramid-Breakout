using UnityEngine;
using Pyramid.Levels;

public class Proyectil : MonoBehaviour
{
    [SerializeField] public float velocidad = 60f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public float danio = 1;

    [Header("Efectos de Audio")]
    [Tooltip("Clip de sonido que se reproducirá al ser disparado")]
    [SerializeField] private AudioClip sfxDisparo;
    [Range(0f, 1f)][SerializeField] private float volumenSfx = 0.8f;

    [Header("Origen del proyectil")]
    public string tagOrigen = "Player";
    public GameObject origen;
    public bool debugImpact = false;

    Rigidbody rb;
    bool consumido;

    void OnDestroy()
    {
        if (LevelSession.Active) LevelSession.Active.Objects.Forget(gameObject);
    }

    void Start()
    {
        // Reproducir sonido de disparo al instante
        if (sfxDisparo != null)
        {
            AudioSource.PlayClipAtPoint(sfxDisparo, transform.position, volumenSfx);
        }

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

        if (!string.IsNullOrEmpty(tagOrigen) && col.CompareTag(tagOrigen))
        {
            if (debugImpact) Debug.Log($"Proyectil ignoró impacto por tag: tagOrigen={tagOrigen}, colliderTag={col.tag}");
            return;
        }

        if (origen != null)
        {
            var t = col.transform;
            if (t == origen.transform || t.IsChildOf(origen.transform))
            {
                if (debugImpact) Debug.Log($"Proyectil ignoró impacto por origen: origen={origen.name}, collider={col.name}");
                return;
            }
        }

        consumido = true;
        var dmg = objetivo.GetComponentInParent<Pyramid.Levels.IDamageable>();
        if (dmg != null)
        {
            if (debugImpact) Debug.Log($"Proyectil aplicando {danio} de daño a {objetivo.name} (IDamageable en {((Component)dmg).gameObject.name})");
            dmg.TakeDamage(danio);
            Destroy(gameObject);
        }
        else
        {
            if (debugImpact) Debug.Log($"Proyectil impacto en {objetivo.name} pero no encontró IDamageable. collider.tag={col.tag}");
            Destroy(gameObject);
        }
    }
}