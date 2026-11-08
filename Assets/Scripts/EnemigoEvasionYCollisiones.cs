using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
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

    [Header("Audio de Choque y Audio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixer; // Conectar al canal Enemigos o SFX
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
    private AudioSource audioSourceLocal;
    private float tiempoProximoChoque;
    private bool estaReubicandose = false;

    void Awake()
    {
        miSalud = GetComponent<Health>() ?? GetComponentInChildren<Health>();

        // Reutilizar el AudioSource del objeto o crear uno configurado al Mixer
        audioSourceLocal = GetComponent<AudioSource>();
        if (audioSourceLocal == null)
        {
            audioSourceLocal = gameObject.AddComponent<AudioSource>();
            audioSourceLocal.playOnAwake = false;
            audioSourceLocal.spatialBlend = 0.8f;
            audioSourceLocal.minDistance = 5f;
            audioSourceLocal.maxDistance = 60f;
        }

        if (grupoMixer != null)
        {
            audioSourceLocal.outputAudioMixerGroup = grupoMixer;
        }
    }

    void Update()
    {
        // Si está en maniobra de salida de pared, no ejecutamos la evasión estándar
        if (estaReubicandose) return;

        EvitarObstaculos();
        SepararseDeAliados();
    }

    private void EvitarObstaculos()
    {
        Vector3 direccionEvasion = Vector3.zero;
        Vector3 origen = transform.position;

        // Bigote frontal con SphereCast
        if (Physics.SphereCast(origen, radioBigote, transform.forward, out RaycastHit hitFrontal, distanciaSondeo, capaObstaculos))
        {
            direccionEvasion += hitFrontal.normal;
        }

        // Sensores diagonales en cruz
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
            // Bloqueamos el eje Z para no frenar ni acelerar el ritmo del riel
            Vector3 desvio = direccionEvasion.normalized * fuerzaEvasion * Time.deltaTime;
            desvio.z = 0f;
            transform.position += desvio;
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
            repulsion.z = 0f;
            transform.position += repulsion.normalized * fuerzaSeparacion * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other) => ProcesarContacto(other.gameObject, other);
    private void OnCollisionEnter(Collision collision) => ProcesarContacto(collision.gameObject, collision.collider);

    private void ProcesarContacto(GameObject otro, Collider col)
    {
        if (Time.time < tiempoProximoChoque) return;

        // Ignorar proyectiles propios, amigos o a la propia nave del jugador
        if (otro.GetComponent<Proyectil>() != null || otro.CompareTag("Player") || otro.CompareTag("Enemy")) return;

        // Verificar si es escenario (por Tag o Layer)
        bool esEscenario = otro.CompareTag("Escenario") || otro.layer == LayerMask.NameToLayer("Default");

        if (esEscenario)
        {
            tiempoProximoChoque = Time.time + cooldownImpacto;

            // 1. Audio que respeta el Audio Mixer
            ReproducirAudioImpacto();

            // 2. Aplicar daño por choque al enemigo
            if (miSalud != null)
            {
                miSalud.TakeDamage(danioPorChoqueMuro);
            }

            // 3. Si sobrevivió al choque, reubicarlo de inmediato a un carril seguro
            if (gameObject.activeInHierarchy && (miSalud == null || miSalud.Current > 0))
            {
                if (!estaReubicandose)
                {
                    StartCoroutine(RutinaReubicacionSegura(col));
                }
            }
        }
    }

    private void ReproducirAudioImpacto()
    {
        if (sfxChoqueEscenario == null) return;

        if (audioSourceLocal != null)
        {
            audioSourceLocal.PlayOneShot(sfxChoqueEscenario, volumenChoque);
        }
        else
        {
            // Emisor temporal compatible con Mixer si el objeto no tuviera AudioSource
            GameObject tempAudio = new GameObject("TempAudioChoque");
            tempAudio.transform.position = transform.position;
            AudioSource src = tempAudio.AddComponent<AudioSource>();
            src.clip = sfxChoqueEscenario;
            src.volume = volumenChoque;
            src.spatialBlend = 0.8f;
            if (grupoMixer != null) src.outputAudioMixerGroup = grupoMixer;
            src.Play();
            Destroy(tempAudio, sfxChoqueEscenario.length);
        }
    }

    private IEnumerator RutinaReubicacionSegura(Collider obstaculo)
    {
        estaReubicandose = true;

        // 1. Dirección de escape inmediata alejándose de la superficie tocada
        Vector3 puntoCercano = (obstaculo != null) ? obstaculo.ClosestPoint(transform.position) : transform.position;
        Vector3 vectorEscape = (transform.position - puntoCercano).normalized;
        if (vectorEscape.sqrMagnitude < 0.01f) vectorEscape = Vector3.up;
        vectorEscape.z = 0f;

        // 2. Calcular nuevo cuadrante opuesto dentro del riel
        float nuevoX = (transform.localPosition.x > 0)
            ? Random.Range(-limitesAreaSegura.x * 0.7f, -1f)
            : Random.Range(1f, limitesAreaSegura.x * 0.7f);

        float nuevoY = (transform.localPosition.y > 0)
            ? Random.Range(-limitesAreaSegura.y * 0.6f, -1f)
            : Random.Range(1f, limitesAreaSegura.y * 0.6f);

        float tiempo = 0f;
        while (tiempo < duracionReubicacion)
        {
            if (miSalud != null && miSalud.Current <= 0) break;

            // Mantenerse en coordenadas locales consistentes
            Vector3 posLocal = transform.localPosition;

            // Empuje de rebote inmediato
            posLocal.x += vectorEscape.x * (fuerzaEvasion * 1.2f) * Time.deltaTime;
            posLocal.y += vectorEscape.y * (fuerzaEvasion * 1.2f) * Time.deltaTime;

            // Movimiento fluido hacia el carril seguro de la pantalla
            posLocal.x = Mathf.MoveTowards(posLocal.x, nuevoX, velocidadReubicacion * Time.deltaTime);
            posLocal.y = Mathf.MoveTowards(posLocal.y, nuevoY, velocidadReubicacion * Time.deltaTime);

            transform.localPosition = posLocal;

            tiempo += Time.deltaTime;
            yield return null;
        }

        estaReubicandose = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioSeparacion);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * distanciaSondeo);
    }
}