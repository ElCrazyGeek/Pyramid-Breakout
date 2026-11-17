using Unity.Cinemachine;
using UnityEngine;

public class Camera_Rieles : MonoBehaviour
{
    public static Camera_Rieles Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private CinemachineFollow follow;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Vector3 offsetBase = new Vector3(0f, 0.2f, -30f);

    [Header("Margen Camara (Modo Rieles Normal)")]
    [SerializeField] private float margenX = 28f;
    [SerializeField] private float margenY = 23f;
    [SerializeField] private float seguimientoSuavidad = 5f;

    [Header("Rotación de Cámara (Siempre vuelve a Y = 0)")]
    [SerializeField] private float pitchFijoX = 0.38f;
    [SerializeField] private float velocidadRotacionY = 4f;

    [Header("Esquive Automático (Opcional)")]
    [SerializeField] private bool activarGuiaEscenario = true;
    [SerializeField] private LayerMask capasEscenario;
    [SerializeField] private float distanciaDeteccion = 25f;
    [SerializeField] private float aperturaSensoresX = 8f;
    [SerializeField] private float desplazamientoMaximoX = 14f;
    [SerializeField] private float velocidadEsquive = 4f;

    private float fixedX;
    private float fixedY;
    private float desvioEscenarioActualX;

    // Control de puntos/zonas de trayectoria
    private bool enPuntoTrayectoria = false;
    private float puntoObjetivoX;
    private float puntoObjetivoZ;
    private float rotacionObjetivoY = 0f;
    private float velocidadPunto = 4f;
    private bool regresarAlRebasarZ = false;

    void Awake()
    {
        Instance = this;
        if (follow == null)
            follow = GetComponent<CinemachineFollow>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        ReiniciarCentro();
    }

    public void ReiniciarCentro()
    {
        if (playerTarget != null)
        {
            fixedX = playerTarget.position.x + offsetBase.x;
            fixedY = playerTarget.position.y + offsetBase.y;
        }
        enPuntoTrayectoria = false;
        rotacionObjetivoY = 0f;
        desvioEscenarioActualX = 0f;
    }

    void LateUpdate()
    {
        if (playerTarget == null || follow == null) return;

        // 1. MODO PUNTO / TRAYECTORIA
        if (enPuntoTrayectoria)
        {
            if (regresarAlRebasarZ && playerTarget.position.z >= puntoObjetivoZ)
            {
                SalirDePuntoTrayectoria();
            }
            else
            {
                fixedX = Mathf.Lerp(fixedX, puntoObjetivoX, velocidadPunto * Time.deltaTime);

                float deltaY = playerTarget.position.y - fixedY;
                float excesoY = Mathf.Max(0f, Mathf.Abs(deltaY) - margenY) * Mathf.Sign(deltaY);
                fixedY = Mathf.Lerp(fixedY, fixedY + excesoY, seguimientoSuavidad * Time.deltaTime);

                desvioEscenarioActualX = Mathf.Lerp(desvioEscenarioActualX, 0f, velocidadPunto * Time.deltaTime);
            }
        }

        // 2. MODO RIELES NORMAL (Si no está en punto o acaba de salir en este frame)
        if (!enPuntoTrayectoria)
        {
            rotacionObjetivoY = 0f;

            float deltaX = playerTarget.position.x - fixedX;
            float deltaY = playerTarget.position.y - fixedY;

            float excesoX = Mathf.Max(0f, Mathf.Abs(deltaX) - margenX) * Mathf.Sign(deltaX);
            float excesoY = Mathf.Max(0f, Mathf.Abs(deltaY) - margenY) * Mathf.Sign(deltaY);

            fixedX = Mathf.Lerp(fixedX, fixedX + excesoX, seguimientoSuavidad * Time.deltaTime);
            fixedY = Mathf.Lerp(fixedY, fixedY + excesoY, seguimientoSuavidad * Time.deltaTime);

            float desvioObjetivoX = 0f;
            if (activarGuiaEscenario)
            {
                Vector3 origen = playerTarget.position;
                bool bloqueoCentro = Physics.Raycast(origen, Vector3.forward, out RaycastHit hitCentro, distanciaDeteccion, capasEscenario);
                bool bloqueoIzq = Physics.Raycast(origen + Vector3.left * aperturaSensoresX, Vector3.forward, distanciaDeteccion, capasEscenario);
                bool bloqueoDer = Physics.Raycast(origen + Vector3.right * aperturaSensoresX, Vector3.forward, distanciaDeteccion, capasEscenario);

                if (bloqueoCentro || bloqueoIzq || bloqueoDer)
                {
                    if (bloqueoIzq && !bloqueoDer) desvioObjetivoX = desplazamientoMaximoX;
                    else if (bloqueoDer && !bloqueoIzq) desvioObjetivoX = -desplazamientoMaximoX;
                    else if (bloqueoCentro) desvioObjetivoX = Mathf.Sign(hitCentro.normal.x) * desplazamientoMaximoX;
                }
            }

            desvioEscenarioActualX = Mathf.Lerp(desvioEscenarioActualX, desvioObjetivoX, velocidadEsquive * Time.deltaTime);
        }

        // 3. APLICAR POSICIÓN EN FOLLOW OFFSET
        float offsetRelativoX = (fixedX - playerTarget.position.x) + desvioEscenarioActualX;
        float offsetRelativoY = (fixedY - playerTarget.position.y);
        follow.FollowOffset = new Vector3(offsetRelativoX, offsetRelativoY, offsetBase.z);

        // 4. APLICAR ROTACIÓN EN Y (Siempre regresa a 0° fuera de puntos)
        Quaternion rotacionDeseada = Quaternion.Euler(pitchFijoX, rotacionObjetivoY, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadRotacionY * Time.deltaTime);
    }

    public void IrAPuntoTrayectoria(float destinoMundialX, float destinoMundialZ, float anguloRotacionY, float velocidad = 4f, bool autoRegresarEnZ = true)
    {
        enPuntoTrayectoria = true;
        puntoObjetivoX = destinoMundialX;
        puntoObjetivoZ = destinoMundialZ;
        rotacionObjetivoY = anguloRotacionY;
        velocidadPunto = velocidad;
        regresarAlRebasarZ = autoRegresarEnZ;
    }

    public void SalirDePuntoTrayectoria()
    {
        enPuntoTrayectoria = false;
        rotacionObjetivoY = 0f;
    }
}