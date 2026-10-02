using System;
using System.Collections.Generic;

namespace Pyramid.Levels
{
    // Sin MonoBehaviour: se puede comprobar el orden de ejecución sin una escena.
    public sealed class LevelRunner
    {
        readonly LevelEvent[] events;
        readonly Dictionary<LevelAction, Action<LevelEvent>> handlers = new Dictionary<LevelAction, Action<LevelEvent>>();
        public int NextIndex { get; private set; }
        public bool Stopped { get; private set; }
        public float Distance { get; private set; }

        public LevelRunner(LevelEvent[] events) { this.events = events ?? Array.Empty<LevelEvent>(); }
        public void Register(LevelAction action, Action<LevelEvent> handler) => handlers[action] = handler;
        public void Stop() => Stopped = true;
        public void Advance(float distance)
        {
            if (Stopped) return;
            Distance = Math.Max(Distance, distance);
            while (!Stopped && NextIndex < events.Length && events[NextIndex].at <= Distance)
            {
                var item = events[NextIndex];
                if (!handlers.TryGetValue(item.action, out var handler))
                    throw new InvalidOperationException($"Evento '{item.id}': acción sin ejecutor {item.action}.");
                handler(item);
                NextIndex++;
            }
        }
    }
}
