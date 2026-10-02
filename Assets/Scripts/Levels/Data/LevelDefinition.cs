using System;
using UnityEngine;

namespace Pyramid.Levels
{
    public enum LevelAction { Spawn, Background, Environment, Music, Ambience, Finish }

    [Serializable]
    public sealed class LevelEvent
    {
        public string id;
        public float at;
        public LevelAction action;
        public ContentDefinition resource;
        public Vector3 routePosition; // x = lateral, y = vertical, z = distancia s
        public EntitySettings settings;
        public string segment;
        public float transition;
    }

    [Serializable]
    public sealed class LevelSegment
    {
        public string id;
        public SceneSegmentDefinition resource;
        public float start, end, preloadAt, unloadAt;
    }

    public sealed class LevelDefinition : ScriptableObject
    {
        public int version = 1;
        public string levelId;
        public float length;
        public BackgroundDefinition initialBackground;
        public EnvironmentDefinition initialEnvironment;
        public AudioDefinition initialMusic;
        public AudioDefinition initialAmbience;
        public LevelEvent[] events = Array.Empty<LevelEvent>();
        public LevelSegment[] segments = Array.Empty<LevelSegment>();
        [HideInInspector] public bool valid;
        [HideInInspector] public string diagnostics;
        [HideInInspector] public string sourceHash;
    }
}
