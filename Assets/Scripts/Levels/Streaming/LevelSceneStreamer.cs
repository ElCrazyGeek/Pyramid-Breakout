using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pyramid.Levels
{
    public enum SegmentStatus { Unloaded, Loading, Ready, Unloading, Retired, Failed }

    public sealed class LevelSceneStreamer : MonoBehaviour
    {
        sealed class Slot
        {
            public LevelSegment data;
            public Scene scene;
            public SegmentStatus status;
        }
        readonly List<Slot> slots = new List<Slot>();
        IRoute route;
        EntitySpawner entities;
        bool accepting;
        int operations;
        public string Error { get; private set; }
        public int ReadyCount => slots.FindAll(x => x.status == SegmentStatus.Ready).Count;

        public void Configure(LevelSegment[] segments, IRoute path, EntitySpawner spawner)
        {
            if (operations != 0 || slots.Count != 0) throw new InvalidOperationException("Hay tramos de la sesión anterior sin limpiar.");
            route = path;
            entities = spawner;
            Error = null;
            accepting = true;
            foreach (var segment in segments) slots.Add(new Slot { data = segment });
        }
        public void Tick(float distance)
        {
            if (!accepting || Error != null) return;
            foreach (var slot in slots)
            {
                if (slot.status == SegmentStatus.Unloaded && distance >= slot.data.preloadAt)
                    StartCoroutine(Load(slot));
            }
        }
        public void RetirePassed(float distance)
        {
            if (!accepting) return;
            foreach (var slot in slots)
                if (slot.status == SegmentStatus.Ready && distance >= slot.data.unloadAt) StartCoroutine(Unload(slot));
        }
        public bool InitialReady()
        {
            foreach (var slot in slots) if (slot.data.preloadAt <= 0 && slot.status != SegmentStatus.Ready) return false;
            return true;
        }
        public bool CanReach(float distance)
        {
            foreach (var slot in slots)
                if (distance >= slot.data.start && distance < slot.data.end && slot.status != SegmentStatus.Ready) return false;
            return true;
        }
        public bool IsReady(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return slots.Exists(x => x.data.id == id && x.status == SegmentStatus.Ready);
        }
        public void Suspend() => accepting = false;
        IEnumerator Load(Slot slot)
        {
            slot.status = SegmentStatus.Loading;
            operations++;
            yield return SceneLoader.Load(slot.data.resource.scenePath, scene => slot.scene = scene, error => Error = error);
            if (slot.scene.IsValid() && slot.scene.isLoaded)
            {
                try
                {
                    SceneSegmentRoot root = null;
                    foreach (var obj in slot.scene.GetRootGameObjects())
                    {
                        var candidate = obj.GetComponent<SceneSegmentRoot>();
                        if (candidate)
                        {
                            if (root) throw new InvalidOperationException("Cada escena de tramo debe tener un solo SceneSegmentRoot.");
                            root = candidate;
                        }
                    }
                    if (!root || !root.content) throw new InvalidOperationException($"Tramo '{slot.data.id}': falta SceneSegmentRoot/content.");
                    // Si se solicitó reinicio durante la carga, no iniciar su contenido.
                    if (accepting) root.Prepare(route, slot.data.start);
                    slot.status = SegmentStatus.Ready;
                }
                catch (Exception error) { Error = error.Message; slot.status = SegmentStatus.Failed; }
            }
            else slot.status = SegmentStatus.Failed;
            operations--;
        }
        IEnumerator Unload(Slot slot)
        {
            slot.status = SegmentStatus.Unloading;
            operations++;
            if (entities) entities.ReleaseSegment(slot.data.id);
            yield return SceneLoader.Unload(slot.scene);
            slot.scene = default;
            slot.status = SegmentStatus.Retired;
            operations--;
        }
        public IEnumerator Clear()
        {
            accepting = false;
            while (operations > 0) yield return null;
            foreach (var slot in slots) yield return SceneLoader.Unload(slot.scene);
            slots.Clear();
            Error = null;
        }
    }
}
