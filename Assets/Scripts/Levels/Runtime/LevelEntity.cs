using System;
using UnityEngine;

namespace Pyramid.Levels
{
    public interface ILevelEntityAdapter
    {
        bool SupportsParameter(string key);
        string ValidateSettings(EntitySettings settings);
        void Configure(EntitySettings settings, Transform player, Camera camera);
        void Begin();
        void End();
    }

    public sealed class LevelEntity : MonoBehaviour
    {
        public string Segment { get; private set; }
        public string EventId { get; private set; }
        public Action<LevelEntity> Released;
        ILevelEntityAdapter[] adapters;
        float remaining;
        bool running;

        public void Initialize(string eventId, string segment, EntitySettings settings, Transform player, Camera camera)
        {
            EventId = eventId;
            Segment = segment;
            remaining = settings.lifetime;
            adapters = GetComponentsInChildren<ILevelEntityAdapter>(true);
            if (TryGetComponent<Health>(out var health)) health.ResetHealth(settings.health);
            foreach (var adapter in adapters) adapter.Configure(settings.Copy(), player, camera);
        }
        public void Begin()
        {
            running = true;
            foreach (var adapter in adapters) adapter.Begin();
        }
        void Update()
        {
            if (!running || remaining <= 0) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0) Release();
        }
        public void Release()
        {
            if (!running) return;
            running = false;
            if (adapters != null) foreach (var adapter in adapters) adapter.End();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        void OnDestroy() { Released?.Invoke(this); Released = null; }
    }
}
