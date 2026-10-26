using UnityEngine;

public class Lanzador_Rayos : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Objetivo al que disparar (por defecto busca el tag 'Player')")]
    public Transform objetivo;

    [Tooltip("Punto exacto desde donde sale el proyectil")]
    public Transform puntoDisparo;

    [Tooltip("Prefab del proyectil que se va a disparar")]
    public GameObject prefabProyectil;

    [Header("Configuración de Apuntado")]
    [Tooltip("Distancia máxima de detección y disparo")]
    public float alcance = 20f;

    [Tooltip("Velocidad con la que rota hacia el objetivo")]
    public float velocidadRotacion = 10f;

    [Tooltip("Si es true, solo rota en el plano horizontal (eje Y)")]
    public bool rotarSoloY = true;

    [Tooltip("Dirección de avance del jugador en el mundo; se usa para dejar de disparar cuando ya pasó al lanzador")]
    public Vector3 direccionAvanceJugador = Vector3.forward;

    [Tooltip("Ajuste de rotación por si el modelo 3D del lanzador no mira hacia el Z positivo por defecto")]
    public Vector3 offsetRotacionEuler = Vector3.zero;

    [Header("Disparo")]
    [Tooltip("Disparos por segundo")]
    public float cadenciaDisparo = 1f;

    private float temporizadorDisparo = 0f;

    void Start()
    {
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
        }

        if (puntoDisparo == null)
        {
            puntoDisparo = transform;
        }
    }

    void Update()
    {
        if (objetivo == null) return;

        if (JugadorYaPaso())
        {
            temporizadorDisparo = 0f;
            return;
        }

        float distanciaAlObjetivo = Vector3.Distance(transform.position, objetivo.position);

        if (distanciaAlObjetivo <= alcance)
        {
            RotarHaciaObjetivo();

            temporizadorDisparo -= Time.deltaTime;
            if (temporizadorDisparo <= 0f)
            {
                Disparar();
                temporizadorDisparo = 1f / Mathf.Max(0.0001f, cadenciaDisparo);
            }
        }
    }

    bool JugadorYaPaso()
    {
        Vector3 direccionAvance = Vector3.ProjectOnPlane(direccionAvanceJugador, Vector3.up);
        if (direccionAvance.sqrMagnitude <= 0.0001f)
        {
            direccionAvance = Vector3.forward;
        }

        direccionAvance.Normalize();
        Vector3 haciaJugador = objetivo.position - transform.position;
        return Vector3.Dot(haciaJugador, direccionAvance) > 0f;
    }

    void RotarHaciaObjetivo()
    {
        Vector3 origen = (puntoDisparo != null) ? puntoDisparo.position : transform.position;
        Vector3 direccion = ObtenerDireccionObjetivo(origen);
        if (direccion.sqrMagnitude <= 0.0001f) return;

        Quaternion rotacionDeseada = Quaternion.LookRotation(direccion);
        rotacionDeseada *= Quaternion.Euler(offsetRotacionEuler);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionDeseada, velocidadRotacion * 50f * Time.deltaTime);
    }

    Vector3 ObtenerDireccionObjetivo(Vector3 origen)
    {
        Vector3 direccion = objetivo.position - origen;
        if (rotarSoloY)
        {
            direccion.y = 0f;
        }

        return direccion.sqrMagnitude > 0.0001f ? direccion.normalized : Vector3.zero;
    }

    void Disparar()
    {
        if (prefabProyectil == null) return;

        Vector3 origen = (puntoDisparo != null) ? puntoDisparo.position : transform.position;
        Vector3 direccion = ObtenerDireccionObjetivo(origen);
        if (direccion.sqrMagnitude <= 0.0001f) return;

        GameObject instancia = Instantiate(prefabProyectil, origen, Quaternion.LookRotation(direccion));

        ProyectilRayo proj = instancia.GetComponent<ProyectilRayo>();
        if (proj != null)
        {
            proj.tagOrigen = "Enemy";
            proj.origen = this.gameObject;
        }

        Rigidbody rbProj = instancia.GetComponent<Rigidbody>();
        if (rbProj != null)
        {
            float vel = (proj != null) ? proj.velocidad : 60f;
            rbProj.linearVelocity = direccion * vel;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcance);

        // Muestra el frente actual del lanzador en la vista de escena.
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }
}