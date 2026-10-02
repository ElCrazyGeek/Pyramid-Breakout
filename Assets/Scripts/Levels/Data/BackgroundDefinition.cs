using UnityEngine;

namespace Pyramid.Levels
{
    [CreateAssetMenu(menuName = "Pyramid/Niveles/Fondo")]
    public sealed class BackgroundDefinition : ContentDefinition
    {
        public Material skybox;
        public Color cameraColor = Color.black;
        public GameObject backdropPrefab;
        [Tooltip("0 = fondo fijo; 1 = sigue la cámara. Sin colisiones.")]
        [Range(0, 1)] public float followCamera = 1;
        public Vector3 cameraOffset = new Vector3(0, 0, 150);
    }
}
