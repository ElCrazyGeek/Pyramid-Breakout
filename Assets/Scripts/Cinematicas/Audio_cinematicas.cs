using System.Collections;
using UnityEngine;

public class Audio_cinematicas : MonoBehaviour
{
    public static Audio_cinematicas Instance { get; private set; }

    [Header("Fuentes de Audio (AudioSources)")]
    [SerializeField] private AudioSource sourceMusica;
    [SerializeField] private AudioSource sourceSFX;
    [SerializeField] private AudioSource sourceVozTipeo;

    [Header("Sonidos de Interfaz y Diálogo")]
    [SerializeField] private AudioClip sfxTipeoLetra;
    [SerializeField] private AudioClip sfxTextoCompletado;
    [SerializeField] private AudioClip sfxAvanzarDialogo;
    [SerializeField] private AudioClip sfxDialogoFinal;

    [Header("Ajustes de Sonido")]
    [Range(0f, 1f)][SerializeField] private float volumenMusica = 0.75f;
    [Range(0f, 1f)][SerializeField] private float volumenSFX = 1f;

    private Coroutine rutinaFadeMusica;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Crear fuentes automáticas si no se asignaron en el inspector
        if (sourceMusica == null) sourceMusica = gameObject.AddComponent<AudioSource>();
        if (sourceSFX == null) sourceSFX = gameObject.AddComponent<AudioSource>();
        if (sourceVozTipeo == null) sourceVozTipeo = gameObject.AddComponent<AudioSource>();

        sourceMusica.playOnAwake = false;
        sourceSFX.playOnAwake = false;
        sourceVozTipeo.playOnAwake = false;
    }

    // --- GESTIÓN DE MÚSICA CON FADES ---

    public void ReproducirMusica(AudioClip nuevaPista, bool loop, float duracionFadeIn, bool conservarEntreEscenas)
    {
        if (nuevaPista == null) return;

        // Si ya está sonando la misma pista, no la reinicies
        if (sourceMusica.clip == nuevaPista && sourceMusica.isPlaying) return;

        if (conservarEntreEscenas)
        {
            DontDestroyOnLoad(sourceMusica.gameObject);
        }

        if (rutinaFadeMusica != null) StopCoroutine(rutinaFadeMusica);
        rutinaFadeMusica = StartCoroutine(RutinaCambiarMusica(nuevaPista, loop, duracionFadeIn));
    }

    private IEnumerator RutinaCambiarMusica(AudioClip clip, bool loop, float duracionFade)
    {
        // 1. Fade Out si ya había música sonando
        if (sourceMusica.isPlaying && sourceMusica.volume > 0f)
        {
            while (sourceMusica.volume > 0f)
            {
                sourceMusica.volume = Mathf.MoveTowards(sourceMusica.volume, 0f, (1f / duracionFade) * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        // 2. Asignar nuevo clip
        sourceMusica.clip = clip;
        sourceMusica.loop = loop;
        sourceMusica.Play();

        // 3. Fade In suave hasta el volumen objetivo
        while (sourceMusica.volume < volumenMusica)
        {
            sourceMusica.volume = Mathf.MoveTowards(sourceMusica.volume, volumenMusica, (volumenMusica / duracionFade) * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    public void DetenerMusicaConFade(float duracionFadeOut)
    {
        if (rutinaFadeMusica != null) StopCoroutine(rutinaFadeMusica);
        rutinaFadeMusica = StartCoroutine(RutinaFadeOut(duracionFadeOut));
    }

    private IEnumerator RutinaFadeOut(float duracionFade)
    {
        while (sourceMusica.volume > 0f)
        {
            sourceMusica.volume = Mathf.MoveTowards(sourceMusica.volume, 0f, (1f / duracionFade) * Time.unscaledDeltaTime);
            yield return null;
        }
        sourceMusica.Stop();
    }

    // --- EFECTOS DE SONIDO (SFX) ---

    public void PlaySFX(AudioClip clip, float pitchVariacion = 0f)
    {
        if (clip == null || sourceSFX == null) return;
        sourceSFX.pitch = 1f + Random.Range(-pitchVariacion, pitchVariacion);
        sourceSFX.PlayOneShot(clip, volumenSFX);
    }

    public void PlaySonidoTipeo()
    {
        if (sfxTipeoLetra == null || sourceVozTipeo == null) return;
        // Variación sutil de pitch para que el tecleo/blip no suene robótico
        sourceVozTipeo.pitch = Random.Range(0.95f, 1.05f);
        sourceVozTipeo.PlayOneShot(sfxTipeoLetra, volumenSFX * 0.45f);
    }

    public void PlayTextoCompletado() => PlaySFX(sfxTextoCompletado);
    public void PlayAvanzarDialogo() => PlaySFX(sfxAvanzarDialogo);
    public void PlayDialogoFinal() => PlaySFX(sfxDialogoFinal);
}