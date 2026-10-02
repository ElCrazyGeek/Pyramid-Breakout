using UnityEngine;
using UnityEngine.Rendering;

namespace Pyramid.Levels
{
    [CreateAssetMenu(menuName = "Pyramid/Niveles/Ambientación")]
    public sealed class EnvironmentDefinition : ContentDefinition
    {
        public Color ambient = Color.gray;
        public Color lightColor = Color.white;
        [Min(0)] public float lightIntensity = 1;
        public Vector3 lightEuler = new Vector3(50, -30, 0);
        public bool fog;
        public Color fogColor = Color.gray;
        [Min(0)] public float fogDensity = 0.01f;
        public VolumeProfile volumeProfile;
        public GameObject particlesPrefab;
    }
}
