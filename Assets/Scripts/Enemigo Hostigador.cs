using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo Seguidor:
/// 
/// se desplaza dinámicamente frente al jugador o frente a la cámara, le dispara mientras esté en modo de ataque,
/// y tras agotarse su tiempo de vida realiza una maniobra de retirada lateral antes de destruirse.
/// </summary>
public class EnemigoSeguidor : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Objetivo a seguir. Si está vacío, busca automáticamente por tag 'Player'")]
    public Transform objetivo;

    [Tooltip("Punto de salida del disparo")]
    public Transform boca;

    [Header("Movimiento y Posicionamiento")]
    [Tooltip("Distancia que busca mantener delante del jugador o de la cámara")]
    public float distanciaSeguimiento = 6f;


    [Header("Rotación y Apuntado")]
    public float velocidadRotacion = 6f;

    [Tooltip("Disparos por segundo")]
    public float cadenciaDisparo = 1.5f;

    public LayerMask mascaraObjetivo = ~0;
    public GameObject prefabProyectil;

    [Header("Ciclo de Vida y Retirada")]
    [Tooltip("Tiempo activo antes de iniciar la maniobra de escape")]
    public float tiempoVida = 12f;

    [Tooltip("Velocidad lateral durante la retirada")]
    public float velocidadRetirada = 8f;

    [Tooltip("Duración de la retirada lateral antes de destruir el objeto")]
    public float tiempoRetirada = 2f;

    private Vector3 posicionInicial;
    private Vector3 posicionDeseada;
    private bool tieneAggro = true;
    private float timerAggro = 0f;
    private float timerVida = 0f;
    private float temporizadorDisparo = 0f;
    private bool estaRetirandose = false;

    void Start()
    {
        posicionInicial = transform.position;
        timerVida = tiempoVida;

        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
        }
    }

    void Update()
    {
        if (estaRetirandose || objetivo == null) return;

        MovimientoFijo();
        GestionarCombate();
        GestionarTiempoVida();
    }



    /// <summary>
    /// Si tiene aggro, se posiciona delante de la cámara o del jugador; si lo pierde, regresa suavemente a su origen.
    /// </summary>
    
    void MovimientoFijo()
    {

        // Preferir el punto de vista de la cámara activa si existe, o el frente del jugador
        Transform puntoReferencia = Camera.main != null ? Camera.main.transform : objetivo;
        posicionDeseada = puntoReferencia.position + (puntoReferencia.forward * distanciaSeguimiento);

        transform.position = Vector3.Lerp(transform.position, posicionDeseada, Time.deltaTime /* * velocidadRespuesta*/ );

    }

    /// <summary>
    /// Rota apuntando al objetivo y abre fuego si está dentro de la distancia de ataque.
    /// </summary>
    void GestionarCombate()
    {
        RotarHaciaObjetivo();

        if (tieneAggro )
        {
            temporizadorDisparo -= Time.deltaTime;
            if (temporizadorDisparo <= 0f)
            {
                Disparar();
                temporizadorDisparo = 1f / Mathf.Max(0.0001f, cadenciaDisparo);
            }
        }
    }

    void RotarHaciaObjetivo()
    {
        Vector3 direccion = (objetivo.position - transform.position).normalized;

        Quaternion rotacion = Quaternion.LookRotation(direccion);

        transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, Time.deltaTime * velocidadRotacion);
    }

    void Disparar()
    {
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        string mensaje= boca ? "Disparando desde boca" : "Disparando desde posición del enemigo";
        Debug.Log(mensaje);
        Vector3 direccion = ObtenerDireccionDisparo();
      
        if (prefabProyectil != null)
        {
            Quaternion rot = Quaternion.LookRotation(direccion);
            GameObject instancia = Instantiate(prefabProyectil, origen, rot);

            Proyectil proj = instancia.GetComponent<Proyectil>();
            if (proj != null)
            {
                Debug.Log("Disparando proyectil desde EnemigoSeguidor hacia " + direccion);
                proj.tagOrigen = "Enemy";
            }
        }
    }

    /// <summary>
    /// Obtiene la dirección frontal real hacia donde apunta el cañón/nave,
    /// compensando el offset de rotación aplicado al modelo 3D.
    /// </summary>
    public Vector3 ObtenerDireccionDisparo()
    {
        Quaternion rotacionVisual = transform.rotation;
      
        Vector3 frente = rotacionVisual * Vector3.forward;

        return frente.normalized;
    }

    void GestionarTiempoVida()
    {
        timerVida -= Time.deltaTime;
        if (timerVida <= 0f)
        {
            StartCoroutine(RetirarCoroutine());
        }
    }

    /// <summary>
    /// Maniobra evasiva hacia un lateral antes de autodestruirse.
    /// </summary>
    IEnumerator RetirarCoroutine()
    {
        estaRetirandose = true;
        tieneAggro = false;
        // Decidir si retirarse a la izquierda o derecha alejándose del centro del objetivo
        Vector3 dir = transform.right;
        if (objetivo != null)
        {
            Vector3 haciaObjetivo = objetivo.position - transform.position;
            float dot = Vector3.Dot(haciaObjetivo.normalized, transform.right);
            dir = (dot > 0f) ? -transform.right : transform.right;
        }

        float transcurrido = 0f;
        while (transcurrido < tiempoRetirada)
        {
            transform.position += dir.normalized * velocidadRetirada * Time.deltaTime;
            transcurrido += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

   
}
