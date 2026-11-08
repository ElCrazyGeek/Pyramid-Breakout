using UnityEngine;
using UnityEngine.Audio;
using Pyramid.Levels;

[RequireComponent(typeof(Collider))]
public class Repair_kit : MonoBehaviour
{
    [Header("Ajustes de Reparación")]
    [Tooltip("Cantidad de vida que restaura a la nave")]
    [SerializeField] private float repairAmount = 20f;

    [Header("Audio y Mezcla")]
    [Tooltip("Canal del Mixer (arrastra aquí el grupo SFX o Jugador)")]
    [SerializeField] private AudioMixerGroup grupoMixer;
    [SerializeField] private AudioClip sfxRecoger;
    [Range(0f, 1f)][SerializeField] private float volumenAudio = 0.85f;

    [Header("Efecto Visual Opcional")]
    [Tooltip("Partícula que aparece al recoger el kit (opcional)")]
    [SerializeField] private GameObject prefabParticulasRecoger;

    private Collider col;
    private bool yaRecogido = false;

    private void Awake()
    {
        col = GetComponent<Collider>();
        // Forzar siempre a que sea Trigger para evitar choques físicos con la nave
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (yaRecogido) return;

        // Detectar si el objeto o su raíz es el jugador
        if (!other.CompareTag("Player") && other.transform.root.tag != "Player") return;

        Health health = other.GetComponentInParent<Health>() ?? other.GetComponentInChildren<Health>();

        if (health != null)
        {
            yaRecogido = true;

            // 1. Reparar la vida de la nave
            health.RepairDamage(repairAmount);
            Debug.Log($"[Repair_kit] Reparados {repairAmount} HP para {other.gameObject.name}. Vida actual: {health.Current}/{health.maximum}");

            // 2. Reproducir sonido compatible con Audio Mixer sin cortarse
            ReproducirAudioRecoleccion();

            // 3. Efecto de partículas si se asignó
            if (prefabParticulasRecoger != null)
            {
                Instantiate(prefabParticulasRecoger, transform.position, Quaternion.identity);
            }

            // 4. Desactivar visuales y collider de inmediato, destruyendo tras el sonido
            DesactivarYDestruir();
        }
    }

    private void ReproducirAudioRecoleccion()
    {
        if (sfxRecoger == null) return;

        // Emisor temporal en 2D/3D configurado al canal del Audio Mixer
        GameObject audioTemp = new GameObject("SonidoRecogerKitTemp");
        audioTemp.transform.position = transform.position;

        AudioSource src = audioTemp.AddComponent<AudioSource>();
        src.clip = sfxRecoger;
        src.volume = volumenAudio;
        src.spatialBlend = 0f; // Sonido 2D de interfaz nítido en cabina

        if (grupoMixer != null)
        {
            src.outputAudioMixerGroup = grupoMixer;
        }

        src.Play();
        Destroy(audioTemp, sfxRecoger.length);
    }

    private void DesactivarYDestruir()
    {
        // Apaga el collider para no volver a recogerlo
        if (col != null) col.enabled = false;

        // Oculta todas las mallas del kit de reparación
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = false;
        }

        // Destruir el objeto con margen seguro
        float tiempoEspera = (sfxRecoger != null) ? sfxRecoger.length : 0.1f;
        Destroy(gameObject, tiempoEspera);
    }
}