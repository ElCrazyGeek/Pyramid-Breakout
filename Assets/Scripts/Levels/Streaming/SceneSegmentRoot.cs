using UnityEngine;

namespace Pyramid.Levels
{
    public sealed class SceneSegmentRoot : MonoBehaviour
    {
        [Tooltip("Contenido guardado inactivo en la escena; se activa después de colocarlo.")]
        public GameObject content;

        public void Prepare(IRoute route, float start)
        {
            var pose = route.Evaluate(new Vector3(0, 0, start));
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (content) content.SetActive(true);
        }
    }
}
