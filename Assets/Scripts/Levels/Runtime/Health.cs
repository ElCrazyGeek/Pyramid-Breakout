using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace Pyramid.Levels
{
    public interface IDamageable { void TakeDamage(float amount); }

    public sealed class Health : MonoBehaviour, IDamageable
    {
        [Min(0.01f)] public float maximum = 40;
        public float Current { get; private set; }
        bool initialized;

        [Header("UI - Barra e Información")]
        [Tooltip("Arrastra aquí la imagen 'Vida' configurada en Image Type = Filled")]
        [SerializeField] private Image barraVidaImagen;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Efectos al Recibir Daño (Hit Feedback)")]
        [SerializeField] private Color colorParpadeo = Color.red;
        [SerializeField] private float duracionParpadeo = 0.08f;

        [Header("Audio y Canales del Mixer")]
        [Tooltip("Arrastra aquí el grupo del AudioMixer (ej. SFX, Jugador o Enemigos)")]
        [SerializeField] private AudioMixerGroup grupoMixer;
        [SerializeField] private AudioClip sfxImpacto;
        [Tooltip("Control de volumen independiente para el impacto")]
        [Range(0f, 1f)][SerializeField] private float volumenImpacto = 0.5f;

        [Header("Efectos al Morir (Explosión)")]
        [SerializeField] private GameObject prefabExplosion;
        [SerializeField] private AudioClip sfxExplosionMuerte;
        [Tooltip("Control de volumen para la explosión")]
        [Range(0f, 1f)][SerializeField] private float volumenExplosion = 0.7f;

        [Header("Feedback de Impacto (Solo Jugador)")]
        [SerializeField] private ImpactFeedback feedbackJugador;

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Color> coloresOriginales = new List<Color>();
        private AudioSource audioSource;
        private Coroutine rutinaParpadeo;

        void Awake()
        {
            if (!initialized) ResetHealth(maximum);
            ActualizarUI();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Enlazar el AudioSource del objeto al canal del Audio Mixer
            if (grupoMixer != null)
            {
                audioSource.outputAudioMixerGroup = grupoMixer;
            }

            if (feedbackJugador == null)
            {
                feedbackJugador = GetComponent<ImpactFeedback>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f;
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 60f;

            GetComponentsInChildren(true, renderers);
            foreach (var rend in renderers)
            {
                if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
                {
                    coloresOriginales.Add(rend.material.color);
                }
                else if (rend != null && rend.material != null && rend.material.HasProperty("_BaseColor"))
                {
                    coloresOriginales.Add(rend.material.GetColor("_BaseColor"));
                }
                else
                {
                    coloresOriginales.Add(Color.white);
                }
            }
        }

        public void ResetHealth(float value)
        {
            maximum = value;
            Current = value;
            initialized = true;
            ActualizarUI();
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0 || Current <= 0) return;

            if (feedbackJugador != null)
            {
                feedbackJugador.EjecutarSacudidaImpacto();
            }

            Current = Mathf.Max(0, Current - amount);
            Debug.Log($"[Health] {gameObject.name} recibió {amount} de daño. Salud actual: {Current}/{maximum}");

            ActualizarUI();

            if (sfxImpacto != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxImpacto, volumenImpacto);
            }

            if (gameObject.activeInHierarchy)
            {
                if (rutinaParpadeo != null) StopCoroutine(rutinaParpadeo);
                rutinaParpadeo = StartCoroutine(RutinaParpadeo());
            }

            if (Current > 0) return;

            // --- Lógica de Muerte ---
            LevelSession session = null;
            var parentSession = GetComponentInParent<LevelSession>();
            if (parentSession != null && parentSession.player != null)
            {
                if (transform == parentSession.player || transform.IsChildOf(parentSession.player))
                    session = parentSession;
            }
            if (session == null && LevelSession.Active != null && LevelSession.Active.player != null)
            {
                var playerTransform = LevelSession.Active.player;
                if (transform == playerTransform || transform.IsChildOf(playerTransform)) session = LevelSession.Active;
            }

            if (session != null)
            {
                if (GameOverManager.Instance != null)
                {
                    GameOverManager.Instance.MostrarGameOver();
                }
                else
                {
                    session.Fail("El jugador ha sido destruido.");
                }
            }
            else
            {
                EjecutarEfectoMuerte();

                if (TryGetComponent<LevelEntity>(out var entity))
                {
                    entity.Release();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        private void ActualizarUI()
        {
            if (barraVidaImagen != null)
            {
                barraVidaImagen.fillAmount = Mathf.Clamp01(Current / maximum);
            }

            if (healthText != null)
            {
                healthText.SetText($"{Current} / {maximum}");
            }
        }

        private void EjecutarEfectoMuerte()
        {
            if (prefabExplosion != null)
            {
                var fx = Instantiate(prefabExplosion, transform.position, Quaternion.identity);
                if (LevelSession.Active != null)
                {
                    LevelSession.Active.Objects.Track(fx);
                }
            }

            // Reproductor temporal que enruta la explosión al Audio Mixer en lugar de saltárselo
            if (sfxExplosionMuerte != null)
            {
                GameObject audioTemp = new GameObject("SonidoExplosionTemp");
                audioTemp.transform.position = transform.position;

                AudioSource src = audioTemp.AddComponent<AudioSource>();
                src.clip = sfxExplosionMuerte;
                src.volume = volumenExplosion;
                src.spatialBlend = 0.8f;
                src.minDistance = 5f;
                src.maxDistance = 60f;

                if (grupoMixer != null)
                {
                    src.outputAudioMixerGroup = grupoMixer;
                }

                src.Play();
                Destroy(audioTemp, sfxExplosionMuerte.length);
            }
        }

        private IEnumerator RutinaParpadeo()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", colorParpadeo);
                    else if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.color = colorParpadeo;
                }
            }

            yield return new WaitForSeconds(duracionParpadeo);

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", coloresOriginales[i]);
                    else if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.color = coloresOriginales[i];
                }
            }

            rutinaParpadeo = null;
        }

        public void RepairDamage(float amount)
        {
            if (amount <= 0f) return;
            if (!initialized) ResetHealth(maximum);

            Current = Mathf.Min(maximum, Current + amount);
            Debug.Log($"[Health] {gameObject.name} reparado {amount}. Salud actual: {Current}/{maximum}");
            ActualizarUI();
        }

        private void OnDisable()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", coloresOriginales[i]);
                    else if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.color = coloresOriginales[i];
                }
            }
        }
    }
}