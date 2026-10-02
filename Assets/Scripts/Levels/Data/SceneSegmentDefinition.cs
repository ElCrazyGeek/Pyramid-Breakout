using UnityEngine;

namespace Pyramid.Levels
{
    [CreateAssetMenu(menuName = "Pyramid/Niveles/Tramo de escena")]
    public sealed class SceneSegmentDefinition : ContentDefinition
    {
        [Tooltip("Ruta completa Assets/.../Escena.unity incluida en Build Settings.")]
        public string scenePath;
    }
}
