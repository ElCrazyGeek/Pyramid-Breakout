using UnityEngine;
using Pyramid.Levels;

public class Proyectil : MonoBehaviour
{
    [SerializeField] public float velocidad = 60f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public float danio = 1;

    [Header("Origen del proyectil (para evitar \"friendly fire\")")]
    public string tagOrigen = "Player"; // por defecto viene del jugador

    Rigidbody rb;
    bool consumido;

    void OnDestroy() { if (LevelSession.Active) LevelSession.Active.Objects.Forget(gameObject); }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * velocidad;
        }

        Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        // Si no tenemos Rigidbody, movemos manualmente (menos recomendado a altas velocidades)
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
        // Ignorar colisiones con quien disparó
        if (col.CompareTag(tagOrigen)) return;

        consumido = true;
        objetivo.GetComponentInParent<IDamageable>()?.TakeDamage(danio);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
