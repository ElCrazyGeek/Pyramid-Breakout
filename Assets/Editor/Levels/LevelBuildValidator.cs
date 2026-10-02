using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pyramid.Levels.Editor
{
    public sealed class LevelBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => ValidateAll();

        [MenuItem("Pyramid/Niveles/Validar todos los niveles")]
        public static void ValidateAll()
        {
            int count = 0;
            foreach (var file in Directory.GetFiles("Assets", "*.level", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (!level || !level.valid) throw new BuildFailedException($"{path}: {(level ? level.diagnostics : "No se pudo importar")}");
                ValidateScenes(level);
                count++;
            }
            Debug.Log($"Niveles: {count} archivos validados.");
        }
        public static void ValidateScenes(LevelDefinition level)
        {
            foreach (var segment in level.segments)
            {
                string path = segment.resource.scenePath;
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) throw new BuildFailedException($"Tramo '{segment.id}': no existe {path}.");
                if (!EditorBuildSettings.scenes.Any(x => x.enabled && x.path == path)) throw new BuildFailedException($"Tramo '{segment.id}': añade {path} a Build Settings.");
                var preview = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var roots = preview.GetRootGameObjects();
                    var markers = roots.SelectMany(x => x.GetComponentsInChildren<SceneSegmentRoot>(true)).ToArray();
                    if (roots.Length != 1 || markers.Length != 1 || markers[0].gameObject != roots[0])
                        throw new BuildFailedException($"Tramo '{segment.id}': debe tener una única raíz con SceneSegmentRoot.");
                    var marker = markers[0];
                    if (!marker.content || marker.content == marker.gameObject || !marker.content.transform.IsChildOf(marker.transform) || marker.content.activeSelf)
                        throw new BuildFailedException($"Tramo '{segment.id}': content debe ser un hijo inactivo de la raíz.");
                    if (roots[0].GetComponentInChildren<Camera>(true) || roots[0].GetComponentInChildren<GameManager>(true) || roots[0].GetComponentInChildren<LevelSession>(true))
                        throw new BuildFailedException($"Tramo '{segment.id}': jugador/cámaras/gestores deben permanecer en GameRoot.");
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }
            }
        }
    }
}
