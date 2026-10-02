using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pyramid.Levels
{
    [Serializable]
    public sealed class EntitySettings
    {
        public float health = 40;
        public float damage = 10;
        public float fireRate = 1;
        public float range = 50;
        public float lifetime = 20;
        public float speed = 5;
        public string behaviour = "estacionaria";
        public List<EntityParameter> extra = new List<EntityParameter>();
        public string Get(string key, string fallback = null) => extra.Find(x => x.key == key)?.value ?? fallback;
        public EntitySettings Copy()
        {
            var result = (EntitySettings)MemberwiseClone();
            result.extra = extra.ConvertAll(x => new EntityParameter { key = x.key, value = x.value });
            return result;
        }
    }

    [Serializable]
    public sealed class EntityParameter { public string key; public string value; }

    [CreateAssetMenu(menuName = "Pyramid/Niveles/Entidad")]
    public sealed class EntityDefinition : ContentDefinition
    {
        public GameObject prefab;
        public EntitySettings defaults = new EntitySettings();
    }
}
