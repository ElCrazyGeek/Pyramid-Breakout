using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [SerializeField] public float velocidad = 60f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public int danio = 1;

    [Header("Origen del proyectil (para evitar \"friendly fire\")")]
    public string tagOrigen = "Player"; // por defecto viene del jugador

    Rigidbody rb;

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
        // Ignorar colisiones con quien disparó
        if (col.CompareTag(tagOrigen)) return;

        // Impacto en enemigo
        if (col.CompareTag("Enemy") && tagOrigen != "Enemy")
        {
            // Llamar al componente de vida si existe
            // var vida = col.GetComponent<EnemyHealth>(); if (vida!=null) vida.RecibirDanio(danio);
            Destroy(gameObject);
            return;
        }

        // Impacto en jugador
        if (col.CompareTag("Player") && tagOrigen != "Player")
        {
            Debug.Log("Proyectil: golpeó al jugador");
            // Aplicar daño al jugador si existe componente de salud
            // var salud = col.GetComponent<PlayerHealth>(); if (salud!=null) salud.RecibirDanio(danio);
            Destroy(gameObject);
            return;
        }

        // Otros impactos (paredes, suelo...): destruir la bala
        Destroy(gameObject);
    }
}
