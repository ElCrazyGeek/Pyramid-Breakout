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
    public Sprite fondoEscena;             // Ilustración/render de fondo. Deja vacío si quieres ver el 3D detrás.

    [Header("Personajes en esta Toma")]
    [Tooltip("Solo los personajes listados aquí aparecerán; los demás se borran/apagan automáticamente.")]
    public PersonajeEnToma[] personajesPresentes;

    [Header("Caja de Diálogo")]
    public string nombreAMostrar;          // "Lyra", "Orión", etc.
    [TextArea(2, 4)]
    public string textoDialogo;

    [Header("Tiempo")]
    [Tooltip("0 = Espera Espacio/Clic. Mayor a 0 = Pasa de toma automáticamente tras estos segundos.")]
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
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoDialogo;

    private TomaCinematica[] tomasActuales;
    private int indiceToma = 0;
    private bool enCinematica = false;
    private Action alTerminarCallback;
    private Coroutine rutinaAutoAvance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inicializar pool de actores
        if (actoresPool != null)
        {
            foreach (var actor in actoresPool)
            {
                if (actor.imagenUI != null)
                {
                    actor.animacion = actor.imagenUI.GetComponent<RetratoAnimadoUI>();
                    actor.imagenUI.gameObject.SetActive(false); // Arrancan apagados
                }
            }
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

        MostrarTomaActual();
    }

    private void Update()
    {
        if (!enCinematica) return;

        // Salto manual de toma
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
            AvanzarToma();
        }
    }

    private void MostrarTomaActual()
    {
        if (tomasActuales == null || indiceToma >= tomasActuales.Length) return;

        TomaCinematica toma = tomasActuales[indiceToma];

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

        // 3. LIMPIAR: Apaga a todos los actores para no dejar remanentes del plano anterior
        LimpiarTodosLosActores();

        // 4. COLOCAR: Encender únicamente a los personajes de esta toma
        if (toma.personajesPresentes != null)
        {
            foreach (var p in toma.personajesPresentes)
            {
                ActorSlot slot = BuscarActorPool(p.idActor);
                if (slot == null || slot.imagenUI == null) continue;

                slot.imagenUI.gameObject.SetActive(true);

                if (p.spritePose != null) slot.imagenUI.sprite = p.spritePose;

                // Ubicar en el slot designado si fue asignado
                if (p.slotUbicacion != null)
                {
                    slot.imagenUI.rectTransform.position = p.slotUbicacion.position;
                }

                // Opacidad / Brillo según si habla
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

        // 5. Temporizador automático
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        if (toma.tiempoEnPantalla > 0f)
        {
            rutinaAutoAvance = StartCoroutine(TemporizadorToma(toma.tiempoEnPantalla));
        }
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

    private IEnumerator TemporizadorToma(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        AvanzarToma();
    }

    private void AvanzarToma()
    {
        indiceToma++;
        if (indiceToma < tomasActuales.Length) MostrarTomaActual();
        else CerrarCinematica();
    }

    private void CerrarCinematica()
    {
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        enCinematica = false;
        LimpiarTodosLosActores();
        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);

        Time.timeScale = 1f;
        alTerminarCallback?.Invoke();
    }
}