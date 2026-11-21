using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct DiapositivaTutorial
{
    [Header("Textos")]
    public string tituloPaso;
    [TextArea(2, 4)]
    public string textoInstruccion;

    [Header("Imágenes (Opcional)")]
    public Sprite imagenPrincipal;
    public Sprite imagenSecundaria;

    [Header("Tiempo")]
    [Tooltip("Segundos que permanecerá visible esta diapositiva antes de pasar a la siguiente o cerrarse.")]
    public float duracionSegundos;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Referencias de UI (PanelDialogos/Tutorial)")]
    [SerializeField] private GameObject panelTutorial;
    [SerializeField] private TextMeshProUGUI textoTituloUI;
    [SerializeField] private TextMeshProUGUI textoInstruccionUI;
    [SerializeField] private Image imagenPrincipalUI;
    [SerializeField] private Image imagenSecundariaUI;
    [SerializeField] private GameObject luzParpadeante;

    [Header("Audio Mixer y Efectos de Sonido")]
    [Tooltip("Arrastra aquí tu canal 'SFX' o 'UI' del Audio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixerUI;
    [Tooltip("Sonido cuando aparece por primera vez el cuadro de tutorial")]
    [SerializeField] private AudioClip sfxAparecerPanel;
    [Tooltip("Sonido corto tipo radio/tecleo mientras se escriben las letras")]
    [SerializeField] private AudioClip sfxEscribirLetra;
    [Tooltip("Sonido al pasar de una diapositiva de texto a la siguiente")]
    [SerializeField] private AudioClip sfxCambiarTexto;

    [Header("Ajustes de Volumen y Escritura")]
    [Range(0f, 1f)][SerializeField] private float volumenGeneralUI = 0.85f;
    [Range(0f, 1f)][SerializeField] private float volumenTipeo = 0.45f;
    [Tooltip("Suena cada N letras para que no sature si la velocidad es muy rápida")]
    [Min(1)][SerializeField] private int intervaloSonidoLetras = 2;
    [SerializeField] private float velocidadTipeo = 0.02f;
    [Tooltip("Si en una diapositiva dejas 'Duracion Segundos' en 0, usará este tiempo por defecto.")]
    [SerializeField] private float duracionPorDefecto = 3.5f;

    private AudioSource sourceUI;
    private DiapositivaTutorial[] diapositivasActuales;
    private int indiceActual;
    private Coroutine rutinaSecuencia;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Configurar reproductor 2D conectado al Audio Mixer
        sourceUI = gameObject.AddComponent<AudioSource>();
        sourceUI.playOnAwake = false;
        sourceUI.spatialBlend = 0f;
        sourceUI.ignoreListenerPause = true;

        if (grupoMixerUI != null)
        {
            sourceUI.outputAudioMixerGroup = grupoMixerUI;
        }

        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (luzParpadeante != null) luzParpadeante.SetActive(false);
    }

    public void MostrarTutorial(DiapositivaTutorial[] diapositivas)
    {
        if (diapositivas == null || diapositivas.Length == 0) return;

        if (rutinaSecuencia != null) StopCoroutine(rutinaSecuencia);

        diapositivasActuales = diapositivas;
        indiceActual = 0;

        if (panelTutorial != null) panelTutorial.SetActive(true);

        // 1. Sonido al aparecer el panel
        ReproducirSFX(sfxAparecerPanel, volumenGeneralUI, 1f);

        rutinaSecuencia = StartCoroutine(EjecutarSecuenciaPorTiempo());
    }

    private IEnumerator EjecutarSecuenciaPorTiempo()
    {
        while (indiceActual < diapositivasActuales.Length)
        {
            // Si no es la primera diapositiva, reproducir sonido de cambio de texto
            if (indiceActual > 0)
            {
                ReproducirSFX(sfxCambiarTexto, volumenGeneralUI, 1f);
            }

            DiapositivaTutorial slide = diapositivasActuales[indiceActual];

            // Título opcional
            if (textoTituloUI != null)
            {
                bool tieneTitulo = !string.IsNullOrEmpty(slide.tituloPaso);
                textoTituloUI.gameObject.SetActive(tieneTitulo);
                if (tieneTitulo) textoTituloUI.text = slide.tituloPaso;
            }

            // Imágenes opcionales
            ConfigurarImagen(imagenPrincipalUI, slide.imagenPrincipal);
            ConfigurarImagen(imagenSecundariaUI, slide.imagenSecundaria);

            // Efecto máquina de escribir con sonido
            if (luzParpadeante != null) luzParpadeante.SetActive(false);
            string texto = slide.textoInstruccion ?? "";

            if (textoInstruccionUI != null)
            {
                textoInstruccionUI.text = "";
                if (velocidadTipeo > 0f)
                {
                    int contadorLetras = 0;
                    foreach (char letra in texto)
                    {
                        textoInstruccionUI.text += letra;

                        // 2. Sonido al escribir (ignora espacios y aplica ligera variación de pitch)
                        if (!char.IsWhiteSpace(letra))
                        {
                            if (contadorLetras % intervaloSonidoLetras == 0)
                            {
                                float pitchAleatorio = Random.Range(0.95f, 1.06f);
                                ReproducirSFX(sfxEscribirLetra, volumenTipeo, pitchAleatorio);
                            }
                            contadorLetras++;
                        }

                        yield return new WaitForSecondsRealtime(velocidadTipeo);
                    }
                }
                else
                {
                    textoInstruccionUI.text = texto;
                }
            }

            if (luzParpadeante != null) luzParpadeante.SetActive(true);

            // Esperar el tiempo configurado antes de pasar o cerrar
            float tiempoEspera = slide.duracionSegundos > 0f ? slide.duracionSegundos : duracionPorDefecto;
            yield return new WaitForSecondsRealtime(tiempoEspera);

            indiceActual++;
        }

        CerrarTutorial();
    }

    private void ReproducirSFX(AudioClip clip, float volumen, float pitch)
    {
        if (clip == null || sourceUI == null) return;

        if (grupoMixerUI != null && sourceUI.outputAudioMixerGroup != grupoMixerUI)
        {
            sourceUI.outputAudioMixerGroup = grupoMixerUI;
        }

        sourceUI.pitch = pitch;
        sourceUI.PlayOneShot(clip, volumen);
    }

    private void ConfigurarImagen(Image componenteUI, Sprite sprite)
    {
        if (componenteUI == null) return;

        bool tieneSprite = sprite != null;
        componenteUI.gameObject.SetActive(tieneSprite);
        if (tieneSprite) componenteUI.sprite = sprite;
    }

    public void CerrarTutorial()
    {
        if (rutinaSecuencia != null)
        {
            StopCoroutine(rutinaSecuencia);
            rutinaSecuencia = null;
        }

        if (luzParpadeante != null) luzParpadeante.SetActive(false);
        if (panelTutorial != null) panelTutorial.SetActive(false);
    }
}