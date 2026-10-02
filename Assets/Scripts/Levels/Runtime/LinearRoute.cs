using UnityEngine;

namespace Pyramid.Levels
{
    public interface IRoute
    {
        Pose Evaluate(Vector3 position);
        float Project(Vector3 worldPosition);
    }

    public sealed class LinearRoute : MonoBehaviour, IRoute
    {
        public Pose Evaluate(Vector3 position) => new Pose(transform.position + transform.rotation * position, transform.rotation);
        public float Project(Vector3 worldPosition) => Vector3.Dot(worldPosition - transform.position, transform.forward);
    }
}
