using UnityEditor;
using UnityEngine;

namespace Pyramid.Levels.Editor
{
    [CustomEditor(typeof(LevelSession))]
    public sealed class LevelSessionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var session = (LevelSession)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Validar / reimportar archivo"))
            {
                if (session.level)
                {
                    string path = AssetDatabase.GetAssetPath(session.level);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    session.level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                    if (session.level && session.level.valid) { LevelBuildValidator.ValidateScenes(session.level); Debug.Log("Nivel válido.", session); }
                }
            }
            if (!Application.isPlaying) return;
            EditorGUILayout.LabelField("Estado", session.State.ToString());
            EditorGUILayout.LabelField("Progreso", $"{session.Distance:F1} m · evento {session.NextEventIndex} · entidades {session.EntityCount}");
            if (!string.IsNullOrEmpty(session.LastError)) EditorGUILayout.HelpBox(session.LastError, MessageType.Error);
            if (GUILayout.Button("Iniciar / reiniciar recargando"))
            {
                if (session.level)
                {
                    string path = AssetDatabase.GetAssetPath(session.level);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    session.level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                }
                session.RestartLevel();
            }
            if (GUILayout.Button(session.State == LevelState.Paused ? "Continuar" : "Pausar"))
            { if (session.State == LevelState.Paused) session.ResumeLevel(); else session.PauseLevel(); }
            if (GUILayout.Button("Detener y limpiar")) session.StopLevel();
            Repaint();
        }
    }
}
