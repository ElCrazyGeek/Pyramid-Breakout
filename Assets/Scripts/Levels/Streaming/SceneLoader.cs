using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pyramid.Levels
{
    public static class SceneLoader
    {
        public static IEnumerator Load(string path, Action<Scene> completed, Action<string> failed)
        {
            if (string.IsNullOrWhiteSpace(path) || !Application.CanStreamedLevelBeLoaded(path))
            { failed($"Escena no incluida en Build Settings: '{path}'."); yield break; }
            if (SceneManager.GetSceneByPath(path).isLoaded)
            { failed($"El tramo ya está cargado fuera de esta sesión: '{path}'."); yield break; }
            AsyncOperation operation;
            try { operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive); }
            catch (Exception error) { failed(error.Message); yield break; }
            if (operation == null) { failed($"No se pudo iniciar la carga: {path}."); yield break; }
            yield return operation;
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) failed($"La escena no terminó de cargar: {path}.");
            else completed(scene);
        }
        public static IEnumerator Unload(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation != null) yield return operation;
            }
        }
    }
}
