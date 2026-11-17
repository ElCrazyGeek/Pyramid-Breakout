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
            var rotation = definition.preservePrefabRotation
                ? pose.rotation * definition.prefab.transform.localRotation
                : pose.rotation;
            var instance = Instantiate(definition.prefab, pose.position, rotation, staging);
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

    public sealed class MeteoriteZoneRunner
    {
        sealed class ZoneState
        {
            public MeteoriteZoneDefinition definition;
            public readonly List<LevelEntity> active = new List<LevelEntity>();
            public System.Random random;
            public float nextSpawnAt;
            public int spawnIndex;
        }

        readonly ZoneState[] zones;
        readonly EntitySpawner spawner;
        readonly IRoute route;
        readonly Transform player;
        readonly Camera camera;

        public MeteoriteZoneRunner(MeteoriteZoneDefinition[] definitions, EntitySpawner spawner, IRoute route, Transform player, Camera camera)
        {
            this.spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            this.route = route ?? throw new ArgumentNullException(nameof(route));
            this.player = player;
            this.camera = camera;
            definitions = definitions ?? Array.Empty<MeteoriteZoneDefinition>();
            zones = new ZoneState[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                MeteoriteZoneDefinition definition = definitions[i];
                int seed = definition.seed == 0 ? Environment.TickCount ^ Guid.NewGuid().GetHashCode() : definition.seed;
                zones[i] = new ZoneState
                {
                    definition = definition,
                    random = new System.Random(seed),
                    nextSpawnAt = definition.start
                };
            }
        }

        public void Tick(float distance)
        {
            foreach (ZoneState zone in zones)
            {
                MeteoriteZoneDefinition definition = zone.definition;
                zone.active.RemoveAll(entity => !entity);
                if (distance < definition.start || distance >= definition.end || distance < zone.nextSpawnAt)
                    continue;

                if (zone.active.Count < definition.maxActive)
                    Spawn(zone, distance);

                zone.nextSpawnAt = distance + RandomRange(zone, definition.minInterval, definition.maxInterval);
            }
        }

        void Spawn(ZoneState zone, float distance)
        {
            MeteoriteZoneDefinition definition = zone.definition;
            MeteoriteSpawnChoice choice = SelectMeteorite(zone);
            float s = Mathf.Min(definition.end, distance + (float)zone.random.NextDouble() * definition.anticipation);
            var item = new LevelEvent
            {
                id = $"{definition.id}_{zone.spawnIndex++}",
                resource = choice.resource,
                routePosition = new Vector3(
                    RandomRange(zone, definition.xMin, definition.xMax),
                    RandomRange(zone, definition.yMin, definition.yMax),
                    s),
                settings = choice.resource.defaults.Copy(),
                segment = definition.segment
            };
            item.settings.lifetime = definition.lifetime;
            zone.active.Add(spawner.Spawn(item, route, player, camera));
        }

        static MeteoriteSpawnChoice SelectMeteorite(ZoneState zone)
        {
            double totalWeight = 0;
            foreach (MeteoriteSpawnChoice choice in zone.definition.meteorites)
                totalWeight += choice.weight;

            double selection = zone.random.NextDouble() * totalWeight;
            foreach (MeteoriteSpawnChoice choice in zone.definition.meteorites)
            {
                selection -= choice.weight;
                if (selection < 0) return choice;
            }
            return zone.definition.meteorites[zone.definition.meteorites.Length - 1];
        }

        static float RandomRange(ZoneState zone, float minimum, float maximum)
        {
            return minimum + (float)zone.random.NextDouble() * (maximum - minimum);
        }
    }
}
