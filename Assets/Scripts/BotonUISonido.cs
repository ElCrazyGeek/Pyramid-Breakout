using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SonidoBotonUI : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Audio Mixer")]
    [Tooltip("Arrastra aquí el grupo 'UI' o 'SFX' de tu Audio Mixer")]
    [SerializeField] private AudioMixerGroup grupoMixerUI;

    [Header("Clips de Sonido")]
    [Tooltip("Sonido opcional al pasar el puntero por encima del botón")]
    [SerializeField] private AudioClip sfxHover;
    [Range(0f, 1f)][SerializeField] private float volumenHover = 0.5f;

    [Tooltip("Sonido al hacer clic en el botón")]
    [SerializeField] private AudioClip sfxClick;
    [Range(0f, 1f)][SerializeField] private float volumenClick = 0.85f;

    private Button boton;
    private static AudioSource sourceUIGlobal;

    private void Awake()
    {
        boton = GetComponent<Button>();

        // Crea un único AudioSource persistente para que el sonido del clic no se corte al cambiar de escena
        if (sourceUIGlobal == null)
        {
            GameObject go = new GameObject("AudioUI_Global");
            DontDestroyOnLoad(go);
            sourceUIGlobal = go.AddComponent<AudioSource>();
            sourceUIGlobal.playOnAwake = false;
            sourceUIGlobal.spatialBlend = 0f;
            sourceUIGlobal.ignoreListenerPause = true; // Suena incluso con Time.timeScale = 0
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boton != null && !boton.interactable) return;
        Reproducir(sfxHover, volumenHover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boton != null && !boton.interactable) return;
        Reproducir(sfxClick, volumenClick);
    }

    private void Reproducir(AudioClip clip, float volumen)
    {
        if (clip == null || sourceUIGlobal == null) return;

        if (grupoMixerUI != null)
        {
            sourceUIGlobal.outputAudioMixerGroup = grupoMixerUI;
        }

        sourceUIGlobal.pitch = Random.Range(0.98f, 1.02f);
        sourceUIGlobal.PlayOneShot(clip, volumen);
    }
}