using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Map = Pyramid.Levels.Editor.LevelYamlReader.Map;

namespace Pyramid.Levels.Editor
{
    public static class LevelCompiler
    {
        public static void Compile(string text, string source, LevelDefinition output, Func<string, ContentCatalog> loadCatalog)
        {
            output.valid = false;
            output.events = Array.Empty<LevelEvent>();
            output.segments = Array.Empty<LevelSegment>();
            output.meteoriteZones = Array.Empty<MeteoriteZoneDefinition>();
            var root = LevelYamlReader.Read(text, source);
            if (root.Number("version", 0, true) != 1) throw root.Error("Solo se admite version: 1.");
            output.version = 1;
            output.levelId = root.Text("id", required: true);
            output.length = root.Number("longitud_m", required: true);
            if (output.length <= 0) throw root.Error("longitud_m debe ser mayor que cero.");
            string catalogPath = root.Text("catalogo", required: true);
            var catalog = loadCatalog(catalogPath);
            if (!catalog) throw root.Error($"No existe el catálogo '{catalogPath}'.");
            var index = catalog.BuildIndex();
            var initial = root.Child("inicial");
            if (initial != null)
            {
                output.initialBackground = ResolveOptional<BackgroundDefinition>(initial, "fondo", index);
                output.initialEnvironment = ResolveOptional<EnvironmentDefinition>(initial, "ambientacion", index);
                output.initialMusic = ResolveOptional<AudioDefinition>(initial, "musica", index);
                output.initialAmbience = ResolveOptional<AudioDefinition>(initial, "sonido_ambiental", index);
                initial.Complete();
            }
            var segments = new List<LevelSegment>();
            var segmentIds = new HashSet<string>(StringComparer.Ordinal);
            var scenePaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in root.List("tramos"))
            {
                var segment = new LevelSegment
                {
                    id = entry.Text("id", required: true),
                    resource = Resolve<SceneSegmentDefinition>(entry.Text("recurso", required: true), entry, index),
                    start = entry.Number("inicio_s", required: true),
                    end = entry.Number("fin_s", required: true),
                    preloadAt = entry.Number("precargar_en", required: true),
                    unloadAt = entry.Number("descargar_en", required: true)
                };
                entry.Complete();
                if (!segmentIds.Add(segment.id)) throw entry.Error($"Tramo duplicado '{segment.id}'.");
                if (string.IsNullOrWhiteSpace(segment.resource.scenePath) || !scenePaths.Add(segment.resource.scenePath))
                    throw entry.Error("Cada tramo debe referenciar una escena distinta con ruta no vacía.");
                if (segment.start < 0 || segment.start >= segment.end || segment.end > output.length || segment.preloadAt < 0 || segment.preloadAt > segment.start || segment.unloadAt < segment.end)
                    throw entry.Error($"Tramo '{segment.id}': se requiere 0 <= precargar_en <= inicio_s < fin_s <= longitud_m y descargar_en >= fin_s.");
                segments.Add(segment);
            }
            output.segments = segments.OrderBy(x => x.start).ToArray();
            if (segments.Count > 0)
            {
                float end = 0;
                foreach (var segment in output.segments)
                {
                    if (Mathf.Abs(segment.start - end) > 0.001f) throw root.Error("Los tramos deben cubrir el recorrido sin huecos ni solapamientos.");
                    end = segment.end;
                }
                if (Mathf.Abs(end - output.length) > 0.001f) throw root.Error("Los tramos deben cubrir toda longitud_m.");
            }
            var zones = new List<MeteoriteZoneDefinition>();
            var zoneIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in root.List("zonas_meteoritos"))
            {
                var zone = new MeteoriteZoneDefinition
                {
                    id = entry.Text("id", required: true),
                    start = entry.Number("desde_s", required: true),
                    end = entry.Number("hasta_s", required: true),
                    minInterval = entry.Number("intervalo_min_m", required: true),
                    maxInterval = entry.Number("intervalo_max_m", required: true),
                    anticipation = entry.Number("anticipacion_m", 80),
                    maxActive = ReadPositiveInteger(entry, "max_activos"),
                    lifetime = entry.Number("duracion_s", 15),
                    seed = ReadSeed(entry),
                    segment = entry.Text("tramo")
                };
                if (!zoneIds.Add(zone.id)) throw entry.Error($"Zona de meteoritos duplicada '{zone.id}'.");
                if (zone.start < 0 || zone.start >= zone.end || zone.end > output.length)
                    throw entry.Error($"Zona '{zone.id}': se requiere 0 <= desde_s < hasta_s <= longitud_m.");
                if (zone.minInterval <= 0 || zone.maxInterval < zone.minInterval || zone.anticipation < 0 || zone.lifetime <= 0)
                    throw entry.Error($"Zona '{zone.id}': intervalos y duración deben ser positivos, el máximo debe ser >= al mínimo y anticipacion_m >= 0.");

                var area = entry.Child("area", true);
                zone.xMin = area.Number("x_min", required: true);
                zone.xMax = area.Number("x_max", required: true);
                zone.yMin = area.Number("y_min", required: true);
                zone.yMax = area.Number("y_max", required: true);
                area.Complete();
                if (zone.xMin > zone.xMax || zone.yMin > zone.yMax)
                    throw entry.Error($"Zona '{zone.id}': los límites mínimos del área deben ser menores o iguales a los máximos.");
                if (float.IsInfinity(zone.xMax - zone.xMin) || float.IsInfinity(zone.yMax - zone.yMin))
                    throw entry.Error($"Zona '{zone.id}': el tamaño del área debe ser finito.");

                if (segments.Count > 0)
                {
                    if (string.IsNullOrWhiteSpace(zone.segment)) throw entry.Error($"Zona '{zone.id}': tramo es obligatorio cuando el nivel usa tramos.");
                    var owner = segments.Find(x => x.id == zone.segment);
                    if (owner == null) throw entry.Error($"Tramo desconocido '{zone.segment}'.");
                    if (zone.start < owner.start || zone.end > owner.end)
                        throw entry.Error($"Zona '{zone.id}': su rango debe quedar dentro del tramo '{zone.segment}'.");
                }
                else if (zone.segment != null)
                    throw entry.Error($"Zona '{zone.id}': no puede indicar tramo si el nivel no define tramos.");

                var choices = new List<MeteoriteSpawnChoice>();
                foreach (var choice in entry.List("meteoritos"))
                {
                    var definition = Resolve<EntityDefinition>(choice.Text("recurso", required: true), choice, index);
                    if (!definition.prefab) throw choice.Error($"Entidad '{definition.id}' sin prefab.");
                    float weight = choice.Number("peso", 1);
                    if (weight <= 0) throw choice.Error("peso debe ser mayor que cero.");
                    var settings = definition.defaults.Copy();
                    ValidateSettings(settings, choice);
                    foreach (var adapter in definition.prefab.GetComponentsInChildren<ILevelEntityAdapter>(true))
                    {
                        string error = adapter.ValidateSettings(settings);
                        if (error != null) throw choice.Error($"'{definition.id}': {error}");
                    }
                    choice.Complete();
                    choices.Add(new MeteoriteSpawnChoice { resource = definition, weight = weight });
                }
                if (choices.Count == 0) throw entry.Error($"Zona '{zone.id}': debe incluir al menos un meteorito.");
                zone.meteorites = choices.ToArray();
                entry.Complete();
                zones.Add(zone);
            }
            output.meteoriteZones = zones.ToArray();
            var events = new List<LevelEvent>();
            var eventIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in root.List("eventos"))
            {
                string id = entry.Text("id", required: true);
                if (!eventIds.Add(id)) throw entry.Error($"Evento duplicado '{id}'.");
                if (!entry.Boolean("habilitado", true)) continue;
                var item = new LevelEvent { id = id, at = entry.Number("en", required: true) };
                if (item.at < 0 || item.at > output.length) throw entry.Error($"Evento '{id}': en está fuera del recorrido.");
                string action = entry.Text("accion", required: true);
                switch (action)
                {
                    case "crear":
                        item.action = LevelAction.Spawn;
                        var definition = Resolve<EntityDefinition>(entry.Text("recurso", required: true), entry, index);
                        if (!definition.prefab) throw entry.Error($"Entidad '{definition.id}' sin prefab.");
                        item.resource = definition;
                        item.settings = definition.defaults.Copy();
                        var position = entry.Child("posicion", true);
                        item.routePosition = new Vector3(position.Number("x"), position.Number("y"), position.Number("s", required: true));
                        position.Complete();
                        if (item.routePosition.z < 0 || item.routePosition.z > output.length) throw entry.Error($"Evento '{id}': s está fuera del recorrido.");
                        item.segment = entry.Text("tramo");
                        if (item.segment != null)
                        {
                            var owner = segments.Find(x => x.id == item.segment);
                            if (owner == null) throw entry.Error($"Tramo desconocido '{item.segment}'.");
                            if (item.at < owner.preloadAt || item.at >= owner.unloadAt) throw entry.Error($"Evento '{id}': el tramo no estará disponible al crear la entidad.");
                            if (item.routePosition.z < owner.start || item.routePosition.z > owner.end) throw entry.Error($"Evento '{id}': posición fuera de su tramo.");
                        }
                        ApplySettings(entry.Child("parametros"), definition, item.settings);
                        ValidateSettings(item.settings, entry);
                        foreach (var adapter in definition.prefab.GetComponentsInChildren<ILevelEntityAdapter>(true))
                        {
                            string error = adapter.ValidateSettings(item.settings);
                            if (error != null) throw entry.Error($"'{definition.id}': {error}");
                        }
                        break;
                    case "fondo": item.action = LevelAction.Background; item.resource = Resolve<BackgroundDefinition>(entry.Text("recurso", required: true), entry, index); break;
                    case "ambientacion": item.action = LevelAction.Environment; item.resource = Resolve<EnvironmentDefinition>(entry.Text("recurso", required: true), entry, index); break;
                    case "musica": item.action = LevelAction.Music; item.resource = Resolve<AudioDefinition>(entry.Text("recurso", required: true), entry, index); break;
                    case "sonido_ambiental": item.action = LevelAction.Ambience; item.resource = Resolve<AudioDefinition>(entry.Text("recurso", required: true), entry, index); break;
                    case "finalizar": item.action = LevelAction.Finish; break;
                    default: throw entry.Error($"Evento '{id}': acción desconocida '{action}'.");
                }
                if (item.action != LevelAction.Spawn && item.action != LevelAction.Finish)
                {
                    item.transition = entry.Number("transicion_s");
                    if (item.transition < 0) throw entry.Error("transicion_s no puede ser negativo.");
                }
                entry.Complete();
                events.Add(item);
            }
            root.Complete();
            output.events = events.OrderBy(x => x.at).ToArray(); // OrderBy mantiene orden original para distancias iguales.
            int finish = Array.FindIndex(output.events, x => x.action == LevelAction.Finish);
            if (finish >= 0 && finish != output.events.Length - 1) throw root.Error("finalizar debe ser el último evento ejecutado.");
            output.diagnostics = "Nivel válido.";
            output.valid = true;
        }
        static T ResolveOptional<T>(Map map, string key, Dictionary<string, ContentDefinition> index) where T : ContentDefinition
        {
            var id = map.Text(key);
            return id == null ? null : Resolve<T>(id, map, index);
        }
        static T Resolve<T>(string id, Map map, Dictionary<string, ContentDefinition> index) where T : ContentDefinition
        {
            if (!index.TryGetValue(id, out var value)) throw map.Error($"No existe el recurso '{id}'.");
            if (!(value is T result)) throw map.Error($"'{id}' no es un recurso {typeof(T).Name}.");
            if (result is AudioDefinition audio && !audio.clip) throw map.Error($"'{id}' no tiene AudioClip.");
            return result;
        }
        static void ApplySettings(Map map, EntityDefinition definition, EntitySettings settings)
        {
            if (map == null) return;
            var adapters = definition.prefab.GetComponentsInChildren<ILevelEntityAdapter>(true);
            foreach (string key in map.Keys)
            {
                bool supported = key == "duracion_s" || (key == "vida" && definition.prefab.GetComponent<Health>()) || adapters.Any(x => x.SupportsParameter(key));
                if (!supported) throw map.Error($"'{definition.id}' no admite el parámetro '{key}'.");
                switch (key)
                {
                    case "vida": settings.health = map.Number(key); break;
                    case "dano": settings.damage = map.Number(key); break;
                    case "cadencia": settings.fireRate = map.Number(key); break;
                    case "alcance": settings.range = map.Number(key); break;
                    case "duracion_s": settings.lifetime = map.Number(key); break;
                    case "velocidad": settings.speed = map.Number(key); break;
                    case "comportamiento": settings.behaviour = map.Text(key); break;
                    default:
                        settings.extra.RemoveAll(x => x.key == key);
                        settings.extra.Add(new EntityParameter { key = key, value = map.Text(key) });
                        break;
                }
            }
            map.Complete();
        }
        static void ValidateSettings(EntitySettings value, Map map)
        {
            var values = new[] { value.health, value.damage, value.fireRate, value.range, value.lifetime, value.speed };
            if (values.Any(x => float.IsNaN(x) || float.IsInfinity(x) || x < 0) || value.health <= 0)
                throw map.Error("Parámetros inválidos: vida > 0; daño, cadencia, alcance, duración y velocidad >= 0.");
        }
        static int ReadPositiveInteger(Map map, string key)
        {
            double value = map.Number(key, required: true);
            if (value < 1 || value > int.MaxValue || value != Math.Floor(value)) throw map.Error($"'{key}' debe ser un entero positivo.");
            return (int)value;
        }
        static int ReadSeed(Map map)
        {
            double value = map.Number("semilla");
            if (value == 0) return 0;
            if (value < int.MinValue || value > int.MaxValue || value != Math.Floor(value)) throw map.Error("'semilla' debe ser un entero dentro del rango de 32 bits.");
            return (int)value;
        }
    }
}
