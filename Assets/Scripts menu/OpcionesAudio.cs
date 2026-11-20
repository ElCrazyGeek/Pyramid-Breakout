using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class OpcionesAudioUI : MonoBehaviour
{
    [Header("Referencia al Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders de Interfaz")]
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderSFX;

    [Header("Textos de Porcentaje (Opcional)")]
    [SerializeField] private TextMeshProUGUI textoPorcentajeMaster;
    [SerializeField] private TextMeshProUGUI textoPorcentajeMusica;
    [SerializeField] private TextMeshProUGUI textoPorcentajeSFX;

    // Nombres exactos expuestos en el Audio Mixer
    private const string PARAM_MASTER = "Volumen_General";
    private const string PARAM_MUSICA = "Volumen_Musica";
    private const string PARAM_SFX = "Volumen_SFX";

    // Claves para guardar en PlayerPrefs
    private const string KEY_MASTER = "Config_Volumen_General";
    private const string KEY_MUSICA = "Config_Volumen_Musica";
    private const string KEY_SFX = "Config_Volumen_SFX";

    private void Awake()
    {
        ConfigurarRangoSlider(sliderMaster);
        ConfigurarRangoSlider(sliderMusica);
        ConfigurarRangoSlider(sliderSFX);
    }

    private void Start()
    {
        CargarYAplicarVolumenes();

        if (sliderMaster != null) sliderMaster.onValueChanged.AddListener(CambiarVolumenMaster);
        if (sliderMusica != null) sliderMusica.onValueChanged.AddListener(CambiarVolumenMusica);
        if (sliderSFX != null) sliderSFX.onValueChanged.AddListener(CambiarVolumenSFX);
    }

    private void OnEnable()
    {
        // Sincronizar sliders cada vez que se abre el panel de opciones
        CargarYAplicarVolumenes();
    }

    private void ConfigurarRangoSlider(Slider slider)
    {
        if (slider == null) return;
        // Mínimo 0.0001f para evitar Log10(0) = -Infinito
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
    }

    public void CargarYAplicarVolumenes()
    {
        float volMaster = PlayerPrefs.GetFloat(KEY_MASTER, 0.85f);
        float volMusica = PlayerPrefs.GetFloat(KEY_MUSICA, 0.8f);
        float volSFX = PlayerPrefs.GetFloat(KEY_SFX, 0.85f);

        if (sliderMaster != null) sliderMaster.SetValueWithoutNotify(volMaster);
        if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(volMusica);
        if (sliderSFX != null) sliderSFX.SetValueWithoutNotify(volSFX);

        AplicarAlMixer(PARAM_MASTER, volMaster, textoPorcentajeMaster);
        AplicarAlMixer(PARAM_MUSICA, volMusica, textoPorcentajeMusica);
        AplicarAlMixer(PARAM_SFX, volSFX, textoPorcentajeSFX);
    }

    public void CambiarVolumenMaster(float valor)
    {
        AplicarAlMixer(PARAM_MASTER, valor, textoPorcentajeMaster);
        PlayerPrefs.SetFloat(KEY_MASTER, valor);
        PlayerPrefs.Save();
    }

    public void CambiarVolumenMusica(float valor)
    {
        AplicarAlMixer(PARAM_MUSICA, valor, textoPorcentajeMusica);
        PlayerPrefs.SetFloat(KEY_MUSICA, valor);
        PlayerPrefs.Save();
    }

    public void CambiarVolumenSFX(float valor)
    {
        AplicarAlMixer(PARAM_SFX, valor, textoPorcentajeSFX);
        PlayerPrefs.SetFloat(KEY_SFX, valor);
        PlayerPrefs.Save();
    }

    private void AplicarAlMixer(string parametroExpuesto, float valorLineal, TextMeshProUGUI textoUI)
    {
        float valorSeguro = Mathf.Clamp(valorLineal, 0.0001f, 1f);

        if (audioMixer != null)
        {
            // Fórmula estándar: escala lineal (0..1) a decibelios (-80 dB..0 dB)
            float decibelios = Mathf.Log10(valorSeguro) * 20f;
            audioMixer.SetFloat(parametroExpuesto, decibelios);
        }

        if (textoUI != null)
        {
            int porcentaje = Mathf.RoundToInt((valorSeguro <= 0.001f ? 0f : valorSeguro) * 100f);
            textoUI.text = $"{porcentaje}%";
        }
    }

    public void RestablecerPorDefecto()
    {
        CambiarVolumenMaster(0.85f);
        CambiarVolumenMusica(0.8f);
        CambiarVolumenSFX(0.85f);

        if (sliderMaster != null) sliderMaster.SetValueWithoutNotify(0.85f);
        if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(0.8f);
        if (sliderSFX != null) sliderSFX.SetValueWithoutNotify(0.85f);
    }
}