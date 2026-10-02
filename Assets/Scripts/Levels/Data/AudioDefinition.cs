using UnityEngine;
using UnityEngine.Audio;

namespace Pyramid.Levels
{
    [CreateAssetMenu(menuName = "Pyramid/Niveles/Audio")]
    public sealed class AudioDefinition : ContentDefinition
    {
        public AudioClip clip;
        [Range(0, 1)] public float volume = 0.6f;
        public bool loop = true;
        public AudioMixerGroup mixerGroup;
    }
}
