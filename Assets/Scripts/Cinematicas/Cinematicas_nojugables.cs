using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// --- ESTRUCTURAS SERIALIZABLES DEL SISTEMA DE TOMAS ---

[System.Serializable]
public class ActorSlot
{
    public string idActor;                 // "Lyra", "Orion", "Tercero"
    public Image imagenUI;                 // Componente Image dentro del Canvas
    [HideInInspector] public RetratoAnimadoUI animacion;
}

[System.Serializable]
public struct PersonajeEnToma
{
    public string idActor;                 // Debe coincidir con el idActor configurado en el pool
    public Sprite spritePose;              // PNG de la pose/expresión para este plano
    public RectTransform slotUbicacion;    // Objeto vacío en el Canvas donde debe pararse
    public bool estaHablando;              // TRUE = 100% luz (blanco). FALSE = 40% atenuado (gris).
}

[System.Serializable]
public struct TomaCinematica
{
    [Header("Encuadre / Fondo")]
    public Sprite fondoEscena;

    [Header("Transiciones y Fade")]
    [Tooltip("Aparecer gradualmente desde negro al entrar a esta toma (0 a 100% opacidad).")]
    public bool fadeEntrada;
    [Tooltip("Fundirse a negro antes de pasar a la siguiente toma.")]
    public bool fadeSalida;
    [Tooltip("Duración de la transición en segundos (ej. 0.5 o 1.0).")]
    public float duracionFade;

    [Header("Personajes en esta Toma")]
    public PersonajeEnToma[] personajesPresentes;

    [Header("Caja de Diálogo")]
    public string nombreAMostrar;
    [TextArea(2, 4)]
    public string textoDialogo;

    [Header("Tiempo")]
    public float tiempoEnPantalla;
}

// --- GESTOR DE CINEMÁTICAS ---

public class Cinematicas_nojugables : MonoBehaviour
{
    public static Cinematicas_nojugables Instance { get; private set; }

    [Header("Pool de Actores en UI")]
    [SerializeField] private ActorSlot[] actoresPool;

    [Header("Paneles de UI")]
    [SerializeField] private GameObject panelVisualNovel;
    [SerializeField] private Image imagenFondo;
    [SerializeField] private CanvasGroup cortinaNegro;
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoDialogo;

    private TomaCinematica[] tomasActuales;
    private int indiceToma = 0;
    private bool enCinematica = false;
    private bool enTransicion = false;
    private Action alTerminarCallback;
    private Coroutine rutinaAutoAvance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LimpiarTodosLosActores();

        if (cortinaNegro != null)
        {
            cortinaNegro.alpha = 0f;
            cortinaNegro.gameObject.SetActive(true);
        }

        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);
    }

    public void IniciarCinematicaVN(TomaCinematica[] tomas, Action alTerminar = null)
    {
        if (tomas == null || tomas.Length == 0) return;

        tomasActuales = tomas;
        indiceToma = 0;
        alTerminarCallback = alTerminar;
        enCinematica = true;

        Time.timeScale = 0f;
        if (panelVisualNovel != null) panelVisualNovel.SetActive(true);

        StartCoroutine(ProcesarEntradaToma());
    }

    private void Update()
    {
        if (!enCinematica || enTransicion) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
            StartCoroutine(AvanzarTomaConFade());
        }
    }

    private IEnumerator ProcesarEntradaToma()
    {
        enTransicion = true;
        TomaCinematica toma = tomasActuales[indiceToma];
        float duracion = toma.duracionFade > 0f ? toma.duracionFade : 0.5f;

        // Si la toma pide entrar con Fade In (de negro 100% a visible 0%)
        if (toma.fadeEntrada && cortinaNegro != null)
        {
            cortinaNegro.alpha = 1f; // Pantalla negra primero
            MontarVisualesToma(toma); // Carga la imagen mientras está en negro

            // Desvanece el negro de 1 a 0
            while (cortinaNegro.alpha > 0f)
            {
                cortinaNegro.alpha = Mathf.MoveTowards(cortinaNegro.alpha, 0f, (1f / duracion) * Time.unscaledDeltaTime);
                yield return null;
            }
        }
        else
        {
            if (cortinaNegro != null) cortinaNegro.alpha = 0f;
            MontarVisualesToma(toma);
        }

        enTransicion = false;

        // Iniciar temporizador automático si fue configurado
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        if (toma.tiempoEnPantalla > 0f)
        {
            rutinaAutoAvance = StartCoroutine(TemporizadorToma(toma.tiempoEnPantalla));
        }
    }

    private void MontarVisualesToma(TomaCinematica toma)
    {
        // 1. Textos
        if (textoNombre != null) textoNombre.text = toma.nombreAMostrar;
        if (textoDialogo != null) textoDialogo.text = toma.textoDialogo;

        // 2. Fondo
        if (imagenFondo != null)
        {
            if (toma.fondoEscena != null)
            {
                imagenFondo.gameObject.SetActive(true);
                imagenFondo.sprite = toma.fondoEscena;
            }
            else
            {
                imagenFondo.gameObject.SetActive(false);
            }
        }

        // 3. Limpiar actores de la toma previa
        LimpiarTodosLosActores();

        // 4. Ubicar e iluminar únicamente a los actores presentes
        if (toma.personajesPresentes != null)
        {
            foreach (var p in toma.personajesPresentes)
            {
                ActorSlot slot = BuscarActorPool(p.idActor);
                if (slot == null || slot.imagenUI == null) continue;

                slot.imagenUI.gameObject.SetActive(true);
                if (p.spritePose != null) slot.imagenUI.sprite = p.spritePose;

                if (p.slotUbicacion != null)
                {
                    slot.imagenUI.rectTransform.position = p.slotUbicacion.position;
                }

                if (p.estaHablando)
                {
                    slot.imagenUI.color = Color.white;
                    if (slot.animacion != null) slot.animacion.PonerEnPrimerPlano(p.spritePose);
                }
                else
                {
                    slot.imagenUI.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                    if (slot.animacion != null) slot.animacion.Atenuar();
                }
            }
        }
    }

    private IEnumerator AvanzarTomaConFade()
    {
        enTransicion = true;
        TomaCinematica tomaActual = tomasActuales[indiceToma];
        float duracion = tomaActual.duracionFade > 0f ? tomaActual.duracionFade : 0.5f;

        // Si la toma actual pide salir a negro antes del siguiente plano
        if (tomaActual.fadeSalida && cortinaNegro != null)
        {
            while (cortinaNegro.alpha < 1f)
            {
                cortinaNegro.alpha = Mathf.MoveTowards(cortinaNegro.alpha, 1f, (1f / duracion) * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        indiceToma++;

        if (indiceToma < tomasActuales.Length)
        {
            StartCoroutine(ProcesarEntradaToma());
        }
        else
        {
            CerrarCinematica();
        }
    }

    private IEnumerator TemporizadorToma(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        StartCoroutine(AvanzarTomaConFade());
    }

    private void LimpiarTodosLosActores()
    {
        if (actoresPool == null) return;
        foreach (var actor in actoresPool)
        {
            if (actor.imagenUI != null) actor.imagenUI.gameObject.SetActive(false);
        }
    }

    private ActorSlot BuscarActorPool(string id)
    {
        if (actoresPool == null) return null;
        foreach (var actor in actoresPool)
        {
            if (actor.idActor.Equals(id, StringComparison.OrdinalIgnoreCase)) return actor;
        }
        return null;
    }

    private void CerrarCinematica()
    {
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        enCinematica = false;
        enTransicion = false;
        LimpiarTodosLosActores();
        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);

        Time.timeScale = 1f;
        alTerminarCallback?.Invoke();
    }
}