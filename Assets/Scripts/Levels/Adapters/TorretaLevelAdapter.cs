using UnityEngine;

namespace Pyramid.Levels
{
    [RequireComponent(typeof(Torreta))]
    public sealed class TorretaLevelAdapter : MonoBehaviour, ILevelEntityAdapter
    {
        Torreta turret;
        public bool SupportsParameter(string key) => key == "dano" || key == "cadencia" || key == "alcance" || key == "velocidad" || key == "comportamiento";
        public string ValidateSettings(EntitySettings settings) => settings.behaviour == "estacionaria" || settings.behaviour == "seguidora" ? null : "La torreta admite comportamiento estacionaria o seguidora.";
        public void Configure(EntitySettings settings, Transform player, Camera camera)
        {
            turret = GetComponent<Torreta>();
            turret.enabled = false;
            turret.objetivo = player;
            turret.camaraReferencia = camera;
            turret.danio = settings.damage;
            turret.cadenciaDisparo = settings.fireRate;
            turret.alcance = settings.range;
            // La duración de LevelEntity es el límite total; permite la retirada antes de ese límite.
            turret.tiempoVida = settings.lifetime > 0 ? Mathf.Max(0.01f, settings.lifetime - turret.tiempoRetirada) : 0;
            turret.velocidadRetirada = settings.speed;
            turret.tipo = settings.behaviour == "seguidora" ? Torreta.TipoTorreta.Seguidora : Torreta.TipoTorreta.Estacionaria;
            turret.ResetForLevel();
        }
        public void Begin() { turret.enabled = true; }
        public void End() { if (turret) { turret.StopAllCoroutines(); turret.enabled = false; } }
    }
}
