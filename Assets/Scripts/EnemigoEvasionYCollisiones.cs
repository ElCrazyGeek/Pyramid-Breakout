using UnityEngine;
using Pyramid.Levels;

[RequireComponent(typeof(Collider))]
public class EnemigoEvasionYColisiones : MonoBehaviour
{
    [Header("Evasión de Escenario (Raycast Whisker)")]
    [SerializeField] private LayerMask capaObstaculos; // Default / Escenario
    [SerializeField] private float distanciaSondeo = 12f;
    [SerializeField] private float radioBigote = 1.2f;
    [SerializeField] private float fuerzaEvasion = 8f;

    [Header("Separación entre Enemigos (Flocking)")]
    [SerializeField] private LayerMask capaEnemigos;   // Layer 'Enemigo'
    [SerializeField] private float radioSeparacion = 3.5f;
    [SerializeField] private float fuerzaSeparacion = 5f;

    [Header("Daño si la colisión es inevitable")]
    [SerializeField] private float danioPorChoqueMuro = 3f;
    [SerializeField] private float danioPorChoqueAliado = 1f;
    [SerializeField] private float cooldownImpacto = 0.5f;

    private Health miSalud;
    private Collider miCollider;
    private float tiempoProximoChoque;

    void Awake()
    {
        miCollider = GetComponent<Collider>();
        miSalud = GetComponent<Health>() ?? GetComponentInChildren<Health>();
    }

    void Update()
    {
        EvitarObstaculos();
        SepararseDeAliados();
    }

    /// <summary>
    /// Lanza sensores (frente, izquierda, derecha, arriba, abajo) para anticipar muros y esquivarlos
    /// </summary>
    private void EvitarObstaculos()
    {
        Vector3 direccionEvasion = Vector3.zero;
        Vector3 origen = transform.position;

        // Bigote central
        if (Physics.SphereCast(origen, radioBigote, transform.forward, out RaycastHit hitFrontal, distanciaSondeo, capaObstaculos))
        {
            direccionEvasion += hitFrontal.normal;
        }

        // Sensores diagonales (Cruz)
        Vector3[] direcciones = {
            transform.right,
            -transform.right,
            transform.up,
            -transform.up
        };

        foreach (var dir in direcciones)
        {
            Vector3 diagonal = (transform.forward + dir * 0.5f).normalized;
            if (Physics.Raycast(origen, diagonal, out RaycastHit hit, distanciaSondeo * 0.7f, capaObstaculos))
            {
                // Empuja en dirección contraria al obstáculo detectado
                direccionEvasion += (origen - hit.point).normalized;
            }
        }

        // Aplica el desplazamiento evasivo de forma fluida
        if (direccionEvasion != Vector3.zero)
        {
            direccionEvasion.z = 0; // Evita alterar el avance del riel
            transform.position += direccionEvasion.normalized * fuerzaEvasion * Time.deltaTime;
        }
    }

    /// <summary>
    /// Evita que dos naves enemigas compartan el mismo espacio tridimensional
    /// </summary>
    private void SepararseDeAliados()
    {
        Collider[] aliadosCercanos = Physics.OverlapSphere(transform.position, radioSeparacion, capaEnemigos);
        Vector3 vectorSeparacion = Vector3.zero;
        int aliadosDetectados = 0;

        foreach (var col in aliadosCercanos)
        {
            if (col.gameObject != this.gameObject)
            {
                Vector3 delta = transform.position - col.transform.position;
                float distancia = delta.magnitude;

                if (distancia > 0.001f)
                {
                    // A menor distancia, mayor fuerza de repulsión
                    vectorSeparacion += (delta.normalized / distancia);
                    aliadosDetectados++;
                }
            }
        }

        if (aliadosDetectados > 0)
        {
            vectorSeparacion /= aliadosDetectados;
            vectorSeparacion.z = 0; // Mantener la profundidad
            transform.position += vectorSeparacion.normalized * fuerzaSeparacion * Time.deltaTime;
        }
    }

    // --- Recepción de daño si el choque ocurre físicamente ---
    private void OnTriggerEnter(Collider other) => ProcesarChoque(other.gameObject);
    private void OnCollisionEnter(Collision collision) => ProcesarChoque(collision.gameObject);

    private void ProcesarChoque(GameObject otro)
    {
        if (Time.time < tiempoProximoChoque) return;

        // 1. Ignorar proyectiles propios o el jugador (el jugador tiene su propio script de embestida)
        if (otro.GetComponent<Proyectil>() != null || otro.CompareTag("Player")) return;

        // 2. Choque contra el Escenario / Paredes
        bool esEscenario = otro.CompareTag("Escenario") || otro.layer == LayerMask.NameToLayer("Default");
        if (esEscenario)
        {
            tiempoProximoChoque = Time.time + cooldownImpacto;
            miSalud?.TakeDamage(danioPorChoqueMuro);

            // Rebote de seguridad inmediato para desincrustar
            Vector3 rebase = (transform.position - otro.transform.position).normalized;
            rebase.z = 0;
            transform.position += rebase * 1.5f;
            return;
        }

        // 3. Choque entre dos enemigos aliados
        if (otro.CompareTag("Enemy") || (otro.layer == LayerMask.NameToLayer("Enemigo") && otro != this.gameObject))
        {
            tiempoProximoChoque = Time.time + cooldownImpacto;
            miSalud?.TakeDamage(danioPorChoqueAliado);

            // Se separan inmediatamente
            Vector3 dirRebote = (transform.position - otro.transform.position).normalized;
            dirRebote.z = 0;
            transform.position += dirRebote * 1.2f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizar el radio de separación en Scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioSeparacion);

        // Visualizar el sensor frontal
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * distanciaSondeo);
    }
}