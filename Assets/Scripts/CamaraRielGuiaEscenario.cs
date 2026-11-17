using Unity.Cinemachine;
using UnityEngine;

public class CamaraRielGuiaEscenario : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineFollow follow;
    [SerializeField] private Transform jugador;

    [Header("Detección de Obstáculos en el Camino")]
    [SerializeField] private LayerMask capasEscenario; // Marca 'Default' y 'Escenario'
    [SerializeField] private float distanciaDeteccion = 25f;
    [SerializeField] private float aperturaSensoresX = 8f; // Qué tan ancho revisa a izquierda y derecha
    [SerializeField] private float desplazamientoMaximoX = 14f;
    [SerializeField] private float velocidadEsquive = 4f;

    private float offsetXBase;

    void Awake()
    {
        if (follow == null) follow = GetComponent<CinemachineFollow>();
        if (follow != null) offsetXBase = follow.FollowOffset.x;
    }

    void LateUpdate()
    {
        if (follow == null || jugador == null) return;

        Vector3 origen = jugador.position;
        float desvioObjetivoX = offsetXBase;

        // 1. Lanza un rayo al frente y dos a los costados para ver qué lado está libre
        bool bloqueoCentro = Physics.Raycast(origen, Vector3.forward, out RaycastHit hitCentro, distanciaDeteccion, capasEscenario);
        bool bloqueoIzq = Physics.Raycast(origen + Vector3.left * aperturaSensoresX, Vector3.forward, distanciaDeteccion, capasEscenario);
        bool bloqueoDer = Physics.Raycast(origen + Vector3.right * aperturaSensoresX, Vector3.forward, distanciaDeteccion, capasEscenario);

        // 2. Si hay un obstáculo enfrente o en un costado, empuja la cámara hacia el lado despejado
        if (bloqueoCentro || bloqueoIzq || bloqueoDer)
        {
            if (bloqueoIzq && !bloqueoDer)
                desvioObjetivoX = desplazamientoMaximoX; // Libre a la derecha
            else if (bloqueoDer && !bloqueoIzq)
                desvioObjetivoX = -desplazamientoMaximoX; // Libre a la izquierda
            else if (bloqueoCentro)
            {
                // Si el obstáculo está justo al frente, usa la normal de la pared para saber hacia dónde abrirse
                desvioObjetivoX = Mathf.Sign(hitCentro.normal.x) * desplazamientoMaximoX;
            }
        }

        // 3. Aplica el movimiento suave en X al FollowOffset de Cinemachine
        Vector3 offsetActual = follow.FollowOffset;
        offsetActual.x = Mathf.Lerp(offsetActual.x, desvioObjetivoX, velocidadEsquive * Time.deltaTime);
        follow.FollowOffset = offsetActual;
    }
}