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
            saludJugador = GetComponentInParent<Health>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Punto Camara") ||
            other.GetComponentInParent<PuntoTrayectoriaCamara>() != null ||
            other.GetComponentInParent<ZonaSeguraOInstakill>() != null ||
            (other.isTrigger && other.GetComponentInParent<ContactDamage>() == null))
        {
            return;
        }

        Debug.Log($"[Choque Detectado] La nave tocó: {other.gameObject.name} (Tag: {other.tag})");
        ProcesarContacto(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcesarContacto(collision.gameObject);
    }

    private void ProcesarContacto(GameObject obstaculo)
    {
        // 1. Ignorar disparos propios, puntos de cámara y zonas especiales
        if (obstaculo.CompareTag("Player") || obstaculo.GetComponent<Proyectil>() != null) return;
        if (obstaculo.CompareTag("Punto Camara") ||
            obstaculo.GetComponentInParent<PuntoTrayectoriaCamara>() != null ||
            obstaculo.GetComponentInParent<ZonaSeguraOInstakill>() != null)
        {
            return;
        }

        var puerta = obstaculo.GetComponentInParent<PuertaContacto>();
        if (puerta != null)
        {
            puerta.ProcesarContacto(saludJugador);
            return;
        }

        if (Time.time < proximoChoquePermitido) return;

        // 2. Si el obstáculo tiene ContactDamage, delegamos en él
        var cd = obstaculo.GetComponentInParent<ContactDamage>();
        if (cd != null)
        {
            cd.Hit(transform.root.gameObject);
            proximoChoquePermitido = Time.time + cooldownEntreImpactos;
            return;
        }

        // 3. Si chocó contra un muro, suelo o asteroide genérico del escenario
        if (obstaculo.CompareTag("Escenario") || obstaculo.layer == LayerMask.NameToLayer("Default"))
        {
            proximoChoquePermitido = Time.time + cooldownEntreImpactos;

            // Aplicar daño
            if (saludJugador != null)
            {
                saludJugador.TakeDamage(danioDefecto);
            }

            // Empuje de rebote al centro del riel
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

        // Desplazamiento inmediato o impulso
        naveRoot.localPosition += direccionAlCentro * (fuerzaRebote * 0.1f);
    }
}