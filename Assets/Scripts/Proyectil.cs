using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [SerializeField] public float velocidad = 60f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public int danio = 1;

    [Header("Origen del proyectil (para evitar \"friendly fire\")")]
    public string tagOrigen = "Player"; // por defecto viene del jugador

    Rigidbody rb;
    private bool impactoProcesado;

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
        ProcesarImpacto(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcesarImpacto(collision.collider);
    }

    private void ProcesarImpacto(Collider col)
    {
        if (impactoProcesado) return;

        // Incluye los colliders hijos de la nave y evita sus propias balas.
        SaludNave salud = col.GetComponentInParent<SaludNave>();
        if (salud != null)
        {
            if (tagOrigen != "Enemy") return;

            impactoProcesado = true;
            salud.RecibirDanio(danio);
            Destroy(gameObject);
            return;
        }

        // Comparar cadenas también funciona si aún no se registró el tag Enemy.
        if (col.tag == tagOrigen) return;

        // Otros impactos (paredes, suelo...): destruir la bala
        impactoProcesado = true;
        Destroy(gameObject);
    }
}
