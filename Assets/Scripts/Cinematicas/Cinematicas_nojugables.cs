using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// --- ESTRUCTURAS SERIALIZABLES GLOBALES ---

[System.Serializable]
public class ActorSlot
{
    public string idActor;                 // "Lyra", "Orion", "Tercero"
    public Image imagenUI;
    [HideInInspector] public RetratoAnimadoUI animacion;
}

[System.Serializable]
public struct PersonajeEnToma
{
    public string idActor;
    public Sprite spritePose;
    public RectTransform slotUbicacion;
    public bool estaHablando;
}

public enum TipoTransicionEntrada
{
    Directo,
    DisolverSobreJuego,
    FundidoNegro
}

[System.Serializable]
public struct TomaCinematica
{
    [Header("Encuadre / Fondo")]
    public Sprite fondoEscena;

    [Header("Transiciones Visuales")]
    public TipoTransicionEntrada transicionEntrada;
    public bool fadeSalida;
    public float duracionFade;

    [Header("Audio de la Toma")]
    [Tooltip("Música de fondo que arranca o cambia en esta viñeta (Opcional).")]
    public AudioClip musicaFondo;
    public bool musicaEnBucle;
    public bool musicaFadeIn;
    [Tooltip("¿Mantener esta música sonando aunque se cargue la siguiente escena del juego?")]
    public bool mantenerMusicaEnSiguienteEscena;
    [Tooltip("Efecto de sonido de impacto visual que detona justo al entrar la toma (ej. clack de bota, alarma).")]
    public AudioClip sfxImpactoToma;

    [Header("Personajes en esta Toma")]
    public PersonajeEnToma[] personajesPresentes;

    [Header("Caja de Diálogo")]
    public string nombreAMostrar;
    [TextArea(2, 4)]
    public string textoDialogo;

    [Header("Tiempo y Máquina de Escribir")]
    public float velocidadTipeo;
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
    [SerializeField] private CanvasGroup grupoPanelVN;
    [SerializeField] private Image imagenFondo;
    [SerializeField] private CanvasGroup cortinaNegro;

    [Header("Cajas de Texto y Contenedores")]
    [SerializeField] private GameObject cajaNombre;
    [SerializeField] private GameObject cajaDialogo;
    [SerializeField] private IndicadorAnimadoUI indicadorAvance;
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoDialogo;

    [Header("Control de Ritmo y Anti-Spam")]
    [SerializeField] private float multiplicadorFastForward = 4.0f;
    [SerializeField] private float delayAntiSpam = 0.35f;

    private TomaCinematica[] tomasActuales;
    private int indiceToma = 0;
    private bool enCinematica = false;
    private bool enTransicion = false;
    private bool escribiendoTexto = false;
    private bool puedeAvanzar = false;
    private string textoCompletoActual = "";

    private Action alTerminarCallback;
    private Coroutine rutinaAutoAvance;
    private Coroutine rutinaMaquinaEscribir;
    private Coroutine rutinaDelayAntiSpam;

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

        if (indicadorAvance != null) indicadorAvance.Ocultar();
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

        // ESPACIO / ENTER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (escribiendoTexto)
            {
                CompletarTextoInstantaneo();
                return;
            }

            if (puedeAvanzar)
            {
                EjecutarAvance();
            }
            return;
        }

        // CLIC IZQUIERDO
        if (Input.GetMouseButtonDown(0))
        {
            if (puedeAvanzar && !escribiendoTexto)
            {
                EjecutarAvance();
            }
        }
    }

    private void EjecutarAvance()
    {
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);

        bool esUltimaToma = (indiceToma >= tomasActuales.Length - 1);

        // Disparar sonido según si avanza o si es el remate definitivo
        if (Audio_cinematicas.Instance != null)
        {
            if (esUltimaToma) Audio_cinematicas.Instance.PlayDialogoFinal();
            else Audio_cinematicas.Instance.PlayAvanzarDialogo();
        }

        StartCoroutine(AvanzarTomaConFade());
    }

    private IEnumerator ProcesarEntradaToma()
    {
        enTransicion = true;
        puedeAvanzar = false;
        if (indicadorAvance != null) indicadorAvance.Ocultar();

        TomaCinematica toma = tomasActuales[indiceToma];
        float duracion = toma.duracionFade > 0f ? toma.duracionFade : 0.8f;

        // --- GESTIÓN DE AUDIO DE LA TOMA ---
        if (Audio_cinematicas.Instance != null)
        {
            if (toma.musicaFondo != null)
            {
                float fadeDur = toma.musicaFadeIn ? duracion : 0.05f;
                Audio_cinematicas.Instance.ReproducirMusica(toma.musicaFondo, toma.musicaEnBucle, fadeDur, toma.mantenerMusicaEnSiguienteEscena);
            }
            if (toma.sfxImpactoToma != null)
            {
                Audio_cinematicas.Instance.PlaySFX(toma.sfxImpactoToma);
            }
        }

        MontarVisualesToma(toma);

        switch (toma.transicionEntrada)
        {
            case TipoTransicionEntrada.DisolverSobreJuego:
                if (cortinaNegro != null) cortinaNegro.alpha = 0f;
                if (grupoPanelVN != null)
                {
                    grupoPanelVN.alpha = 0f;
                    while (grupoPanelVN.alpha < 1f)
                    {
                        grupoPanelVN.alpha = Mathf.MoveTowards(grupoPanelVN.alpha, 1f, (1f / duracion) * Time.unscaledDeltaTime);
                        yield return null;
                    }
                    grupoPanelVN.alpha = 1f;
                }
                break;

            case TipoTransicionEntrada.FundidoNegro:
                if (grupoPanelVN != null) grupoPanelVN.alpha = 1f;
                if (cortinaNegro != null)
                {
                    cortinaNegro.alpha = 1f;
                    while (cortinaNegro.alpha > 0f)
                    {
                        cortinaNegro.alpha = Mathf.MoveTowards(cortinaNegro.alpha, 0f, (1f / duracion) * Time.unscaledDeltaTime);
                        yield return null;
                    }
                    cortinaNegro.alpha = 0f;
                }
                break;

            case TipoTransicionEntrada.Directo:
            default:
                if (cortinaNegro != null) cortinaNegro.alpha = 0f;
                if (grupoPanelVN != null) grupoPanelVN.alpha = 1f;
                break;
        }

        enTransicion = false;
        IniciarEscrituraDialogo(toma);
    }

    private void MontarVisualesToma(TomaCinematica toma)
    {
        bool tieneTexto = !string.IsNullOrEmpty(toma.textoDialogo);
        bool tieneNombre = !string.IsNullOrEmpty(toma.nombreAMostrar);

        if (cajaDialogo != null) cajaDialogo.SetActive(tieneTexto);
        if (cajaNombre != null) cajaNombre.SetActive(tieneNombre);

        if (textoNombre != null)
        {
            textoNombre.gameObject.SetActive(tieneNombre);
            textoNombre.text = tieneNombre ? toma.nombreAMostrar : "";
        }

        if (indicadorAvance != null) indicadorAvance.Ocultar();

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

        LimpiarTodosLosActores();
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

    private void IniciarEscrituraDialogo(TomaCinematica toma)
    {
        if (rutinaMaquinaEscribir != null) StopCoroutine(rutinaMaquinaEscribir);
        if (rutinaDelayAntiSpam != null) StopCoroutine(rutinaDelayAntiSpam);

        textoCompletoActual = toma.textoDialogo;

        if (string.IsNullOrEmpty(textoCompletoActual))
        {
            if (textoDialogo != null) textoDialogo.text = "";
            escribiendoTexto = false;
            puedeAvanzar = true;
            VerificarTemporizadorAuto(toma);
            return;
        }

        if (textoDialogo != null)
        {
            textoDialogo.gameObject.SetActive(true);

            if (toma.velocidadTipeo > 0f)
            {
                rutinaMaquinaEscribir = StartCoroutine(EscribirTextoGradual(toma));
            }
            else
            {
                textoDialogo.text = textoCompletoActual;
                IniciarCooldownParaAvanzar(toma);
            }
        }
    }

    private IEnumerator EscribirTextoGradual(TomaCinematica toma)
    {
        escribiendoTexto = true;
        puedeAvanzar = false;
        textoDialogo.text = "";

        for (int i = 0; i < textoCompletoActual.Length; i++)
        {
            textoDialogo.text += textoCompletoActual[i];

            // Tocar sonido de tecleo cada 2 caracteres y solo si no es un espacio en blanco
            if (i % 2 == 0 && !char.IsWhiteSpace(textoCompletoActual[i]))
            {
                if (Audio_cinematicas.Instance != null)
                {
                    Audio_cinematicas.Instance.PlaySonidoTipeo();
                }
            }

            float esperaActual = toma.velocidadTipeo;
            if (Input.GetMouseButton(0) && multiplicadorFastForward > 0f)
            {
                esperaActual /= multiplicadorFastForward;
            }

            yield return new WaitForSecondsRealtime(esperaActual);
        }

        IniciarCooldownParaAvanzar(toma);
    }

    private void CompletarTextoInstantaneo()
    {
        if (rutinaMaquinaEscribir != null) StopCoroutine(rutinaMaquinaEscribir);
        if (textoDialogo != null) textoDialogo.text = textoCompletoActual;

        TomaCinematica toma = tomasActuales[indiceToma];
        IniciarCooldownParaAvanzar(toma);
    }

    private void IniciarCooldownParaAvanzar(TomaCinematica toma)
    {
        escribiendoTexto = false;

        // Avisar que la frase terminó de redactarse
        if (Audio_cinematicas.Instance != null && !string.IsNullOrEmpty(toma.textoDialogo))
        {
            Audio_cinematicas.Instance.PlayTextoCompletado();
        }

        if (rutinaDelayAntiSpam != null) StopCoroutine(rutinaDelayAntiSpam);
        rutinaDelayAntiSpam = StartCoroutine(EsperarDelayAntiSpam(toma));
    }

    private IEnumerator EsperarDelayAntiSpam(TomaCinematica toma)
    {
        puedeAvanzar = false;
        if (indicadorAvance != null) indicadorAvance.Ocultar();

        if (delayAntiSpam > 0f)
        {
            yield return new WaitForSecondsRealtime(delayAntiSpam);
        }

        puedeAvanzar = true;

        if (indicadorAvance != null && !string.IsNullOrEmpty(toma.textoDialogo) && toma.tiempoEnPantalla <= 0f)
        {
            indicadorAvance.MostrarYAnimar();
        }

        VerificarTemporizadorAuto(toma);
    }

    private void VerificarTemporizadorAuto(TomaCinematica toma)
    {
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        if (toma.tiempoEnPantalla > 0f)
        {
            rutinaAutoAvance = StartCoroutine(TemporizadorToma(toma.tiempoEnPantalla));
        }
    }

    private IEnumerator AvanzarTomaConFade()
    {
        enTransicion = true;
        puedeAvanzar = false;
        if (indicadorAvance != null) indicadorAvance.Ocultar();

        TomaCinematica tomaActual = tomasActuales[indiceToma];
        float duracion = tomaActual.duracionFade > 0f ? tomaActual.duracionFade : 0.5f;

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
        EjecutarAvance();
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
        if (rutinaMaquinaEscribir != null) StopCoroutine(rutinaMaquinaEscribir);
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        if (rutinaDelayAntiSpam != null) StopCoroutine(rutinaDelayAntiSpam);
        if (indicadorAvance != null) indicadorAvance.Ocultar();

        enCinematica = false;
        enTransicion = false;
        puedeAvanzar = false;
        LimpiarTodosLosActores();
        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);

        Time.timeScale = 1f;
        alTerminarCallback?.Invoke();
    }
}