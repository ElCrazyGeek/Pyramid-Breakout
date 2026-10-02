using UnityEngine;

namespace Pyramid.Levels
{
    public interface IDamageable { void TakeDamage(float amount); }

    public sealed class Health : MonoBehaviour, IDamageable
    {
        [Min(0.01f)] public float maximum = 40;
        public float Current { get; private set; }
        bool initialized;
        void Awake() { if (!initialized) ResetHealth(maximum); }
        public void ResetHealth(float value) { maximum = value; Current = value; initialized = true; }
        public void TakeDamage(float amount)
        {
            if (amount <= 0 || Current <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            if (Current > 0) return;
            var session = GetComponent<LevelSession>();
            if (!session) session = GetComponentInParent<LevelSession>();
            if (!session && LevelSession.Active && LevelSession.Active.player == transform) session = LevelSession.Active;
            if (session) session.Fail("El jugador ha sido destruido.");
            else if (TryGetComponent<LevelEntity>(out var entity)) entity.Release();
            else Destroy(gameObject);
        }
    }
}
