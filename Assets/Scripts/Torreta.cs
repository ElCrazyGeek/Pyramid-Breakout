using System.Collections;
using UnityEngine;

public class Torreta : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetivo; // asignar el jugador, o dejar vacío para buscar por tag "Player"
    public Transform boca; // punto de salida del disparo

    [Header("Rotación")]
    public float velocidadRotacion = 5f; // velocidad de rotación
    public bool rotarSoloY = true; // si true solo rota sobre el eje Y

    [Header("Disparo")]
    public float cadenciaDisparo = 1f; // disparos por segundo
    public float alcance = 30f;
    public LayerMask mascaraObjetivo = ~0; // capas a considerar en el raycast
    public GameObject prefabProyectil; // opcional: prefab de proyectil físico

    public enum TipoTorreta { Estacionaria, Seguidora }
    public TipoTorreta tipo = TipoTorreta.Estacionaria;

    [Tooltip("Distancia que mantiene la torreta delante del jugador (solo Seguidora)")]
    public float followDistance = 5f;

    [Tooltip("Qué tan rápido responde la torreta seguidora (mayor = más rápida)")]
    public float followResponsiveness = 5f;

    
    [Tooltip("Ajuste de rotación (Euler) aplicado al apuntado y a los proyectiles")]
    public Vector3 rotacionOffsetEuler = Vector3.zero;
    [Tooltip("Invertir dirección de apuntado si el asset está volteado")]
    public bool invertirDireccion = false;

    private Vector3 initialPosition;
    private Vector3 fixedPosition;
    private Vector3 desiredPosition;
    private bool fixedPositionTaken = false;

    [Header("Tiempo y retirada")]
    public float tiempoVida = 10f; // tiempo antes de iniciar retirada
    public bool retirando = false;
    public float velocidadRetirada = 5f; // velocidad al moverse hacia un lado
    public float tiempoRetirada = 2f; // duración del movimiento lateral antes de destruir

    [Header("Aggro / detección")]
    public float detectionRadius = 15f; // radio para tomar aggro respecto al jugador
    public float perderAggroTiempo = 3f; // tiempo que permanece con aggro si el jugador sale del radio
    private bool tieneAggro = false;
    private float aggroTimer = 0f;

    private float vidaTimer = 0f;

    private float temporizadorDisparo = 0f;

    void Start()
    {
        if (objetivo == null)
        {
            var t = GameObject.FindGameObjectWithTag("Player");
            if (t) objetivo = t.transform;
        }
        vidaTimer = tiempoVida;
        initialPosition = transform.position;
    }
    void Update()
    {
        if (objetivo == null) return;

        // --- Comportamiento para torreta estacionaria ---
        if (tipo == TipoTorreta.Estacionaria)
        {
            // Mantener posición inicial (no sigue ni usa aggro ni retirada)
            transform.position = initialPosition;

            // Detección simple por alcance: si el jugador está dentro de alcance, rotar y disparar
            float distancia = Vector3.Distance(transform.position, objetivo.position);
            if (distancia <= alcance)
            {
                RotarHaciaObjetivo();

                temporizadorDisparo -= Time.deltaTime;
                if (temporizadorDisparo <= 0f)
                {
                    Disparar();
                    temporizadorDisparo = 1f / Mathf.Max(0.0001f, cadenciaDisparo);
                }
            }

            // No aplicar lógica de aggro ni retirada para torreta estacionaria
            return;
        }

        // --- Comportamiento para torreta seguidora ---
        // AGGRO: detectar y mantener temporizador
        float dPlayer = Vector3.Distance(transform.position, objetivo.position);
        if (dPlayer <= detectionRadius)
        {
            tieneAggro = true;
            aggroTimer = perderAggroTiempo;
        }
        else
        {
            if (tieneAggro)
            {
                aggroTimer -= Time.deltaTime;
                if (aggroTimer <= 0f) tieneAggro = false;
            }
        }

        // Movimiento seguidora: si tiene aggro, moverse suavemente hacia la posición delante de la cámara (o jugador)
        if (tieneAggro)
        {
            Transform fuente = Camera.main != null ? Camera.main.transform : objetivo;
            if (fuente != null)
                desiredPosition = fuente.position + fuente.forward * followDistance;
            else
                desiredPosition = transform.position;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followResponsiveness);

            // Rotar y disparar
            RotarHaciaObjetivo();
            float distanciaSeg = Vector3.Distance(transform.position, objetivo.position);
            if (distanciaSeg <= alcance)
            {
                temporizadorDisparo -= Time.deltaTime;
                if (temporizadorDisparo <= 0f)
                {
                    Disparar();
                    temporizadorDisparo = 1f / Mathf.Max(0.0001f, cadenciaDisparo);
                }
            }
        }
        else
        {
            // Sin aggro, la seguidora vuelve suavemente a la posición inicial
            transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * (followResponsiveness * 0.5f));
        }

        // Vida y retirada sólo aplican a torretas seguidoras
        if (!retirando)
        {
            vidaTimer -= Time.deltaTime;
            if (vidaTimer <= 0f)
            {
                StartCoroutine(RetirarCoroutine());
            }
        }
    }

    IEnumerator RetirarCoroutine()
    {
        retirando = true;

        // Elegir dirección lateral: preferir alejarse del jugador si existe
        Vector3 dir = transform.right;
        if (objetivo != null)
        {
            Vector3 toPlayer = objetivo.position - transform.position;
            float dot = Vector3.Dot(toPlayer.normalized, transform.right);
            // si dot>0 significa que el jugador está a la derecha, moverse a la izquierda
            dir = (dot > 0f) ? -transform.right : transform.right;
        }

        float t = 0f;
        while (t < tiempoRetirada)
        {
            transform.position += dir.normalized * velocidadRetirada * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    void RotarHaciaObjetivo()
    {
        Vector3 direccion = (objetivo.position - transform.position).normalized;
        if (rotarSoloY)
        {
            direccion.y = 0f; // ignorar diferencia vertical

            if (direccion.sqrMagnitude < 0.0001f) return;
        }

        Quaternion objetivoRot = Quaternion.LookRotation(direccion);
        transform.rotation = Quaternion.Slerp(transform.rotation, objetivoRot, Time.deltaTime * velocidadRotacion);
    }

    // Nota: el movimiento ahora usa Lerp simple controlado por followResponsiveness.

    void Disparar()
    {
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Vector3 direccion = (boca != null) ? boca.forward : (objetivo.position - origen).normalized;

        RaycastHit golpe;
        bool impacto = Physics.Raycast(origen, direccion, out golpe, alcance, mascaraObjetivo);
        Debug.DrawRay(origen, direccion * alcance, impacto ? Color.green : Color.red, 0.5f);
     
        if (impacto)
        {
            Debug.Log($"Torreta: impacto en {golpe.collider.name} (tag={golpe.collider.tag})");
            // Aquí puedes aplicar daño: golpe.collider.GetComponent<...>()?.RecibirDanio(...);
        }

        if (prefabProyectil != null)
        {
            Quaternion rot = Quaternion.LookRotation(direccion);
            var instancia = Instantiate(prefabProyectil, origen, rot);
            // Si el proyectil tiene el script Proyectil, informarle de su origen para evitar "friendly fire"
            var proj = instancia.GetComponent<Proyectil>();
            if (proj != null)
            {
                proj.tagOrigen = "Enemy"; // marcar como proyectil enemigo
            }
        }
    }
}
