using System.Globalization;
using Pyramid.Levels;
using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class EnemigoPerseguidor : MonoBehaviour, ILevelEntityAdapter
{
    [Header("Objetivo")]
    public Transform objetivo;
    [Tooltip("Nombre único o ruta del objeto activo que seguirá si no hay una referencia asignada.")]
    public string nombreObjetivo = "ObjetivoNave";
    public Transform boca;

    [Header("Persecución en rieles")]
    [Tooltip("Distancia detrás del eje Z local del objetivo.")]
    [Min(0f)] public float distanciaSeguimiento = 35f;
    [Min(0f)] public float velocidad = 60f;
    [Min(0f)] public float velocidadRotacion = 180f;

    [Header("Disparo")]
    [Min(0f)] public float cadenciaDisparo = 1.5f;
    [Min(0f)] public float alcance = 200f;
    [Min(0f)] public float danio = 1f;
    public GameObject prefabProyectil;

    Health salud;
    float temporizadorDisparo;
    float proximaBusqueda;

    void Awake() => salud = GetComponent<Health>();

    void OnEnable()
    {
        temporizadorDisparo = 0f;
        proximaBusqueda = 0f;
    }

    void Update()
    {
        if (Time.deltaTime <= 0f || (salud != null && salud.Current <= 0f)) return;
        if (GameManager.Instance != null && GameManager.Instance.ModoActual != ModoVuelo.Riel) return;

        temporizadorDisparo = Mathf.Max(0f, temporizadorDisparo - Time.deltaTime);
        if (objetivo == null && Time.time >= proximaBusqueda)
        {
            proximaBusqueda = Time.time + 0.5f;
            if (!string.IsNullOrWhiteSpace(nombreObjetivo))
            {
                var encontrado = GameObject.Find(nombreObjetivo);
                if (encontrado != null) objetivo = encontrado.transform;
            }
        }
        if (objetivo == null || !objetivo.gameObject.activeInHierarchy ||
            objetivo == transform || objetivo.IsChildOf(transform)) return;

        Vector3 destino = objetivo.position - objetivo.forward * Mathf.Max(0f, distanciaSeguimiento);
        transform.position = Vector3.MoveTowards(transform.position, destino, Mathf.Max(0f, velocidad) * Time.deltaTime);

        Vector3 direccion = objetivo.position - transform.position;
        if (direccion.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direccion),
            Mathf.Max(0f, velocidadRotacion) * Time.deltaTime);

        Vector3 origen = boca != null ? boca.position : transform.position;
        Vector3 direccionDisparo = objetivo.position - origen;
        if (prefabProyectil == null || cadenciaDisparo <= 0f || temporizadorDisparo > 0f ||
            direccionDisparo.sqrMagnitude < 0.0001f || direccionDisparo.sqrMagnitude > alcance * alcance ||
            Vector3.Dot(transform.forward, direccion.normalized) < 0.95f) return;

        var disparo = Instantiate(prefabProyectil, origen, Quaternion.LookRotation(direccionDisparo));
        if (disparo.TryGetComponent<Proyectil>(out var proyectil))
        {
            proyectil.origen = gameObject;
            proyectil.tagOrigen = "Enemy";
            proyectil.danio = danio;
        }
        if (LevelSession.Active != null) LevelSession.Active.Objects.Track(disparo);
        temporizadorDisparo = 1f / cadenciaDisparo;
    }

    public bool SupportsParameter(string key) => key == "velocidad" || key == "cadencia" ||
        key == "dano" || key == "alcance" || key == "objetivo" || key == "distancia";

    public string ValidateSettings(EntitySettings settings)
    {
        string nombre = settings.Get("objetivo");
        if ((nombre != null || objetivo == null) && string.IsNullOrWhiteSpace(nombre ?? nombreObjetivo))
            return "La nave perseguidora necesita un objetivo o su nombre/ruta.";
        string distancia = settings.Get("distancia");
        if (distancia != null && (!float.TryParse(distancia, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor) ||
            float.IsNaN(valor) || float.IsInfinity(valor) || valor < 0f))
            return "La distancia de seguimiento debe ser un número finito mayor o igual a cero.";
        return null;
    }

    public void Configure(EntitySettings settings, Transform player, Camera camera)
    {
        enabled = false;
        velocidad = settings.speed;
        cadenciaDisparo = settings.fireRate;
        danio = settings.damage;
        alcance = settings.range;
        string nombre = settings.Get("objetivo");
        if (nombre != null)
        {
            nombreObjetivo = nombre.Trim();
            objetivo = null;
        }
        string distancia = settings.Get("distancia");
        if (distancia != null)
            distanciaSeguimiento = float.Parse(distancia, CultureInfo.InvariantCulture);
    }

    public void Begin() => enabled = true;
    public void End() => enabled = false;
}
