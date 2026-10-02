using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pyramid.Levels.Editor
{
    [InitializeOnLoad]
    public static class LevelPlayModeChecks
    {
        const string Pending = "Pyramid.Levels.PlayCheck.Pending";
        static double deadline, mark;
        static int step;
        static Vector3 pausedPosition;
        static bool batch;
        static string runtimeFault;

        static LevelPlayModeChecks()
        {
            EditorApplication.playModeStateChanged += OnMode;
            if (SessionState.GetBool(Pending, false) && EditorApplication.isPlaying) BeginWatching();
        }
        [MenuItem("Pyramid/Niveles/Prueba integrada de pausa, streaming y reinicio")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play antes de ejecutar las pruebas.");
            LevelExampleSetup.CreateExample();
            EditorSceneManager.OpenScene(LevelExampleSetup.ScenePath);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        static void OnMode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) BeginWatching();
        }
        static void BeginWatching()
        {
            batch = Application.isBatchMode;
            runtimeFault = null;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            step = 0;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (!EditorApplication.isPlaying) { End(false, "Se abandonó Play durante la prueba."); return; }
            try
            {
                if (runtimeFault != null) throw new Exception(runtimeFault);
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Tiempo agotado.");
                var session = LevelSession.Active ? LevelSession.Active : (step == 7 ? UnityEngine.Object.FindAnyObjectByType<LevelSession>() : null);
                if (!session) return;
                if (session.State == LevelState.Failed) throw new Exception(session.LastError);
                switch (step)
                {
                    case 0:
                        if (session.State != LevelState.Running) return;
                        session.PauseLevel();
                        pausedPosition = session.player.position;
                        mark = EditorApplication.timeSinceStartup;
                        step++;
                        break;
                    case 1:
                        if (EditorApplication.timeSinceStartup - mark < 0.25) return;
                        if ((session.player.position - pausedPosition).sqrMagnitude > 0.001f) throw new Exception("El jugador se movió durante la pausa.");
                        session.ResumeLevel(); step++; break;
                    case 2:
                        if (session.State != LevelState.Running) return;
                        Move(session, 150); step++; break;
                    case 3:
                        if (session.Distance < 150) return;
                        // Reinicia mientras el siguiente tramo puede seguir cargando.
                        session.RestartLevel(); step++; break;
                    case 4:
                        if (session.State != LevelState.Running) return;
                        if (session.Distance > 10 || session.NextEventIndex != 0) throw new Exception("El reinicio no restableció progreso y eventos.");
                        Move(session, 150); step++; break;
                    case 5:
                        if (!SceneManager.GetSceneByPath(LevelExampleSetup.Folder + "/Interior.unity").isLoaded) return;
                        Move(session, 420); step++; break;
                    case 6:
                        if (session.Distance < 420 || SceneManager.GetSceneByPath(LevelExampleSetup.Folder + "/Exterior.unity").isLoaded) return;
                        if (session.EntityCount == 0) throw new Exception("No se generaron entidades.");
                        session.StopLevel(); step++; break;
                    case 7:
                        break;
                }
            }
            catch (Exception error) { End(false, error.Message); }
            // StopLevel limpia Active; comprobar mediante el componente de la escena.
            if (step == 7)
            {
                var session = UnityEngine.Object.FindAnyObjectByType<LevelSession>();
                if (!session || session.State != LevelState.Idle) return;
                bool loaded = SceneManager.GetSceneByPath(LevelExampleSetup.Folder + "/Exterior.unity").isLoaded || SceneManager.GetSceneByPath(LevelExampleSetup.Folder + "/Interior.unity").isLoaded;
                if (loaded || session.EntityCount != 0 || Time.timeScale <= 0) End(false, "La limpieza dejó tramos, entidades o pausa activos.");
                else End(true, "Pausa, carga aditiva, reinicio durante carga y descarga comprobados.");
            }
        }
        static void Move(LevelSession session, float distance)
        {
            var controller = session.player.GetComponent<CharacterController>();
            controller.enabled = false;
            session.player.position = session.transform.position + new Vector3(0, 0, distance);
            controller.enabled = true;
        }
        static void End(bool success, string message)
        {
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Pending, false);
            EditorApplication.update -= Tick;
            if (success) Debug.Log("PYRAMID_PLAY_CHECKS_OK: " + message);
            else Debug.LogError("PYRAMID_PLAY_CHECKS_FAILED: " + message);
            EditorApplication.isPlaying = false;
            if (batch) EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
        }
        static void OnLog(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                && (stack.Contains("Assets/Scripts/") || stack.Contains("Pyramid.Levels.Runtime") || message.StartsWith("Nivel detenido:")))
                runtimeFault = message;
        }
    }
}
