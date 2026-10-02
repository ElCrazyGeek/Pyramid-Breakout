using System;
using System.Collections;
using UnityEngine;

namespace Pyramid.Levels
{
    public enum LevelState { Idle, Preparing, Running, Paused, WaitingForScene, Completed, Failed, Stopping }

    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(EntitySpawner), typeof(LevelSceneStreamer), typeof(LinearRoute))]
    public sealed class LevelSession : MonoBehaviour
    {
        public static LevelSession Active { get; private set; }
        public LevelDefinition level;
        public Transform player;
        public Camera viewCamera;
        public GameManager gameManager;
        public Light mainLight;
        public bool playOnStart = true;
        public bool logEvents;
        [Min(0)] public float boundaryMargin = 5;
        public LevelState State { get; private set; } = LevelState.Idle;
        public string LastError { get; private set; }
        public float Distance => progress.Distance;
        public int NextEventIndex => runner?.NextIndex ?? 0;
        public int EntityCount => spawner ? spawner.Count : 0;
        public LevelObjectRegistry Objects { get; } = new LevelObjectRegistry();

        readonly LevelProgress progress = new LevelProgress();
        LinearRoute route;
        EntitySpawner spawner;
        LevelSceneStreamer streamer;
        BackgroundController background;
        EnvironmentController environment;
        LevelAudioChannel music, ambience;
        LevelRunner runner;
        LevelDefinition activeLevel;
        Player_rieles railPlayer;
        DisparoJugador weapon;
        Vector3 startPosition;
        Quaternion startRotation;
        float originalTimeScale = 1;
        bool captured, ownsTime, wantsStart;
        int revision;
        Coroutine lifecycle;

        void Awake()
        {
            route = GetComponent<LinearRoute>();
            spawner = GetComponent<EntitySpawner>();
            streamer = GetComponent<LevelSceneStreamer>();
            background = GetComponent<BackgroundController>() ?? gameObject.AddComponent<BackgroundController>();
            environment = GetComponent<EnvironmentController>() ?? gameObject.AddComponent<EnvironmentController>();
            music = NewChannel("Música");
            ambience = NewChannel("Sonido ambiental");
        }
        LevelAudioChannel NewChannel(string label)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            return child.AddComponent<LevelAudioChannel>();
        }
        void Start() { if (playOnStart) RestartLevel(); }

        public void RestartLevel() => Request(true);
        public void StopLevel() => Request(false);
        void Request(bool start)
        {
            if (Active && Active != this) { LastError = "Ya existe otra sesión de nivel activa."; State = LevelState.Failed; return; }
            Active = this;
            wantsStart = start;
            revision++;
            runner?.Stop();
            streamer.Suspend();
            SetFrozen(true);
            State = start ? LevelState.Preparing : LevelState.Stopping;
            if (lifecycle == null) lifecycle = StartCoroutine(Rebuild());
        }
        IEnumerator Rebuild()
        {
            // Primer yield: permite almacenar el handle antes de terminar una operación inmediata.
            yield return null;
            while (true)
            {
                int current = revision;
                runner = null;
                spawner.Clear();
                Objects.Clear();
                music.Clear(); ambience.Clear();
                background.Restore(); environment.Restore();
                yield return streamer.Clear();
                if (activeLevel) { Destroy(activeLevel); activeLevel = null; }
                progress.Reset();
                if (current != revision) continue;
                if (!wantsStart)
                {
                    State = LevelState.Idle;
                    ReleaseClock();
                    SetControls(true);
                    Active = null;
                    break;
                }
                LastError = null;
                if (!ValidateSetup(out var error)) { Fail(error); break; }
                activeLevel = Instantiate(level);
                if (!captured)
                {
                    startPosition = player.position;
                    startRotation = player.rotation;
                    captured = true;
                }
                railPlayer = player.GetComponent<Player_rieles>();
                weapon = player.GetComponent<DisparoJugador>();
                if (gameManager) gameManager.CambiarModo(ModoVuelo.Riel);
                ResetPlayer();
                background.Capture(viewCamera);
                environment.directionalLight = mainLight;
                environment.Capture();
                streamer.Configure(activeLevel.segments, route, spawner);
                streamer.Tick(0);
                while (!streamer.InitialReady() && streamer.Error == null && current == revision) yield return null;
                if (current != revision) continue;
                if (streamer.Error != null) { Fail(streamer.Error); break; }
                background.Apply(activeLevel.initialBackground, 0);
                environment.Apply(activeLevel.initialEnvironment, 0);
                music.Play(activeLevel.initialMusic, 0);
                ambience.Play(activeLevel.initialAmbience, 0);
                runner = new LevelRunner(activeLevel.events);
                runner.Register(LevelAction.Spawn, e => spawner.Spawn(e, route, player, viewCamera));
                runner.Register(LevelAction.Background, e => background.Apply((BackgroundDefinition)e.resource, e.transition));
                runner.Register(LevelAction.Environment, e => environment.Apply((EnvironmentDefinition)e.resource, e.transition));
                runner.Register(LevelAction.Music, e => music.Play((AudioDefinition)e.resource, e.transition));
                runner.Register(LevelAction.Ambience, e => ambience.Play((AudioDefinition)e.resource, e.transition));
                runner.Register(LevelAction.Finish, e => Complete());
                State = LevelState.Running;
                SetFrozen(false);
                break;
            }
            lifecycle = null;
        }
        public bool ValidateSetup(out string error)
        {
            error = null;
            if (!level || !level.valid) error = level ? level.diagnostics : "Selecciona un archivo .level válido.";
            else if (!player || !player.GetComponent<Player_rieles>()) error = "Asigna un jugador con Player_rieles.";
            else if (Quaternion.Angle(route.transform.rotation, Quaternion.identity) > 0.01f || (route.transform.lossyScale - Vector3.one).sqrMagnitude > 0.001f)
                error = "El controlador actual requiere un riel recto sobre +Z, sin rotación ni escala.";
            if (!viewCamera) viewCamera = Camera.main;
            if (error == null && !viewCamera) error = "Asigna una cámara de juego.";
            return error == null;
        }
        void ResetPlayer()
        {
            var controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller && controller.enabled;
            if (controller) controller.enabled = false;
            player.SetPositionAndRotation(startPosition, startRotation);
            if (controller) controller.enabled = wasEnabled;
            railPlayer.ResetForLevel(route.transform.position);
            if (weapon) weapon.ResetForLevel();
            if (player.TryGetComponent<Health>(out var health)) health.ResetHealth(health.maximum);
            foreach (var cameraRig in FindObjectsByType<CamaraRiel>()) cameraRig.ResetForLevel(route.transform.position);
            if (gameManager) gameManager.ResetCameras();
        }
        void Update()
        {
            if (State != LevelState.Running && State != LevelState.WaitingForScene) return;
            progress.Sample(route, player.position);
            streamer.Tick(Distance);
            if (streamer.Error != null) { Fail(streamer.Error); return; }
            float nextDistance = Distance + boundaryMargin + railPlayer.VelocidadMaxima * Time.unscaledDeltaTime;
            bool ready = streamer.CanReach(nextDistance);
            for (int i = runner.NextIndex; i < activeLevel.events.Length && activeLevel.events[i].at <= Distance; i++)
            {
                var next = activeLevel.events[i];
                ready &= streamer.IsReady(next.segment);
                if (next.action == LevelAction.Finish) break;
            }
            if (!ready)
            {
                State = LevelState.WaitingForScene;
                SetFrozen(true);
                return;
            }
            if (State == LevelState.WaitingForScene) { State = LevelState.Running; SetFrozen(false); }
            try
            {
                int previous = runner.NextIndex;
                runner.Advance(Distance);
                streamer.RetirePassed(Distance);
                if (logEvents && previous != runner.NextIndex)
                    Debug.Log($"Nivel {activeLevel.levelId}: ejecutados {runner.NextIndex - previous} eventos en {Distance:F1} m.", this);
                if (State == LevelState.Running && Distance >= activeLevel.length) Complete();
            }
            catch (Exception error) { Fail(error.Message); }
        }
        public void PauseLevel()
        {
            if (State != LevelState.Running && State != LevelState.WaitingForScene) return;
            State = LevelState.Paused;
            SetFrozen(true);
        }
        public void ResumeLevel()
        {
            if (State != LevelState.Paused) return;
            // El siguiente Update comprueba el tramo antes de habilitar movimiento.
            State = LevelState.WaitingForScene;
        }
        void Complete() { runner?.Stop(); State = LevelState.Completed; SetFrozen(true); }
        public void Fail(string message)
        {
            runner?.Stop();
            streamer.Suspend();
            LastError = message;
            State = LevelState.Failed;
            SetFrozen(true);
            Debug.LogError($"Nivel detenido: {message}", this);
        }
        void SetControls(bool enabled)
        {
            if (gameManager) gameManager.SetLevelControlsEnabled(enabled);
            else if (player)
            {
                var movement = player.GetComponent<Player_rieles>();
                if (movement) movement.enabled = enabled;
            }
            if (player && player.TryGetComponent<DisparoJugador>(out var gun)) gun.puedeDisparar = enabled;
        }
        void SetFrozen(bool frozen)
        {
            if (!ownsTime) { originalTimeScale = Time.timeScale > 0 ? Time.timeScale : 1; ownsTime = true; }
            Time.timeScale = frozen ? 0 : originalTimeScale;
            SetControls(!frozen);
            if (music) music.SetPaused(frozen);
            if (ambience) ambience.SetPaused(frozen);
        }
        void ReleaseClock() { if (ownsTime) Time.timeScale = originalTimeScale; ownsTime = false; }
        void OnDestroy()
        {
            if (Active != this) return;
            runner?.Stop();
            Objects.Clear();
            if (background) background.Restore();
            if (environment) environment.Restore();
            if (activeLevel) Destroy(activeLevel);
            ReleaseClock();
            Active = null;
        }
    }
}
