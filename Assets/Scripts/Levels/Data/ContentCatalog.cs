using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pyramid.Levels
{
    [CreateAssetMenu(menuName = "Pyramid/Niveles/Catálogo")]
    public sealed class ContentCatalog : ScriptableObject
    {
        public ContentDefinition[] entries = Array.Empty<ContentDefinition>();

        public Dictionary<string, ContentDefinition> BuildIndex()
        {
            var index = new Dictionary<string, ContentDefinition>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (!entry || string.IsNullOrWhiteSpace(entry.id))
                    throw new ArgumentException("Catálogo: recurso vacío o sin identificador.");
                if (!index.TryAdd(entry.id, entry))
                    throw new ArgumentException($"Catálogo: identificador duplicado '{entry.id}'.");
            }
            return index;
        }
    }
}
