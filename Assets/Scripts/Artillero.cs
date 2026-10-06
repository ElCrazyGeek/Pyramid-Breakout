using UnityEngine;

/// <summary>
/// Artillero (Torreta Fija):
/// Permanece en su posición original, detecta si el jugador entra en su rango de alcance,
/// gira hacia él (opcionalmente solo en el eje Y) y dispara con una cadencia configurable.
/// </summary>
public class Artillero : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Objetivo a seguir. Si se deja vacío, busca automáticamente por tag 'Player'")]
    public Transform objetivo;

    [Tooltip("Parte del cuerpo (opcional, para referencia)")]
    public Transform parteCuerpo;

    [Tooltip("Cañón o boca de la torreta")]
    public Transform boca;

    [Tooltip("Punto de salida exacto del proyectil (punta del cañón). Si está vacío, usa la posición de boca")]
    public Transform puntoDisparo;

    [Header("Rotación y Apuntado")]
    [Tooltip("Velocidad de rotación para encarar al objetivo")]
    public float velocidadRotacion = 5f;

    [Tooltip("Si es true, la torreta gira en el plano horizontal (eje Y)")]
    public bool rotarSoloY = true;

    [Tooltip("Permite que la boca se incline verticalmente en su eje local si el modelo lo soporta")]
    public bool inclinarBocaVertical = false;

    [Tooltip("Límite mínimo de inclinación hacia abajo en grados (ej: -30)")]
    public float inclinacionMinima = -30f;

    [Tooltip("Límite máximo de inclinación hacia arriba en grados (ej: 60)")]
    public float inclinacionMaxima = 60f;

    [Tooltip("Ajuste de rotación (Euler) por si el modelo 3D no viene orientado hacia el frente (Z)")]
    public Vector3 rotacionOffsetEuler = Vector3.zero;

    [Tooltip("Invertir dirección de apuntado si el modelo está volteado 180°")]
    public bool invertirDireccion = false;

    [Header("Disparo")]
    [Tooltip("Distancia máxima a la que detecta al jugador y abre fuego")]
    public float alcance = 30f;

    [Tooltip("Cantidad de disparos por segundo")]
    public float cadenciaDisparo = 1f;

    [Tooltip("Si es true, el proyectil y el rayo se dirigen hacia la altura 3D del objetivo al disparar")]
    public bool apuntarDisparoAlObjetivo = true;

    [Tooltip("Capas que interceptan el raycast de línea de visión")]
    public LayerMask mascaraObjetivo = ~0;

    [Tooltip("Prefab del proyectil físico. Si es null, solo evaluará impacto por Raycast")]
    public GameObject prefabProyectil;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicialBoca;
    private float temporizadorDisparo = 0f;

    void Start()
    {
        posicionInicial = transform.position;

        if (boca != null)
        {
            rotacionInicialBoca = boca.localRotation;
        }

        // Búsqueda automática del jugador si no se asignó en el Inspector
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
        if (objetivo == null) return;

        // Asegurar que la raíz de la torreta mantenga su posición fija
        transform.position = posicionInicial;

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

    void RotarHaciaObjetivo()
    {
        // 1. ROTACIÓN HORIZONTAL (El cuerpo y la boca giran juntos en Y como un solo bloque)
        Vector3 dirHaciaObjetivo = objetivo.position - transform.position;
        Vector3 dirHorizontal = new Vector3(dirHaciaObjetivo.x, 0f, dirHaciaObjetivo.z);

        if (dirHorizontal.sqrMagnitude > 0.0001f)
        {
            Vector3 dirFinal = dirHorizontal.normalized;
            if (invertirDireccion) dirFinal = -dirFinal;

            Quaternion rotacionDeseada = Quaternion.LookRotation(dirFinal);

            if (rotacionOffsetEuler != Vector3.zero)
            {
                rotacionDeseada *= Quaternion.Euler(rotacionOffsetEuler);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * velocidadRotacion);
        }

        // 2. INCLINACIÓN VERTICAL DE LA BOCA (Solo si está habilitado explícitamente y en espacio local)
        if (boca != null && inclinarBocaVertical)
        {
            Vector3 dirLocal = transform.InverseTransformDirection(objetivo.position - boca.position);
            float distanciaPlana = new Vector2(dirLocal.x, dirLocal.z).magnitude;

            if (distanciaPlana > 0.001f)
            {
                float pitch = Mathf.Atan2(dirLocal.y, distanciaPlana) * Mathf.Rad2Deg;
                pitch = Mathf.Clamp(pitch, inclinacionMinima, inclinacionMaxima);

                Quaternion rotacionPitch = Quaternion.Euler(-pitch, 0f, 0f);
                boca.localRotation = Quaternion.Slerp(boca.localRotation, rotacionInicialBoca * rotacionPitch, Time.deltaTime * velocidadRotacion);
            }
        }
    }

    void Disparar()
    {
        Vector3 origen = (puntoDisparo != null) ? puntoDisparo.position : ((boca != null) ? boca.position : transform.position);
        Vector3 direccion = ObtenerDireccionDisparo(origen);

        // Raycast para feedback visual o impacto instantáneo
        RaycastHit golpe;
        bool impacto = Physics.Raycast(origen, direccion, out golpe, alcance, mascaraObjetivo);
        Debug.DrawRay(origen, direccion * alcance, impacto ? Color.green : Color.red, 0.5f);

        if (impacto)
        {
            Debug.Log($"Artillero: impacto en {golpe.collider.name} (tag={golpe.collider.tag})");
        }

        // Instanciar proyectil físico
        if (prefabProyectil != null)
        {
            Quaternion rot = Quaternion.LookRotation(direccion);
            GameObject instancia = Instantiate(prefabProyectil, origen, rot);

            Proyectil proj = instancia.GetComponent<Proyectil>();
            if (proj != null)
            {
                proj.tagOrigen = "Enemy";
            }
        }
    }

    /// <summary>
    /// Obtiene la dirección hacia donde saldrá el disparo.
    /// Si apuntarDisparoAlObjetivo es true, compensa la altura para darle al jugador.
    /// </summary>
    public Vector3 ObtenerDireccionDisparo(Vector3 origen)
    {
        if (apuntarDisparoAlObjetivo && objetivo != null)
        {
            return (objetivo.position - origen).normalized;
        }

        Transform fuente = (boca != null) ? boca : transform;
        Quaternion rotacionVisual = fuente.rotation;

        if (rotacionOffsetEuler != Vector3.zero)
        {
            rotacionVisual *= Quaternion.Inverse(Quaternion.Euler(rotacionOffsetEuler));
        }

        Vector3 frente = rotacionVisual * Vector3.forward;

        if (invertirDireccion)
        {
            frente = -frente;
        }

        return frente.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizar el rango de alcance en la vista de escena
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcance);
    }
}
