using System.Collections;
using UnityEngine;

public class EnemigoSeguidor : MonoBehaviour
{
    [Header("Referencias")] public Transform objetivo;
    public Transform boca;
    public Transform visual; // si la malla está en un hijo

    [Header("Movimiento")] public float distanciaSeguimiento = 6f;
    public float velocidadRespuesta = 5f;

    [Header("Apuntado")] public float velocidadRotacion = 6f;
    public float cadenciaDisparo = 1.5f;
    public GameObject prefabProyectil;
    public Vector3 proyectilRotationOffsetEuler = Vector3.zero;

    [Header("Ciclo de vida")] public float tiempoVida = 12f;
    public float velocidadRetirada = 8f;
    public float tiempoRetirada = 2f;

    [Header("Debug")] public bool debugVisuals = false;
    public bool debugUseLineRenderer = true;
    public float debugLength = 100f;

    // estado
    Vector3 posicionInicial;
    float timerVida;
    float temporizadorDisparo;
    bool estaRetirandose;

    // debug
    LineRenderer lrDeseada, lrReal;
    bool visualAutoAdjusted;

    void Start()
    {
        posicionInicial = transform.position;
        timerVida = tiempoVida;

        if (objetivo == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) objetivo = go.transform;
        }

        // Auto-ajuste si la boca apunta en dirección opuesta al objetivo
        if (!visualAutoAdjusted && objetivo != null)
        {
            Vector3 origen = (boca != null) ? boca.position : transform.position;
            Vector3 desired = (objetivo.position - origen);
            if (desired.sqrMagnitude > 0.0001f)
            {
                desired.Normalize();
                Vector3 real = (boca != null) ? boca.forward.normalized : transform.forward.normalized;
                if (Vector3.Dot(desired, real) < 0f)
                {
                    if (visual != null) visual.Rotate(0f, 180f, 0f, Space.Self);
                    else transform.Rotate(0f, 180f, 0f, Space.Self);
                    visualAutoAdjusted = true;
                    if (debugVisuals) Debug.Log($"Auto-ajuste aplicado al {(visual!=null?"visual":"root")}");
                }
            }
        }
    }

    void Update()
    {
        if (estaRetirandose || objetivo == null) return;
        MovimientoFijo();
        GestionarCombate();
        UpdateDebugVisuals();
        GestionarTiempoVida();
    }

    void MovimientoFijo()
    {
        var referencia = Camera.main != null ? Camera.main.transform : objetivo;
        if (referencia == null) return;
        Vector3 objetivoFrontal = referencia.position + referencia.forward * distanciaSeguimiento;
        Vector3 forwardH = Vector3.ProjectOnPlane(referencia.forward, Vector3.up);
        if (forwardH.sqrMagnitude < 0.0001f) forwardH = referencia.forward;
        forwardH.Normalize();
        Vector3 delta = objetivoFrontal - transform.position;
        Vector3 avance = Vector3.Project(delta, forwardH);
        Vector3 destino = transform.position + avance;
        transform.position = Vector3.Lerp(transform.position, destino, Time.deltaTime * velocidadRespuesta);
    }

    void GestionarCombate()
    {
        RotarHaciaObjetivo();
        temporizadorDisparo -= Time.deltaTime;
        if (temporizadorDisparo <= 0f)
        {
            Disparar();
            temporizadorDisparo = 1f / Mathf.Max(0.0001f, cadenciaDisparo);
        }
    }

    void RotarHaciaObjetivo()
    {
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Vector3 dirRaw = (objetivo.position - origen);
        if (dirRaw.sqrMagnitude < 0.0001f) return;
        Vector3 dir = dirRaw.normalized;
        Quaternion objetivoRot = Quaternion.LookRotation(dir);

        Quaternion nueva = Quaternion.RotateTowards((visual != null) ? visual.rotation : transform.rotation, objetivoRot, velocidadRotacion * Time.deltaTime);
        if (visual != null) visual.rotation = nueva; else transform.rotation = nueva;
    }

    void Disparar()
    {
        if (prefabProyectil == null) return;
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Quaternion rot = boca != null ? boca.rotation * Quaternion.Euler(proyectilRotationOffsetEuler) : Quaternion.LookRotation((objetivo!=null?(objetivo.position-origen).normalized:transform.forward)) * Quaternion.Euler(proyectilRotationOffsetEuler);
        var instancia = Instantiate(prefabProyectil, origen, rot);
        var proj = instancia.GetComponent<Proyectil>(); if (proj != null) proj.tagOrigen = "Enemy";
        var rb = instancia.GetComponent<Rigidbody>();
        float vel = (proj != null) ? proj.velocidad : 60f;
        if (rb != null) rb.linearVelocity = instancia.transform.forward * vel;
        if (debugVisuals) Debug.Log($"Disparar: origen={origen}, instancia.forward={instancia.transform.forward}, vel={vel}");
    }

    void UpdateDebugVisuals()
    {
        if (!debugVisuals)
        {
            if (lrDeseada != null) Destroy(lrDeseada.gameObject);
            if (lrReal != null) Destroy(lrReal.gameObject);
            lrDeseada = lrReal = null;
            return;
        }
        Vector3 origen = (boca != null) ? boca.position : transform.position;
        Vector3 desired = (objetivo!=null?(objetivo.position-origen).normalized:(boca!=null?boca.forward:transform.forward));
        Vector3 real = (boca!=null?boca.forward:transform.forward).normalized;
        Debug.DrawRay(origen, desired*debugLength, Color.cyan);
        Debug.DrawRay(origen, real*debugLength, Color.magenta);
        if (!debugUseLineRenderer) return;
        if (lrDeseada==null) lrDeseada = CreateLine("LR_Des", Color.cyan);
        if (lrReal==null) lrReal = CreateLine("LR_Real", Color.magenta);
        SetLine(lrDeseada, origen, origen+desired*debugLength);
        SetLine(lrReal, origen, origen+real*debugLength);
    }

    LineRenderer CreateLine(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2; lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = lr.endWidth = 0.15f; lr.startColor = lr.endColor = color;
        lr.numCapVertices = 8; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
        return lr;
    }

    void SetLine(LineRenderer lr, Vector3 a, Vector3 b) { if (lr==null) return; lr.SetPosition(0,a); lr.SetPosition(1,b); }

    void OnDisable() { if (lrDeseada!=null) Destroy(lrDeseada.gameObject); if (lrReal!=null) Destroy(lrReal.gameObject); lrDeseada = lrReal = null; }

    void OnDrawGizmosSelected()
    {
        if (boca!=null)
        {
            Gizmos.color = Color.yellow; Gizmos.DrawSphere(boca.position, 0.12f);
            Gizmos.color = Color.magenta; Gizmos.DrawLine(boca.position, boca.position + boca.forward*2f);
        }
        else
        {
            Gizmos.color = Color.yellow; Gizmos.DrawSphere(transform.position, 0.12f);
            Gizmos.color = Color.magenta; Gizmos.DrawLine(transform.position, transform.position + transform.forward*2f);
        }
        if (objetivo!=null)
        {
            Gizmos.color = Color.cyan; var o = (boca!=null)?boca.position:transform.position; Gizmos.DrawLine(o, objetivo.position);
        }
    }

    public Vector3 ObtenerDireccionDisparo() { if (boca!=null) return boca.forward.normalized; return (transform.rotation*Vector3.forward).normalized; }

    void GestionarTiempoVida() { timerVida -= Time.deltaTime; if (timerVida<=0f) StartCoroutine(RetirarCoroutine()); }

    IEnumerator RetirarCoroutine()
    {
        estaRetirandose = true;
        Vector3 dir = transform.right;
        if (objetivo!=null)
        {
            float dot = Vector3.Dot((objetivo.position-transform.position).normalized, transform.right);
            dir = (dot>0f)?-transform.right:transform.right;
        }
        float t=0f; while (t<tiempoRetirada) { transform.position += dir.normalized*velocidadRetirada*Time.deltaTime; t+=Time.deltaTime; yield return null; }
        Destroy(gameObject);
    }
}
