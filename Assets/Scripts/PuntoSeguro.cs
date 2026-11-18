using System.Collections;
using UnityEngine;
using Pyramid.Levels;

[RequireComponent(typeof(Collider))]
public class ZonaSeguraOInstakill : MonoBehaviour
{
    public enum TipoZona
    {
        TransportarAPuntoSeguro,
        Instakill
    }

    [Header("Comportamiento de la Zona")]
    [SerializeField] private TipoZona modo = TipoZona.TransportarAPuntoSeguro;
    [SerializeField] private float danioAlEntrar = 25f;

    [Header("Configuración de Punto Seguro (Hijo dentro del Prefab)")]
    [Tooltip("Arrastra aquí un GameObject vacío hijo colocado en el centro despejado del pasillo.")]
    [SerializeField] private Transform puntoSeguroDestino;
    [Tooltip("Tiempo en segundos que tarda en deslizar la nave hacia el punto seguro (0 = teletransporte inmediato).")]
    [SerializeField] private float duracionRescate = 0.25f;
    [SerializeField] private float cooldownRescate = 0.8f;

    private float proximoUsoPermitido;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcesarJugador(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        // Respaldo por si la nave quedó atorada dentro del volumen
        ProcesarJugador(other.gameObject);
    }

    private void ProcesarJugador(GameObject obj)
    {
        if (Time.time < proximoUsoPermitido) return;

        Transform rootJugador = ObtenerRootJugador(obj);
        if (rootJugador == null) return;

        proximoUsoPermitido = Time.time + cooldownRescate;

        var damageable = rootJugador.GetComponentInChildren<IDamageable>() ?? rootJugador.GetComponentInParent<IDamageable>();

        // --- OPCIÓN 1: INSTAKILL ---
        if (modo == TipoZona.Instakill)
        {
            damageable?.TakeDamage(99999f);
            return;
        }

        // --- OPCIÓN 2: DAÑO + MOVER AL PUNTO SEGURO ---
        if (danioAlEntrar > 0f)
        {
            damageable?.TakeDamage(danioAlEntrar);
        }

        if (puntoSeguroDestino != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(MoverAPuntoSeguro(rootJugador));
        }
    }

    private IEnumerator MoverAPuntoSeguro(Transform nave)
    {
        CharacterController cc = nave.GetComponent<CharacterController>();

        // Desactivamos temporalmente el CharacterController para que no choque contra las paredes al salir del hueco
        if (cc != null) cc.enabled = false;

        Vector3 inicio = nave.position;
        Vector3 destino = new Vector3(puntoSeguroDestino.position.x, puntoSeguroDestino.position.y, nave.position.z);

        if (duracionRescate <= 0f)
        {
            nave.position = destino;
        }
        else
        {
            float t = 0f;
            while (t < duracionRescate && nave != null)
            {
                t += Time.deltaTime;
                float factor = Mathf.SmoothStep(0f, 1f, t / duracionRescate);
                // Mantenemos el avance actual en Z de la nave mientras corregimos X e Y hacia el punto seguro
                nave.position = new Vector3(
                    Mathf.Lerp(inicio.x, destino.x, factor),
                    Mathf.Lerp(inicio.y, destino.y, factor),
                    nave.position.z
                );
                yield return null;
            }
        }

        if (cc != null && nave != null) cc.enabled = true;
    }

    private Transform ObtenerRootJugador(GameObject obj)
    {
        if (obj.CompareTag("Player")) return obj.transform;

        var parent = obj.GetComponentInParent<IDamageable>() as Component;
        if (parent != null && parent.CompareTag("Player")) return parent.transform;

        if (LevelSession.Active != null && LevelSession.Active.player != null)
        {
            if (obj.transform == LevelSession.Active.player || obj.transform.IsChildOf(LevelSession.Active.player))
                return LevelSession.Active.player;
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = (modo == TipoZona.Instakill) ? new Color(1f, 0f, 0f, 0.35f) : new Color(0f, 1f, 0.5f, 0.35f);
        if (TryGetComponent<BoxCollider>(out var box))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }

        if (modo == TipoZona.TransportarAPuntoSeguro && puntoSeguroDestino != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, puntoSeguroDestino.position);
            Gizmos.DrawWireSphere(puntoSeguroDestino.position, 1.5f);
        }
    }
}