using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Pyramid.Levels.Editor
{
    // Se usan nodos para conservar líneas y rechazar campos desconocidos.
    public sealed class LevelYamlReader
    {
        public sealed class Map
        {
            readonly Dictionary<string, YamlNode> fields = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
            readonly HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            readonly string source;
            readonly YamlNode node;
            public Map(YamlNode node, string source)
            {
                this.node = node; this.source = source;
                if (!(node is YamlMappingNode mapping)) throw Error("Se esperaba una lista de propiedades.");
                foreach (var pair in mapping.Children)
                {
                    if (!(pair.Key is YamlScalarNode key) || key.Value == null) throw Error("La propiedad debe tener un nombre.");
                    if (!fields.TryAdd(key.Value, pair.Value)) throw Error($"Propiedad duplicada '{key.Value}'.");
                }
            }
            public Exception Error(string message) => new FormatException($"{source}:{node.Start.Line}:{node.Start.Column}: {message}");
            YamlNode Take(string key, bool required)
            {
                used.Add(key);
                if (fields.TryGetValue(key, out var value)) return value;
                if (required) throw Error($"Falta '{key}'.");
                return null;
            }
            public string Text(string key, string fallback = null, bool required = false)
            {
                var value = Take(key, required);
                if (value == null) return fallback;
                if (!(value is YamlScalarNode scalar) || string.IsNullOrWhiteSpace(scalar.Value)) throw Error($"'{key}' debe ser texto no vacío.");
                return scalar.Value;
            }
            public float Number(string key, float fallback = 0, bool required = false)
            {
                var text = Text(key, null, required);
                if (text == null) return fallback;
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || float.IsNaN(value) || float.IsInfinity(value))
                    throw Error($"'{key}' debe ser un número finito con punto decimal.");
                return value;
            }
            public bool Boolean(string key, bool fallback)
            {
                var text = Text(key);
                if (text == null) return fallback;
                if (!bool.TryParse(text, out var result)) throw Error($"'{key}' debe ser true o false.");
                return result;
            }
            public Map Child(string key, bool required = false)
            {
                var value = Take(key, required);
                return value == null ? null : new Map(value, source);
            }
            public List<Map> List(string key)
            {
                var value = Take(key, false);
                var result = new List<Map>();
                if (value == null) return result;
                if (!(value is YamlSequenceNode sequence)) throw Error($"'{key}' debe ser una lista con guiones.");
                foreach (var child in sequence.Children) result.Add(new Map(child, source));
                return result;
            }
            public IEnumerable<string> Keys => fields.Keys;
            public void Complete()
            {
                foreach (var key in fields.Keys) if (!used.Contains(key)) throw Error($"Propiedad desconocida '{key}'.");
            }
        }
        public static Map Read(string text, string source)
        {
            var yaml = new YamlStream();
            try { yaml.Load(new StringReader(text)); }
            catch (Exception error) { throw new FormatException($"{source}: YAML inválido: {error.Message}", error); }
            if (yaml.Documents.Count != 1) throw new FormatException($"{source}: debe haber exactamente un documento YAML.");
            return new Map(yaml.Documents[0].RootNode, source);
        }
    }
}
