using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pyramid.Levels
{
    public sealed class EntitySpawner : MonoBehaviour
    {
        readonly List<LevelEntity> entities = new List<LevelEntity>();
        Transform staging;
        public int Count { get { entities.RemoveAll(x => !x); return entities.Count; } }

        public LevelEntity Spawn(LevelEvent item, IRoute route, Transform player, Camera camera)
        {
            if (!(item.resource is EntityDefinition definition) || !definition.prefab)
                throw new InvalidOperationException($"Evento '{item.id}': prefab no disponible.");
            if (!staging)
            {
                staging = new GameObject("Preparación de entidades").transform;
                staging.SetParent(transform, false);
                staging.gameObject.SetActive(false);
            }
            var pose = route.Evaluate(item.routePosition);
            var instance = Instantiate(definition.prefab, pose.position, pose.rotation, staging);
            instance.SetActive(false);
            var entity = instance.GetComponent<LevelEntity>() ?? instance.AddComponent<LevelEntity>();
            entity.Initialize(item.id, item.segment, item.settings, player, camera);
            entity.Released += OnReleased;
            entities.Add(entity);
            instance.transform.SetParent(transform, true);
            instance.SetActive(true);
            entity.Begin();
            return entity;
        }
        void OnReleased(LevelEntity entity) => entities.Remove(entity);
        public void ReleaseSegment(string id)
        {
            foreach (var entity in entities.ToArray()) if (entity && entity.Segment == id) entity.Release();
        }
        public void Clear()
        {
            foreach (var entity in entities.ToArray()) if (entity) entity.Release();
            entities.Clear();
        }
    }
}
