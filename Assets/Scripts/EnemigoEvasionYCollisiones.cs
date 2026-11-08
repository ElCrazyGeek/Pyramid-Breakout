using System.Collections;
using UnityEngine;
using Pyramid.Levels;

[RequireComponent(typeof(Collider))]
public class EnemigoEvasionYColisiones : MonoBehaviour
{
    [Header("Evasión Preventiva de Escenario (Raycast Whisker)")]
    [SerializeField] private LayerMask capaObstaculos; // Default y Escenario
    [SerializeField] private float distanciaSondeo = 12f;
    [SerializeField] private float radioBigote = 1.2f;
    [SerializeField] private float fuerzaEvasion = 8f;

    [Header("Separación entre Enemigos (Flocking)")]
    [SerializeField] private LayerMask capaEnemigos;   // Layer 'Enemigo'
    [SerializeField] private float radioSeparacion = 3.5f;
    [SerializeField] private float fuerzaSeparacion = 4f;

    [Header("Audio de Choque con el Escenario")]
    [SerializeField] private AudioClip sfxChoqueEscenario;
    [Range(0f, 1f)][SerializeField] private float volumenChoque = 0.8f;

    [Header("Reacción al Choque (Reubicación a Zona Segura)")]
    [SerializeField] private float danioPorChoqueMuro = 5f;
    [SerializeField] private float cooldownImpacto = 0.8f;
    [Tooltip("Rango horizontal y vertical en el que puede reubicarse en pantalla")]
    [SerializeField] private Vector2 limitesAreaSegura = new Vector2(18f, 10f);
    [SerializeField] private float velocidadReubicacion = 15f;
    [SerializeField] private float duracionReubicacion = 0.6f;

    private Health miSalud;
    private float tiempoProximoChoque;
    private bool estaReubicandose = false;

    void Awake()
    {
        miSalud = GetComponent<Health>() ?? GetComponentInChildren<Health>();
    }

    void Update()
    {
        // Si está en medio de la maniobra de reubicación tras chocar, no ejecutamos la evasión estándar
        if (estaReubicandose) return;

        EvitarObstaculos();
        SepararseDeAliados();
    }

    private void EvitarObstaculos()
    {
        Vector3 direccionEvasion = Vector3.zero;
        Vector3 origen = transform.position;

        // Bigote frontal
        if (Physics.SphereCast(origen, radioBigote, transform.forward, out RaycastHit hitFrontal, distanciaSondeo, capaObstaculos))
        {
            direccionEvasion += hitFrontal.normal;
        }

        // Sensores en cruz
        Vector3[] direcciones = { transform.right, -transform.right, transform.up, -transform.up };
        foreach (var dir in direcciones)
        {
            Vector3 diagonal = (transform.forward + dir * 0.5f).normalized;
            if (Physics.Raycast(origen, diagonal, out RaycastHit hit, distanciaSondeo * 0.7f, capaObstaculos))
            {
                direccionEvasion += (origen - hit.point).normalized;
            }
        }

        if (direccionEvasion != Vector3.zero)
        {
            direccionEvasion.z = 0;
            transform.position += direccionEvasion.normalized * fuerzaEvasion * Time.deltaTime;
        }
    }

    private void SepararseDeAliados()
    {
        Collider[] aliados = Physics.OverlapSphere(transform.position, radioSeparacion, capaEnemigos);
        Vector3 repulsion = Vector3.zero;
        int conteo = 0;

        foreach (var col in aliados)
        {
            if (col.gameObject != this.gameObject)
            {
                Vector3 delta = transform.position - col.transform.position;
                float dist = delta.magnitude;
                if (dist > 0.001f)
                {
                    repulsion += (delta.normalized / dist);
                    conteo++;
                }
            }
        }

        if (conteo > 0)
        {
            repulsion /= conteo;
            repulsion.z = 0;
            transform.position += repulsion.normalized * fuerzaSeparacion * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other) => ProcesarContacto(other.gameObject, other);
    private void OnCollisionEnter(Collision collision) => ProcesarContacto(collision.gameObject, collision.collider);

    private void ProcesarContacto(GameObject otro, Collider col)
    {
        if (Time.time < tiempoProximoChoque) return;

        // Ignorar proyectiles propios o al jugador
        if (otro.GetComponent<Proyectil>() != null || otro.CompareTag("Player")) return;

        // Verificar si es escenario
        bool esEscenario = otro.CompareTag("Escenario") || otro.layer == LayerMask.NameToLayer("Default");

        if (esEscenario)
        {
            tiempoProximoChoque = Time.time + cooldownImpacto;

            // 1. Reproducir sonido de impacto del choque
            if (sfxChoqueEscenario != null)
            {
                AudioSource.PlayClipAtPoint(sfxChoqueEscenario, transform.position, volumenChoque);
            }

            // 2. Aplicar daño al enemigo
            if (miSalud != null)
            {
                miSalud.TakeDamage(danioPorChoqueMuro);
            }

            // 3. Si sobrevivió al choque, recolocarse a un carril seguro
            if (gameObject.activeInHierarchy && (miSalud == null || miSalud.Current > 0))
            {
                if (!estaReubicandose)
                {
                    StartCoroutine(RutinaReubicacionSegura(col));
                }
            }
        }
    }

    private IEnumerator RutinaReubicacionSegura(Collider obstaculo)
    {
        estaReubicandose = true;

        // 1. Calcular una dirección de rebote inmediato para despegarse de la superficie
        Vector3 puntoCercano = (obstaculo != null) ? obstaculo.ClosestPoint(transform.position) : transform.position;
        Vector3 vectorEscape = (transform.position - puntoCercano).normalized;
        if (vectorEscape.sqrMagnitude < 0.01f) vectorEscape = Vector3.up;

        // 2. Elegir un nuevo carril objetivo en X/Y (dentro de los límites jugables del riel)
        // Ejemplo: Si chocó a la izquierda (X < 0), lo manda hacia la derecha; si chocó abajo, hacia arriba.
        float nuevoX = (transform.position.x > 0) ? Random.Range(-limitesAreaSegura.x * 0.7f, 0f) : Random.Range(0f, limitesAreaSegura.x * 0.7f);
        float nuevoY = (transform.position.y > 0) ? Random.Range(-limitesAreaSegura.y * 0.6f, 0f) : Random.Range(0f, limitesAreaSegura.y * 0.6f);

        Vector3 destinoLocal = new Vector3(nuevoX, nuevoY, transform.localPosition.z);

        // 3. Desplazar rápidamente la nave durante la duración fijada
        float tiempo = 0f;
        while (tiempo < duracionReubicacion)
        {
            // Combina el impulso de salida de la pared con el viaje hacia el nuevo carril
            Vector3 pasoSalida = vectorEscape * (fuerzaEvasion * 1.5f) * Time.deltaTime;
            pasoSalida.z = 0;
            transform.position += pasoSalida;

            // Interpolación hacia el nuevo carril
            Vector3 posActual = transform.localPosition;
            posActual.x = Mathf.MoveTowards(posActual.x, destinoLocal.x, velocidadReubicacion * Time.deltaTime);
            posActual.y = Mathf.MoveTowards(posActual.y, destinoLocal.y, velocidadReubicacion * Time.deltaTime);
            transform.localPosition = posActual;

            tiempo += Time.deltaTime;
            yield return null;
        }

        estaReubicandose = false;
    }
}