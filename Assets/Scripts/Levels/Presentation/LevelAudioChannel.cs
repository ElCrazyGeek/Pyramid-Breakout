using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pyramid.Levels
{
    // Un canal para música y otro para ambiente. Las transiciones interrumpidas
    // parten de los volúmenes actuales, conservando las voces que todavía se oyen.
    public sealed class LevelAudioChannel : MonoBehaviour
    {
        readonly List<AudioSource> voices = new List<AudioSource>();
        Coroutine transition;
        bool paused;

        public void Play(AudioDefinition value, float seconds)
        {
            if (transition != null) StopCoroutine(transition);
            voices.RemoveAll(x => !x);
            var previous = voices.ToArray();
            AudioSource next = null;
            if (value && value.clip)
            {
                next = gameObject.AddComponent<AudioSource>();
                next.playOnAwake = false;
                next.spatialBlend = 0;
                next.clip = value.clip;
                next.loop = value.loop;
                next.outputAudioMixerGroup = value.mixerGroup;
                next.volume = 0;
                voices.Add(next);
                next.Play();
                if (paused) next.Pause();
            }
            transition = StartCoroutine(Blend(previous, next, value ? value.volume : 0, seconds));
        }
        IEnumerator Blend(AudioSource[] old, AudioSource next, float target, float duration)
        {
            var initial = new float[old.Length];
            for (int i = 0; i < old.Length; i++) initial[i] = old[i] ? old[i].volume : 0;
            float elapsed = 0;
            do
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < old.Length; i++) if (old[i]) old[i].volume = initial[i] * (1 - t);
                if (next) next.volume = target * t;
                if (t >= 1) break;
                yield return null;
            } while (true);
            foreach (var voice in old) if (voice) { voice.Stop(); voices.Remove(voice); Destroy(voice); }
        }
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            foreach (var voice in voices) if (voice) { if (value) voice.Pause(); else voice.UnPause(); }
        }
        public void Clear()
        {
            if (transition != null) StopCoroutine(transition);
            foreach (var voice in voices) if (voice) { voice.Stop(); Destroy(voice); }
            voices.Clear();
            paused = false;
        }
    }
}
