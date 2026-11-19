using System.Globalization;
using Pyramid.Levels;
using UnityEngine;

/// <summary>Pasadas diagonales sobre el riel: entrada, ataque, salida y regreso fuera de cámara.</summary>
[RequireComponent(typeof(Health))]
public sealed class EnemigoPasada : MonoBehaviour, ILevelEntityAdapter
{
    public enum EstadoPasada { Entrada, Ataque, Salida, Espera }

    [Header("Referencias")]
    [Tooltip("Se busca el jugador de la sesión o el objeto Player si queda vacío.")]
    public Transform objetivo;
    public Camera camara;
    public Transform boca;

    [Header("Activación")]
    [Min(1f)] public float distanciaActivacion = 150f;

    [Header("Trayectoria respecto al riel")]
    [Min(1f)] public float distanciaEntrada = 110f;
    [Tooltip("Se amplía automáticamente si hace falta para aparecer completamente fuera de cámara.")]
    [Min(1f)] public float lateralEntrada = 90f;
    public float alturaEntrada = 12f;
    [Tooltip("Distancia horizontal mínima al jugador durante el pase. La nave existente mide unos 25 m de ancho.")]
    [Min(1f)] public float separacionPaso = 20f;
    public float alturaPaso = 2f;
    [Min(1f)] public float distanciaSalida = 65f;
    [Tooltip("Velocidad de la pasada respecto al avance del jugador, en metros por segundo.")]
    [Min(1f)] public float velocidad = 65f;
    [Min(0f)] public float pausaEntrePasadas = 1.5f;
    public bool alternarLado = true;
    public bool entrarPorDerecha = true;

    [Header("Disparo durante la aproximación")]
    public GameObject prefabProyectil;
    [Min(0f)] public float cadenciaDisparo = 2f;
    [Min(0f)] public float alcance = 95f;
    [Tooltip("Deja de disparar antes de pasar junto al jugador.")]
    [Min(0f)] public float distanciaMinimaDisparo = 12f;
    [Range(0f, 89f)] public float anguloDisparo = 70f;
    [Min(0f)] public float danio = 1f;

    public EstadoPasada Estado { get; private set; } = EstadoPasada.Espera;
    public int PasadasIniciadas { get; private set; }

    Health salud;
    Renderer[] modelos;
    readonly Plane[] planosCamara = new Plane[6];
    Vector3 adelante, derecha, arriba, centroPasada, direccionPasada, posicionObjetivoAnterior;
    Transform objetivoAnterior;
    float radioVisual, espera, temporizadorDisparo, busqueda;
    int lado;
    bool iniciada;

    void Awake()
    {
        salud = GetComponent<Health>();
        modelos = GetComponentsInChildren<Renderer>(true);
    }

    void OnEnable()
    {
        iniciada = false;
        PasadasIniciadas = 0;
        Estado = EstadoPasada.Espera;
        espera = temporizadorDisparo = busqueda = 0f;
    }

    // Después del movimiento del jugador: también funciona cuando acelera o frena.
    void LateUpdate() => Avanzar(Time.deltaTime);

    void Avanzar(float deltaTime)
    {
        if (deltaTime <= 0f || (salud != null && salud.Current <= 0f)) return;
        if (GameManager.Instance != null && GameManager.Instance.ModoActual != ModoVuelo.Riel) return;
        BuscarReferencias(deltaTime);
        if (objetivo == null || !objetivo.gameObject.activeInHierarchy ||
            objetivo == transform || objetivo.IsChildOf(transform)) return;

        if (objetivoAnterior != objetivo)
        {
            objetivoAnterior = objetivo;
            iniciada = false;
        }
        if (!iniciada)
        {
            float radio = Mathf.Max(1f, distanciaActivacion);
            if ((objetivo.position - transform.position).sqrMagnitude > radio * radio) return;
            iniciada = IniciarPasada();
            return;
        }

        // Heredar sólo el avance del riel, nunca el desplazamiento de esquiva ni el roll del jugador.
        Vector3 avance = adelante * Vector3.Dot(objetivo.position - posicionObjetivoAnterior, adelante);
        posicionObjetivoAnterior = objetivo.position;
        centroPasada += avance;
        transform.position += avance;

        if (Estado == EstadoPasada.Espera)
        {
            // Una cámara que vuelve a mirar la nave no debe mostrar su reposicionamiento.
            if (EstaEnCamara()) return;
            espera -= deltaTime;
            if (espera <= 0f) IniciarPasada();
            return;
        }

        transform.position += direccionPasada * Mathf.Max(1f, velocidad) * deltaTime;
        temporizadorDisparo = Mathf.Max(0f, temporizadorDisparo - deltaTime);
        float profundidad = Vector3.Dot(transform.position - centroPasada, adelante);
        if (profundidad <= distanciaMinimaDisparo || Estado == EstadoPasada.Salida)
        {
            Estado = EstadoPasada.Salida;
            if (profundidad <= -Mathf.Max(1f, distanciaSalida) && !EstaEnCamara())
            {
                Estado = EstadoPasada.Espera;
                espera = Mathf.Max(0f, pausaEntrePasadas);
            }
            return;
        }

        Vector3 origen = boca != null ? boca.position : transform.position;
        Vector3 haciaObjetivo = objetivo.position - origen;
        bool puedeAtacar = haciaObjetivo.sqrMagnitude > 0.0001f &&
            haciaObjetivo.sqrMagnitude <= alcance * alcance &&
            Vector3.Dot(transform.position - objetivo.position, adelante) > distanciaMinimaDisparo &&
            Vector3.Dot(direccionPasada, haciaObjetivo.normalized) >= Mathf.Cos(anguloDisparo * Mathf.Deg2Rad) &&
            EstaEnCamara();
        Estado = puedeAtacar ? EstadoPasada.Ataque : EstadoPasada.Entrada;
        if (puedeAtacar && prefabProyectil != null && cadenciaDisparo > 0f && temporizadorDisparo <= 0f)
        {
            var disparo = Instantiate(prefabProyectil, origen, Quaternion.LookRotation(haciaObjetivo, arriba));
            if (disparo.TryGetComponent<Proyectil>(out var proyectil))
            {
                proyectil.origen = gameObject;
                proyectil.tagOrigen = "Enemy";
                proyectil.danio = danio;
            }
            if (LevelSession.Active != null) LevelSession.Active.Objects.Track(disparo);
            temporizadorDisparo = 1f / cadenciaDisparo;
        }
    }

    void BuscarReferencias(float deltaTime)
    {
        if (camara == null && LevelSession.Active != null) camara = LevelSession.Active.viewCamera;
        if (camara == null) camara = Camera.main;
        if (objetivo != null) return;
        busqueda -= deltaTime;
        if (busqueda > 0f) return;
        busqueda = 0.5f;
        if (LevelSession.Active != null) objetivo = LevelSession.Active.player;
        if (objetivo != null) return;
        var jugador = FindAnyObjectByType<Player_rieles>();
        if (jugador != null) objetivo = jugador.transform;
        else
        {
            var encontrado = GameObject.FindGameObjectWithTag("Player");
            if (encontrado != null) objetivo = encontrado.transform;
        }
    }

    bool IniciarPasada()
    {
        // Player_rieles orienta su raíz al apuntar. El recorrido, en cambio, conserva sus ejes.
        Transform riel = LevelSession.Active != null ? LevelSession.Active.transform : null;
        adelante = riel != null ? riel.forward : Vector3.forward;
        derecha = riel != null ? riel.right : Vector3.right;
        arriba = riel != null ? riel.up : Vector3.up;
        int siguienteLado = PasadasIniciadas == 0 ? (entrarPorDerecha ? 1 : -1) : (alternarLado ? -lado : lado);
        Vector3 centro = objetivo.position;
        float lateral = Mathf.Max(lateralEntrada, separacionPaso + 10f);
        Vector3 inicio = centro + adelante * Mathf.Max(distanciaEntrada, separacionPaso + 10f) + arriba * alturaEntrada;
        ActualizarRadioVisual();

        // Comprobar un volumen que contiene la nave completa, independientemente de su orientación.
        int intento = 0;
        while (PosicionEnCamara(inicio + derecha * (siguienteLado * lateral)) && intento++ < 24)
            lateral *= 1.3f;
        inicio += derecha * (siguienteLado * lateral);
        if (PosicionEnCamara(inicio)) return false;

        lado = siguienteLado;
        centroPasada = centro;
        posicionObjetivoAnterior = objetivo.position;
        direccionPasada = CalcularDireccion(inicio - centro, siguienteLado, adelante, arriba);
        transform.SetPositionAndRotation(inicio, Quaternion.LookRotation(direccionPasada, arriba));
        temporizadorDisparo = 0f;
        Estado = EstadoPasada.Entrada;
        PasadasIniciadas++;
        return true;
    }

    Vector3 CalcularDireccion(Vector3 entrada, int signo, Vector3 frente, Vector3 vertical)
    {
        Vector3 horizontal = Vector3.ProjectOnPlane(entrada, vertical);
        // Una tangente conserva la distancia mínima incluso si el encuadre amplía la entrada.
        // Cruza por delante y sale por el costado opuesto, manteniendo visible el ataque.
        float angulo = Mathf.Asin(Mathf.Clamp01(Mathf.Max(1f, separacionPaso) / horizontal.magnitude)) * Mathf.Rad2Deg;
        Vector3 rumbo = Quaternion.AngleAxis(signo * angulo, vertical) * -horizontal.normalized;
        float recorridoHastaJugador = Vector3.Dot(horizontal, frente) / Mathf.Max(0.001f, -Vector3.Dot(rumbo, frente));
        return (rumbo + vertical * ((alturaPaso - alturaEntrada) / recorridoHastaJugador)).normalized;
    }

    void ActualizarRadioVisual()
    {
        radioVisual = 1f;
        if (modelos == null) return;
        foreach (var modelo in modelos)
        {
            if (modelo == null) continue;
            Bounds limites = modelo.bounds;
            radioVisual = Mathf.Max(radioVisual, Vector3.Distance(transform.position, limites.center) + limites.extents.magnitude);
        }
    }

    bool PosicionEnCamara(Vector3 posicion)
    {
        if (camara == null) return false;
        GeometryUtility.CalculateFrustumPlanes(camara, planosCamara);
        return GeometryUtility.TestPlanesAABB(planosCamara, new Bounds(posicion, Vector3.one * (radioVisual * 2f)));
    }

    bool EstaEnCamara()
    {
        if (camara == null) return false;
        GeometryUtility.CalculateFrustumPlanes(camara, planosCamara);
        bool tieneModelo = false;
        if (modelos != null)
            foreach (var modelo in modelos)
            {
                if (modelo == null || !modelo.enabled || !modelo.gameObject.activeInHierarchy) continue;
                tieneModelo = true;
                if (GeometryUtility.TestPlanesAABB(planosCamara, modelo.bounds)) return true;
            }
        return !tieneModelo && GeometryUtility.TestPlanesAABB(planosCamara,
            new Bounds(transform.position, Vector3.one * (radioVisual * 2f)));
    }

    public bool SupportsParameter(string key) => key == "velocidad" || key == "cadencia" ||
        key == "dano" || key == "alcance" || key == "entrada" || key == "lateral" ||
        key == "activacion" || key == "separacion" || key == "salida" || key == "pausa" || key == "lado" || key == "alternar";

    public string ValidateSettings(EntitySettings settings)
    {
        if (settings.speed <= 0f) return "La velocidad de la pasada debe ser mayor que cero.";
        foreach (string key in new[] { "activacion", "entrada", "lateral", "separacion", "salida", "pausa" })
        {
            string texto = settings.Get(key);
            if (texto == null) continue;
            if (!float.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out float valor) ||
                float.IsNaN(valor) || float.IsInfinity(valor) || valor < 0f || (key != "pausa" && valor < 1f))
                return $"El parámetro '{key}' debe ser finito y mayor o igual a {(key == "pausa" ? 0 : 1)}.";
        }
        string ladoConfigurado = settings.Get("lado");
        if (ladoConfigurado != null && ladoConfigurado != "izquierda" && ladoConfigurado != "derecha")
            return "El lado de entrada debe ser 'izquierda' o 'derecha'.";
        string alternar = settings.Get("alternar");
        if (alternar != null && !bool.TryParse(alternar, out _)) return "Alternar debe ser true o false.";
        return null;
    }

    public void Configure(EntitySettings settings, Transform player, Camera camera)
    {
        enabled = false;
        objetivo = player;
        camara = camera;
        velocidad = settings.speed;
        cadenciaDisparo = settings.fireRate;
        danio = settings.damage;
        alcance = settings.range;
        distanciaActivacion = LeerNumero(settings, "activacion", distanciaActivacion);
        distanciaEntrada = LeerNumero(settings, "entrada", distanciaEntrada);
        lateralEntrada = LeerNumero(settings, "lateral", lateralEntrada);
        separacionPaso = LeerNumero(settings, "separacion", separacionPaso);
        distanciaSalida = LeerNumero(settings, "salida", distanciaSalida);
        pausaEntrePasadas = LeerNumero(settings, "pausa", pausaEntrePasadas);
        string ladoConfigurado = settings.Get("lado");
        if (ladoConfigurado != null) entrarPorDerecha = ladoConfigurado == "derecha";
        string alternar = settings.Get("alternar");
        if (alternar != null) alternarLado = bool.Parse(alternar);
    }

    static float LeerNumero(EntitySettings settings, string key, float actual) =>
        settings.Get(key) is string texto ? float.Parse(texto, CultureInfo.InvariantCulture) : actual;

    public void Begin() => enabled = true;
    public void End() => enabled = false;

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !iniciada)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(1f, distanciaActivacion));
        }
        Vector3 centro = Application.isPlaying && iniciada ? centroPasada : (objetivo != null ? objetivo.position : transform.position);
        Transform riel = LevelSession.Active != null ? LevelSession.Active.transform : null;
        Vector3 frente = riel != null ? riel.forward : Vector3.forward;
        Vector3 lateral = riel != null ? riel.right : Vector3.right;
        Vector3 vertical = riel != null ? riel.up : Vector3.up;
        int signo = Application.isPlaying && iniciada ? lado : (entrarPorDerecha ? 1 : -1);
        float profundidad = Mathf.Max(distanciaEntrada, separacionPaso + 10f);
        Vector3 inicio = centro + frente * profundidad + lateral * (signo * Mathf.Max(lateralEntrada, separacionPaso + 10f)) + vertical * alturaEntrada;
        Vector3 rumbo = CalcularDireccion(inicio - centro, signo, frente, vertical);
        Vector3 paso = inicio + rumbo * (profundidad / Mathf.Max(0.001f, -Vector3.Dot(rumbo, frente)));
        Vector3 salida = paso + rumbo * (distanciaSalida / Mathf.Max(0.001f, -Vector3.Dot(rumbo, frente)));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(inicio, paso);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(paso, 2f);
        Gizmos.DrawLine(paso, salida);
    }
}
