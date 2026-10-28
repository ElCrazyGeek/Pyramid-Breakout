using UnityEngine;
using Pyramid.Levels;

public class ProyectilRayo : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    [SerializeField] public float velocidad = 60f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public float danio = 1;

    [Header("Origen (Anti Friendly-Fire)")]
    public string tagOrigen = "Player";
    public GameObject origen;
    public bool debugImpact = false;

    [Header("Crecimiento por Tiempo")]
    public float porcentajeCrecimientoPorSegundo = 300f;
    public float escalaMaxima = 5f;

    private Rigidbody rb;
    private bool consumido;
    private Vector3 escalaInicial;

    void OnDestroy()
    {
        if (LevelSession.Active) LevelSession.Active.Objects.Forget(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // El proyectil avanza por su eje frontal.
            rb.linearVelocity = transform.forward * velocidad;
        }

        Destroy(gameObject, tiempoVida);
        escalaInicial = transform.localScale;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        float incremento = (porcentajeCrecimientoPorSegundo / 100f) * Time.fixedDeltaTime;
        transform.localScale += escalaInicial * incremento;

        float escalaClamped = Mathf.Min(transform.localScale.x, escalaMaxima);
        transform.localScale = new Vector3(escalaClamped, escalaClamped, escalaClamped);
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

        if (!string.IsNullOrEmpty(tagOrigen) && col.CompareTag(tagOrigen)) return;

        if (origen != null)
        {
            var t = col.transform;
            if (t == origen.transform || t.IsChildOf(origen.transform)) return;
        }

        consumido = true;
        var dmg = objetivo.GetComponentInParent<Pyramid.Levels.IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(danio);
        }
        Destroy(gameObject);
    }
}