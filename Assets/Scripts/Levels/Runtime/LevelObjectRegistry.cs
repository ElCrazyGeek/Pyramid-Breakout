using System.Collections.Generic;
using UnityEngine;

namespace Pyramid.Levels
{
    // Objetos efímeros creados fuera de EntitySpawner, principalmente proyectiles.
    public sealed class LevelObjectRegistry
    {
        readonly HashSet<GameObject> objects = new HashSet<GameObject>();
        public void Track(GameObject value) { if (value) objects.Add(value); }
        public void Forget(GameObject value) => objects.Remove(value);
        public void Clear()
        {
            foreach (var value in objects) if (value) { value.SetActive(false); Object.Destroy(value); }
            objects.Clear();
        }
    }
}
