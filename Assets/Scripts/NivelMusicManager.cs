using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class NivelMusicManager : MonoBehaviour
{
    public static NivelMusicManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMusica;

    [Header("Pistas Dinámicas del Nivel (Reproducción Simultánea)")]
    [SerializeField] private AudioClip musicaTranquila;
    [SerializeField] private AudioClip musicaCombate;
    [Range(0f, 1f)][SerializeField] private float volumenMaximoNivel = 0.7f;

    [Header("Control de Duración del Fade del Nivel (Segundos)")]
    [Min(0.1f)][SerializeField] private float duracionFadeACombate = 1.5f;
    [Min(0.1f)][SerializeField] private float duracionFadeACalma = 3.0f;

    [Header("Detección de Enemigos")]
    [SerializeField] private Transform jugador;
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private float radioDeteccionCombate = 280f;
    [SerializeField] private float retardoCalmaSeg = 2.0f;
    [SerializeField] private float intervaloEscaneo = 0.25f;

    [Header("Música de Menús (Pausa y Game Over)")]
    [SerializeField] private AudioClip musicaPausa;
    [SerializeField] private AudioClip musicaGameOver;
    [Range(0f, 1f)][SerializeField] private float volumenMenu = 0.65f;
    [Min(0.05f)][SerializeField] private float duracionFadeMenu = 0.5f;
    [SerializeField] private bool loopMusicaGameOver = false;

    private AudioSource sourceTranquila;
    private AudioSource sourceCombate;
    private AudioSource sourceMenu;

    private readonly Collider[] bufferEnemigos = new Collider[32];
    private float factorCombateActual = 0f;
    private float temporizadorCalma = 0f;
    private float temporizadorEscaneo = 0f;
    private bool enCombate = false;
    private bool enPausa = false;
    private bool enGameOver = false;
    private Coroutine rutinaTransicionMenu;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        sourceTranquila = CrearFuente2D(musicaTranquila, true, volumenMaximoNivel);
        sourceCombate = CrearFuente2D(musicaCombate, true, 0f);
        sourceMenu = CrearFuente2D(null, false, 0f);
        sourceMenu.ignoreListenerPause = true;
    }

    private AudioSource CrearFuente2D(AudioClip clip, bool loop, float volumenInicial)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = volumenInicial;
        if (grupoMusica != null) src.outputAudioMixerGroup = grupoMusica;
        return src;
    }

    private void Start()
    {
        if (jugador == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) jugador = go.transform;
        }

        double inicioSincronizado = AudioSettings.dspTime + 0.1;
        if (musicaTranquila != null) sourceTranquila.PlayScheduled(inicioSincronizado);
        if (musicaCombate != null) sourceCombate.PlayScheduled(inicioSincronizado);
    }

    private void Update()
    {
        if (enPausa || enGameOver) return;

        temporizadorEscaneo -= Time.deltaTime;
        if (temporizadorEscaneo <= 0f)
        {
            temporizadorEscaneo = intervaloEscaneo;
            bool hayEnemigos = DetectarEnemigosCercanos();

            if (hayEnemigos)
            {
                enCombate = true;
                temporizadorCalma = retardoCalmaSeg;
            }
            else if (enCombate)
            {
                temporizadorCalma -= intervaloEscaneo;
                if (temporizadorCalma <= 0f) enCombate = false;
            }
        }

        float objetivo = enCombate ? 1f : 0f;
        float duracionActual = enCombate ? duracionFadeACombate : duracionFadeACalma;
        float velocidad = 1f / Mathf.Max(0.05f, duracionActual);

        factorCombateActual = Mathf.MoveTowards(factorCombateActual, objetivo, velocidad * Time.deltaTime);
        float curvaSuave = Mathf.SmoothStep(0f, 1f, factorCombateActual);

        if (sourceTranquila != null)
            sourceTranquila.volume = (1f - curvaSuave) * volumenMaximoNivel;

        if (sourceCombate != null)
            sourceCombate.volume = curvaSuave * volumenMaximoNivel;
    }

    private bool DetectarEnemigosCercanos()
    {
        if (jugador == null) return false;

        int encontrados = Physics.OverlapSphereNonAlloc(
            jugador.position,
            radioDeteccionCombate,
            bufferEnemigos,
            capaEnemigos,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < encontrados; i++)
        {
            if (bufferEnemigos[i] != null && bufferEnemigos[i].gameObject.activeInHierarchy)
                return true;
        }
        return false;
    }

    public void EntrarEnPausa()
    {
        enPausa = true;
        if (rutinaTransicionMenu != null) StopCoroutine(rutinaTransicionMenu);
        rutinaTransicionMenu = StartCoroutine(RutinaEntrarPausa());
    }

    public void SalirDePausa()
    {
        enPausa = false;
        if (rutinaTransicionMenu != null) StopCoroutine(rutinaTransicionMenu);
        rutinaTransicionMenu = StartCoroutine(RutinaSalirPausa());
    }

    public void ActivarMusicaGameOver()
    {
        enGameOver = true;
        enPausa = false;
        if (rutinaTransicionMenu != null) StopCoroutine(rutinaTransicionMenu);
        rutinaTransicionMenu = StartCoroutine(RutinaGameOver());
    }

    private IEnumerator RutinaEntrarPausa()
    {
        float vel = 1f / Mathf.Max(0.05f, duracionFadeMenu);
        float volIniTranquila = sourceTranquila != null ? sourceTranquila.volume : 0f;
        float volIniCombate = sourceCombate != null ? sourceCombate.volume : 0f;

        if (musicaPausa != null && sourceMenu != null)
        {
            sourceMenu.clip = musicaPausa;
            sourceMenu.loop = true;
            sourceMenu.volume = 0f;
            sourceMenu.Play();
        }

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, vel * Time.unscaledDeltaTime);
            if (sourceTranquila != null) sourceTranquila.volume = Mathf.Lerp(volIniTranquila, 0f, t);
            if (sourceCombate != null) sourceCombate.volume = Mathf.Lerp(volIniCombate, 0f, t);
            if (sourceMenu != null && musicaPausa != null) sourceMenu.volume = Mathf.Lerp(0f, volumenMenu, t);
            yield return null;
        }

        if (sourceTranquila != null && sourceTranquila.isPlaying) sourceTranquila.Pause();
        if (sourceCombate != null && sourceCombate.isPlaying) sourceCombate.Pause();
    }

    private IEnumerator RutinaSalirPausa()
    {
        float vel = 1f / Mathf.Max(0.05f, duracionFadeMenu);
        float curvaSuave = Mathf.SmoothStep(0f, 1f, factorCombateActual);
        float volObjTranquila = (1f - curvaSuave) * volumenMaximoNivel;
        float volObjCombate = curvaSuave * volumenMaximoNivel;
        float volIniMenu = sourceMenu != null ? sourceMenu.volume : 0f;

        if (sourceTranquila != null)
        {
            sourceTranquila.volume = 0f;
            sourceTranquila.UnPause();
        }
        if (sourceCombate != null)
        {
            sourceCombate.volume = 0f;
            sourceCombate.UnPause();
        }

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, vel * Time.unscaledDeltaTime);
            if (sourceMenu != null) sourceMenu.volume = Mathf.Lerp(volIniMenu, 0f, t);
            if (sourceTranquila != null) sourceTranquila.volume = Mathf.Lerp(0f, volObjTranquila, t);
            if (sourceCombate != null) sourceCombate.volume = Mathf.Lerp(0f, volObjCombate, t);
            yield return null;
        }

        if (sourceMenu != null && sourceMenu.isPlaying) sourceMenu.Stop();
    }

    private IEnumerator RutinaGameOver()
    {
        float vel = 1f / Mathf.Max(0.05f, duracionFadeMenu);
        float volIniTranquila = sourceTranquila != null ? sourceTranquila.volume : 0f;
        float volIniCombate = sourceCombate != null ? sourceCombate.volume : 0f;
        float volIniMenu = (sourceMenu != null && sourceMenu.isPlaying) ? sourceMenu.volume : 0f;

        // 1. Bajar suavemente lo que estuviera sonando
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, vel * Time.unscaledDeltaTime);
            if (sourceTranquila != null) sourceTranquila.volume = Mathf.Lerp(volIniTranquila, 0f, t);
            if (sourceCombate != null) sourceCombate.volume = Mathf.Lerp(volIniCombate, 0f, t);
            if (sourceMenu != null && sourceMenu.isPlaying) sourceMenu.volume = Mathf.Lerp(volIniMenu, 0f, t);
            yield return null;
        }

        if (sourceTranquila != null) sourceTranquila.Stop();
        if (sourceCombate != null) sourceCombate.Stop();

        // 2. Arrancar tema de Game Over con fade in
        if (musicaGameOver != null && sourceMenu != null)
        {
            sourceMenu.clip = musicaGameOver;
            sourceMenu.loop = loopMusicaGameOver;
            sourceMenu.volume = 0f;
            sourceMenu.Play();

            while (sourceMenu.volume < volumenMenu)
            {
                sourceMenu.volume = Mathf.MoveTowards(sourceMenu.volume, volumenMenu, vel * Time.unscaledDeltaTime);
                yield return null;
            }
            sourceMenu.volume = volumenMenu;
        }
    }
}