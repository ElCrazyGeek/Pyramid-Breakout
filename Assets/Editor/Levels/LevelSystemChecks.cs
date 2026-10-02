using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pyramid.Levels.Editor
{
    public static class LevelSystemChecks
    {
        static int assertions;
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception("Prueba fallida: " + message);
        }
        static void Reject(Action action, string message)
        {
            bool rejected = false;
            try { action(); } catch (FormatException) { rejected = true; } catch (ArgumentException) { rejected = true; }
            Check(rejected, message);
        }

        [MenuItem("Pyramid/Niveles/Pruebas de datos y ejecución")]
        public static void RunAll()
        {
            assertions = 0;
            var prefab = new GameObject("Test entity");
            prefab.SetActive(false);
            prefab.AddComponent<Health>();
            prefab.AddComponent<Torreta>();
            prefab.AddComponent<TorretaLevelAdapter>();
            var definition = ScriptableObject.CreateInstance<EntityDefinition>();
            definition.id = "enemigo.test";
            definition.prefab = prefab;
            var catalog = ScriptableObject.CreateInstance<ContentCatalog>();
            catalog.entries = new ContentDefinition[] { definition };
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                string header = "version: 1\nid: prueba\ncatalogo: test\nlongitud_m: 100\n";
                string spawn = "eventos:\n  - id: uno\n    en: 10\n    accion: crear\n    recurso: enemigo.test\n    posicion: {s: 30, x: -4, y: 2}\n    parametros: {vida: 12, cadencia: 2}\n";
                Action<string> compile = text => LevelCompiler.Compile(text, "test.level", level, _ => catalog);
                compile(header + spawn);
                Check(level.valid && level.events.Length == 1, "Importación del nivel.");
                Check(level.events[0].routePosition == new Vector3(-4, 2, 30), "Conversión de coordenadas.");
                Check(level.events[0].settings.health == 12 && definition.defaults.health == 40, "Los overrides no mutan el catálogo.");
                var a = level.events[0].settings.Copy();
                a.extra.Add(new EntityParameter { key = "test", value = "x" });
                Check(level.events[0].settings.extra.Count == 0, "Copias independientes de configuración.");
                compile(header + spawn + "  - id: futuro\n    habilitado: false\n    recurso: aun_no_existe\n");
                Check(level.events.Length == 1, "Contenido pendiente deshabilitado.");
                Reject(() => compile(header + spawn.Replace("enemigo.test", "desconocido")), "Recurso desconocido.");
                Reject(() => compile(header + spawn.Replace("cadencia: 2", "cadensia: 2")), "Parámetro desconocido.");
                Reject(() => compile(header + spawn.Replace("vida: 12", "vida: -3")), "Salud inválida.");
                Reject(() => compile(header + spawn.Replace("cadencia: 2", "cadencia: .nan")), "Valores no finitos.");
                Reject(() => compile(header + spawn.Replace("cadencia: 2", "comportamiento: invisible")), "Validación del adaptador.");
                Reject(() => compile(header + spawn.Replace("en: 10", "en: 101")), "Eventos fuera del recorrido.");
                Reject(() => compile(header + spawn + "  - id: uno\n    habilitado: false\n"), "ID duplicado.");
                Reject(() => compile(header + "eventoss: []\n"), "Campo desconocido.");
                Reject(() => compile(header.Replace("version: 1", "version: 2") + spawn), "Versión desconocida.");
                Reject(() => compile(header + "eventos: [\n"), "YAML inválido.");
                Reject(() => compile(header + "eventos:\n  - id: fin\n    en: 1\n    accion: finalizar\n  - id: segundo\n    en: 2\n    accion: finalizar\n"), "Eventos posteriores al final.");
                compile(header + "eventos:\n  - id: b\n    en: 20\n    accion: crear\n    recurso: enemigo.test\n    posicion: {s: 40}\n  - id: a\n    en: 10\n    accion: crear\n    recurso: enemigo.test\n    posicion: {s: 30}\n  - id: c\n    en: 20\n    accion: finalizar\n");
                Check(level.events[0].id == "a" && level.events[1].id == "b" && level.events[2].id == "c", "Orden estable por distancia.");
                var ran = new List<string>();
                var runner = new LevelRunner(level.events);
                runner.Register(LevelAction.Spawn, e => ran.Add(e.id));
                runner.Register(LevelAction.Finish, e => { ran.Add(e.id); runner.Stop(); });
                runner.Advance(5);
                Check(ran.Count == 0, "No adelantar eventos.");
                runner.Advance(25);
                Check(string.Join(",", ran) == "a,b,c", "Un salto ejecuta todos los eventos alcanzados.");
                runner.Advance(0); runner.Advance(100);
                Check(ran.Count == 3 && runner.Stopped, "Final y retroceso no repiten eventos.");
                var reset = new LevelRunner(level.events);
                reset.Register(LevelAction.Spawn, e => ran.Add(e.id));
                reset.Advance(10);
                Check(ran.Count == 4, "Nueva sesión vuelve a ejecutar eventos.");
                catalog.entries = new ContentDefinition[] { definition, definition };
                Reject(() => catalog.BuildIndex(), "Catálogo con duplicados.");
                Debug.Log($"PYRAMID_LEVEL_CHECKS_OK: {assertions} comprobaciones.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        public static void BuildExampleAndCheck()
        {
            try { RunAll(); LevelExampleSetup.CreateExample(); LevelBuildValidator.ValidateAll(); }
            catch (Exception error) { Debug.LogException(error); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
        }

        [MenuItem("Pyramid/Niveles/Comprobar scripts de la build")]
        public static void VerifyPlayerCompilation()
        {
            BuildExampleAndCheck();
            var settings = new UnityEditor.Build.Player.ScriptCompilationSettings
            {
                target = BuildTarget.StandaloneOSX,
                group = BuildTargetGroup.Standalone,
                options = UnityEditor.Build.Player.ScriptCompilationOptions.None
            };
            var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, "Temp/LevelPlayerChecks");
            if (result.assemblies == null || !result.assemblies.Any(x => x.EndsWith("Assembly-CSharp.dll")))
                throw new Exception("No se pudo compilar el código del jugador.");
            if (result.assemblies.Any(x => x.Contains("YamlDotNet") || x.Contains("Assembly-CSharp-Editor")))
                throw new Exception("La build contiene código exclusivo del Editor.");
            result.typeDB?.Dispose();
            Debug.Log("PYRAMID_PLAYER_COMPILE_OK: scripts del jugador compilados para macOS.");
        }
    }
}
