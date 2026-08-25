using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [SerializeField] public float velocidad = 60f;
    [SerializeField] public float tiempoVida = 3f;
    [SerializeField] public int danio = 1;

    void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        transform.position += transform.forward * velocidad * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignorar colisiones con el propio jugador
        if (other.CompareTag("Player")) return;

        // Si impacta con un enemigo o jefe
        if (other.CompareTag("Enemy"))
        {
            // Aquí llamarás al script de vida del enemigo (ej. other.GetComponent<EnemyHealth>().RecibirDanio(danio);)
            Destroy(gameObject);
        }
    }
}
