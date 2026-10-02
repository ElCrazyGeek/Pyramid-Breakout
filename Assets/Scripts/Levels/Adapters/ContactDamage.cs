using UnityEngine;

namespace Pyramid.Levels
{
    public sealed class ContactDamage : MonoBehaviour, ILevelEntityAdapter
    {
        public float damage = 10;
        float nextHit;
        public bool SupportsParameter(string key) => key == "dano";
        public string ValidateSettings(EntitySettings settings) => null;
        public void Configure(EntitySettings settings, Transform player, Camera camera) { damage = settings.damage; nextHit = 0; }
        public void Begin() { enabled = true; }
        public void End() { enabled = false; }
        void OnTriggerEnter(Collider other) => Hit(other.gameObject);
        void OnCollisionEnter(Collision collision) => Hit(collision.gameObject);
        void OnControllerColliderHit(ControllerColliderHit hit) => Hit(hit.gameObject);
        public void Hit(GameObject other)
        {
            if (!enabled || Time.timeScale <= 0 || Time.time < nextHit || !other.CompareTag("Player")) return;
            nextHit = Time.time + 0.5f;
            other.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
        }
    }
}
