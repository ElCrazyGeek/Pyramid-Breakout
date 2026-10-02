using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pyramid.Levels.Editor
{
    public static class LevelExampleSetup
    {
        public const string Folder = "Assets/Levels/Examples/Generated";
        public const string ScenePath = Folder + "/GameRoot.unity";
        public const string LevelPath = Folder + "/Entrada.level";

        [MenuItem("Pyramid/Niveles/Crear ejemplo jugable")]
        public static void CreateExample()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play antes de crear el ejemplo.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
            {
                RegisterScenes();
                Debug.Log($"El ejemplo ya existe en {ScenePath}; no se sobrescribe.");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var catalog = ScriptableObject.CreateInstance<ContentCatalog>();
            var turret = Create<EntityDefinition>("Torreta", "enemigo.torreta");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Prefabs/Torreta_Base.prefab");
            if (!original) throw new InvalidOperationException("Falta el prefab Torreta_Base.");
            var copy = UnityEngine.Object.Instantiate(original);
            copy.name = "TorretaNivel";
            copy.tag = "Enemy";
            if (!copy.GetComponent<Health>()) copy.AddComponent<Health>();
            if (!copy.GetComponent<TorretaLevelAdapter>()) copy.AddComponent<TorretaLevelAdapter>();
            turret.prefab = PrefabUtility.SaveAsPrefabAsset(copy, Folder + "/TorretaNivel.prefab");
            UnityEngine.Object.DestroyImmediate(copy);
            turret.defaults.range = 120;
            turret.defaults.damage = 5;
            turret.defaults.lifetime = 12;
            Save(turret);

            var wall = Create<EntityDefinition>("Muro", "obstaculo.muro");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "MuroDestructible";
            cube.transform.localScale = new Vector3(4, 8, 3);
            cube.AddComponent<Health>();
            var contact = cube.AddComponent<ContactDamage>();
            contact.damage = 10;
            wall.prefab = PrefabUtility.SaveAsPrefabAsset(cube, Folder + "/Muro.prefab");
            UnityEngine.Object.DestroyImmediate(cube);
            wall.defaults.health = 10;
            Save(wall);

            var day = Create<BackgroundDefinition>("FondoExterior", "fondo.exterior");
            day.cameraColor = new Color(0.55f, 0.3f, 0.15f);
            Save(day);
            var night = Create<BackgroundDefinition>("FondoInterior", "fondo.interior");
            night.cameraColor = new Color(0.015f, 0.03f, 0.06f);
            Save(night);
            var warm = Create<EnvironmentDefinition>("AmbienteExterior", "ambiente.exterior");
            warm.ambient = new Color(0.5f, 0.35f, 0.2f);
            warm.lightColor = new Color(1, 0.8f, 0.55f);
            Save(warm);
            var cold = Create<EnvironmentDefinition>("AmbienteInterior", "ambiente.interior");
            cold.ambient = new Color(0.1f, 0.16f, 0.25f);
            cold.lightColor = new Color(0.4f, 0.7f, 1);
            cold.fog = true;
            cold.fogColor = night.cameraColor;
            cold.fogDensity = 0.007f;
            Save(cold);
            var musicA = Create<AudioDefinition>("MusicaExterior", "musica.exterior");
            musicA.clip = MakeTone("PruebaMusicalA", 220);
            musicA.volume = 0.12f;
            Save(musicA);
            var musicB = Create<AudioDefinition>("MusicaInterior", "musica.interior");
            musicB.clip = MakeTone("PruebaMusicalB", 146.83f);
            musicB.volume = 0.12f;
            Save(musicB);
            var sound = Create<AudioDefinition>("AmbienteSonoro", "sonido.ambiente");
            sound.clip = MakeTone("PruebaAmbiental", 55);
            sound.volume = 0.025f;
            Save(sound);
            var first = Create<SceneSegmentDefinition>("TramoExterior", "escenario.exterior");
            first.scenePath = Folder + "/Exterior.unity";
            Save(first);
            var second = Create<SceneSegmentDefinition>("TramoInterior", "escenario.interior");
            second.scenePath = Folder + "/Interior.unity";
            Save(second);
            CreateSegment(first.scenePath, false);
            CreateSegment(second.scenePath, true);
            catalog.entries = new ContentDefinition[] { turret, wall, day, night, warm, cold, musicA, musicB, sound, first, second };
            AssetDatabase.CreateAsset(catalog, Folder + "/Catalog.asset");
            foreach (var entry in catalog.entries) EditorUtility.SetDirty(entry);
            AssetDatabase.SaveAssets();

            var builds = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { ScenePath, first.scenePath, second.scenePath })
                if (!builds.Any(x => x.path == path)) builds.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = builds.ToArray();
            string template = File.ReadAllText("Assets/Levels/Examples/Entrada.level.txt");
            File.WriteAllText(LevelPath, template);
            AssetDatabase.ImportAsset(LevelPath, ImportAssetOptions.ForceSynchronousImport);
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            if (!level || !level.valid) throw new InvalidOperationException(level ? level.diagnostics : "No se importó Entrada.level.");

            // Copia independiente: se conserva íntegra la escena original.
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath, true);
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = UnityEngine.Object.FindAnyObjectByType<Player_rieles>();
            var manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var light = UnityEngine.Object.FindObjectsByType<Light>().FirstOrDefault(x => x.type == LightType.Directional);
            if (!player || !manager) throw new InvalidOperationException("SampleScene debe contener jugador de rieles y GameManager.");
            foreach (var root in scene.GetRootGameObjects())
            {
                bool keep = root.GetComponentInChildren<Player_rieles>(true) || root.GetComponentInChildren<GameManager>(true)
                    || root.GetComponentInChildren<Camera>(true) || root.GetComponentInChildren<Light>(true)
                    || root.GetComponentInChildren<Cambiarcamara>(true) || root.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true)
                    || root.GetComponentInChildren<Canvas>(true);
                if (!keep) UnityEngine.Object.DestroyImmediate(root);
            }
            player.transform.position = new Vector3(0, 10, 0);
            player.transform.rotation = Quaternion.identity;
            player.gameObject.tag = "Player";
            player.VelocidadMaxima = 45;
            player.VelocidadAvance = 30;
            var health = player.GetComponent<Health>() ?? player.gameObject.AddComponent<Health>();
            health.maximum = 100;
            var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (input && input.actions != null && input.actions.actionMaps.Count > 0) input.defaultActionMap = input.actions.actionMaps[0].name;
            var host = new GameObject("Sistema de niveles");
            host.transform.position = player.transform.position;
            var session = host.AddComponent<LevelSession>();
            session.level = level;
            session.player = player.transform;
            session.viewCamera = Camera.main;
            session.gameManager = manager;
            session.mainLight = light;
            session.logEvents = true;
            var managerData = new SerializedObject(manager);
            managerData.FindProperty("modoActual").enumValueIndex = (int)ModoVuelo.Riel;
            managerData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
            RegisterScenes();
            AssetDatabase.SaveAssets();
            LevelBuildValidator.ValidateAll();
            Debug.Log($"Ejemplo listo: abre {ScenePath} y pulsa Play. El audio es una señal sintética de prueba.");
        }
        static T Create<T>(string name, string id) where T : ContentDefinition
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.id = id;
            AssetDatabase.CreateAsset(asset, Folder + "/" + name + ".asset");
            return asset;
        }
        static void Save(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
        static void RegisterScenes()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { ScenePath, Folder + "/Exterior.unity", Folder + "/Interior.unity" })
            {
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) continue;
                var entry = new EditorBuildSettingsScene(path, true);
                int index = scenes.FindIndex(x => x.path == path);
                if (index < 0) scenes.Add(entry); else scenes[index] = entry;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        static void CreateSegment(string path, bool interior)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject("Tramo");
            SceneManager.MoveGameObjectToScene(root, scene);
            var marker = root.AddComponent<SceneSegmentRoot>();
            var content = new GameObject("Contenido");
            content.transform.SetParent(root.transform, false);
            marker.content = content;
            MakeCube("Suelo", content.transform, new Vector3(0, -12, 150), new Vector3(100, 2, 300));
            for (int i = 0; i < 6; i++)
            {
                MakeCube("Columna izquierda", content.transform, new Vector3(-40, 0, 25 + i * 50), new Vector3(5, 35, 5));
                MakeCube("Columna derecha", content.transform, new Vector3(40, 0, 25 + i * 50), new Vector3(5, 35, 5));
            }
            if (interior) MakeCube("Techo", content.transform, new Vector3(0, 28, 150), new Vector3(100, 2, 300));
            content.SetActive(false);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }
        static void MakeCube(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
        }
        static AudioClip MakeTone(string name, float frequency)
        {
            string path = Folder + "/" + name + ".wav";
            const int rate = 22050, seconds = 2;
            int samples = rate * seconds;
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                {
                    float envelope = Mathf.Min(1, Mathf.Min(i / 500f, (samples - i - 1) / 500f));
                    writer.Write((short)(Mathf.Sin(2 * Mathf.PI * frequency * i / rate) * 3000 * envelope));
                }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
