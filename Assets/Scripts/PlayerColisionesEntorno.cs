using UnityEngine;
using Pyramid.Levels;

[RequireComponent(typeof(SphereCollider))]
public class PlayerColisionesEntorno : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Health saludJugador;

    [Header("Ajustes de Choque")]
    [SerializeField] private float cooldownEntreImpactos = 0.5f;
    [SerializeField] private float danioDefecto = 10f;
    [SerializeField] private float fuerzaRebote = 15f;

    private float proximoChoquePermitido;
    private SphereCollider col;

    void Awake()
    {
        col = GetComponent<SphereCollider>();
        col.isTrigger = true; // Actúa como sensor continuo

        if (saludJugador == null)
            saludJugador = GetComponentInParent<Health>() ?? transform.root.GetComponentInChildren<Health>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Punto Camara") ||
            other.GetComponentInParent<PuntoTrayectoriaCamara>() != null ||
            other.GetComponentInParent<ZonaSeguraOInstakill>() != null ||
            other.GetComponentInParent<TriggerTutorial>() != null ||
            (other.isTrigger && other.GetComponentInParent<ContactDamage>() == null && other.GetComponentInParent<PuertaContacto>() == null))
        {
            return;
        }

        ProcesarContacto(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcesarContacto(collision.gameObject);
    }

    private void ProcesarContacto(GameObject obstaculo)
    {
        // 1. Ignorar disparos propios, piezas de la nave, puntos de cámara y zonas especiales
        if (obstaculo.transform.IsChildOf(transform.root) || obstaculo.CompareTag("Player")) return;
        if (obstaculo.GetComponent<Proyectil>() != null || obstaculo.GetComponentInParent<Proyectil>() != null) return;

        if (obstaculo.CompareTag("Punto Camara") ||
            obstaculo.GetComponentInParent<PuntoTrayectoriaCamara>() != null ||
            obstaculo.GetComponentInParent<ZonaSeguraOInstakill>() != null ||
            obstaculo.GetComponentInParent<TriggerTutorial>() != null)
        {
            return;
        }

        // 2. Si es una puerta, delegamos en ella (daña sin frenar si está abierta; mata si está cerrada)
        var puerta = obstaculo.GetComponentInParent<PuertaContacto>();
        if (puerta != null)
        {
            puerta.ProcesarContacto(saludJugador);
            return;
        }

        if (Time.time < proximoChoquePermitido) return;

        // 3. Si el obstáculo tiene ContactDamage, delegamos en él
        var cd = obstaculo.GetComponentInParent<ContactDamage>();
        if (cd != null)
        {
            cd.Hit(transform.root.gameObject);
            proximoChoquePermitido = Time.time + cooldownEntreImpactos;
            return;
        }

        // 4. Si chocó contra un muro, suelo o asteroide genérico del escenario
        if (obstaculo.CompareTag("Escenario") || obstaculo.layer == LayerMask.NameToLayer("Default"))
        {
            proximoChoquePermitido = Time.time + cooldownEntreImpactos;

            if (saludJugador != null)
            {
                saludJugador.TakeDamage(danioDefecto);
            }

            RebotarAlCentro();
        }
    }

    private void RebotarAlCentro()
    {
        Transform naveRoot = transform.root;
        Vector3 posLocal = naveRoot.localPosition;

        // Dirección contraria hacia el centro (0, 0)
        Vector3 direccionAlCentro = new Vector3(-posLocal.x, -posLocal.y, 0f).normalized;
        if (direccionAlCentro.sqrMagnitude < 0.001f) direccionAlCentro = Vector3.up;

        // Desplazamiento inmediato
        naveRoot.localPosition += direccionAlCentro * (fuerzaRebote * 0.1f);
    }
}