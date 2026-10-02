using UnityEngine;

namespace Pyramid.Levels
{
    public sealed class LevelProgress
    {
        public float Distance { get; private set; }
        public void Sample(IRoute route, Vector3 position) => Distance = Mathf.Max(Distance, route.Project(position));
        public void Reset() => Distance = 0;
    }
}
