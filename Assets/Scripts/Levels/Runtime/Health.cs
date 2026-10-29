using UnityEngine;
using UnityEngine.UI;

namespace Pyramid.Levels
{
    public interface IDamageable { void TakeDamage(float amount); }

    public sealed class Health : MonoBehaviour, IDamageable
    {
        [Min(0.01f)] public float maximum = 40;
        public float Current { get; private set; }
        bool initialized;

        [Tooltip("Imagen de la barra. Se rellena de izquierda a derecha y se vacía desde la derecha.")]
        public Image healthBar;

        void Awake()
        {
            ConfigureHealthBar();
            if (!initialized) ResetHealth(maximum);
            else UpdateHealthBar();
        }

        public void ResetHealth(float value)
        {
            maximum = value;
            Current = value;
            initialized = true;
            UpdateHealthBar();
        }

        void ConfigureHealthBar()
        {
            if (healthBar == null) return;

            healthBar.type = Image.Type.Filled;
            healthBar.fillMethod = Image.FillMethod.Horizontal;
            healthBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        void UpdateHealthBar()
        {
            if (healthBar == null) return;

            healthBar.fillAmount = maximum > 0f ? Mathf.Clamp01(Current / maximum) : 0f;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0 || Current <= 0) return;

            Current = Mathf.Max(0, Current - amount);
            Debug.Log($"[Health] {gameObject.name} recibió {amount} de daño. Salud actual: {Current}/{maximum}");
            UpdateHealthBar();

            if (Current > 0) return;

            // Buscar sesión de nivel asociada SOLO si este Health pertenece al jugador (o es hijo suyo).
            LevelSession session = null;
            // Comprobar sesión en padres: puede haber una LevelSession como root del sistema de niveles,
            // pero no debe usarse para indicar "jugador" salvo que este Health esté en el player.
            var parentSession = GetComponentInParent<LevelSession>();
            if (parentSession != null && parentSession.player != null)
            {
                if (transform == parentSession.player || transform.IsChildOf(parentSession.player))
                    session = parentSession;
            }
            // Como fallback, comprobar LevelSession.Active (por si el jugador no está bajo el mismo parent)
            if (session == null && LevelSession.Active != null && LevelSession.Active.player != null)
            {
                var playerTransform = LevelSession.Active.player;
                if (transform == playerTransform || transform.IsChildOf(playerTransform)) session = LevelSession.Active;
            }

            if (session != null)
            {
                // Si corresponde al jugador, marcar fallo de nivel
                session.Fail("El jugador ha sido destruido.");
            }
            else if (TryGetComponent<LevelEntity>(out var entity))
            {
                // Entidad del nivel: liberar/reciclar
                entity.Release();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        // Método para reparar/curar la entidad.
        // Suma 'amount' a la salud actual hasta el máximo. No ejecuta lógica de muerte.
        public void RepairDamage(float amount)
        {
            if (amount <= 0f) return;

            if (!initialized) ResetHealth(maximum);

            Current = Mathf.Min(maximum, Current + amount);
            Debug.Log($"[Health] {gameObject.name} reparado {amount}. Salud actual: {Current}/{maximum}");
            UpdateHealthBar();
        }
    }
}
