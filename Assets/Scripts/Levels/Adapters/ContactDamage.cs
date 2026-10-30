using System.Collections;
using UnityEngine;

namespace Pyramid.Levels
{
    public sealed class ContactDamage : MonoBehaviour, ILevelEntityAdapter
    {
        [Header("Daño de Impacto")]
        public float damage = 10;
        [Tooltip("Tiempo de invulnerabilidad / retraso entre golpes del mismo obstáculo")]
        [SerializeField] private float cooldownGolpe = 0.6f;
        float nextHit;

        [Header("Empuje al Centro (Star Fox Bounce)")]
        [Tooltip("Fuerza con la que empuja la nave hacia el centro al chocar")]
        [SerializeField] private float fuerzaEmpuje = 15f;
        [Tooltip("Duración en segundos del empujón suave si no usa física")]
        [SerializeField] private float duracionEmpuje = 0.2f;

        public bool SupportsParameter(string key) => key == "dano";
        public string ValidateSettings(EntitySettings settings) => null;
        public void Configure(EntitySettings settings, Transform player, Camera camera)
        {
            damage = settings.damage;
            nextHit = 0;
        }

        public void Begin() { enabled = true; }
        public void End() { enabled = false; }

        // Colisiones nativas de Unity (triggers y sólidas)
        private void OnTriggerEnter(Collider other) => Hit(other.gameObject);
        private void OnCollisionEnter(Collision collision) => Hit(collision.gameObject);
        private void OnControllerColliderHit(ControllerColliderHit hit) => Hit(hit.gameObject);

        // Método público que Player_rieles.cs llama en su línea 78
        public void Hit(GameObject other)
        {
            if (!enabled || Time.timeScale <= 0 || Time.time < nextHit) return;

            // 1. Detectar si el objeto o cualquiera de sus padres/hijos es el jugador
            Transform rootJugador = ObtenerRootJugador(other);
            if (rootJugador == null) return;

            nextHit = Time.time + cooldownGolpe;

            // 2. Aplicar el daño
            var damageable = rootJugador.GetComponentInChildren<IDamageable>() ?? rootJugador.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);

            // 3. Empujar la nave hacia el centro
            EmpujarHaciaElCentro(rootJugador);
        }

        private Transform ObtenerRootJugador(GameObject obj)
        {
            if (obj.CompareTag("Player")) return obj.transform;

            var parent = obj.GetComponentInParent<IDamageable>() as Component;
            if (parent != null && parent.CompareTag("Player")) return parent.transform;

            if (LevelSession.Active != null && LevelSession.Active.player != null)
            {
                if (obj.transform == LevelSession.Active.player || obj.transform.IsChildOf(LevelSession.Active.player))
                {
                    return LevelSession.Active.player;
                }
            }

            return null;
        }

        private void EmpujarHaciaElCentro(Transform naveTransform)
        {
            // Vector hacia el centro (0, 0) en coordenadas locales del riel
            Vector3 posLocal = naveTransform.localPosition;
            Vector3 direccionAlCentro = new Vector3(-posLocal.x, -posLocal.y, 0f).normalized;

            if (direccionAlCentro.sqrMagnitude < 0.001f)
            {
                Vector3 direccionMundial = (naveTransform.position - transform.position).normalized;
                direccionAlCentro = naveTransform.InverseTransformDirection(direccionMundial);
                direccionAlCentro.z = 0f;
                direccionAlCentro.Normalize();
            }

            if (naveTransform.TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
            {
                Vector3 fuerzaMundial = naveTransform.TransformDirection(direccionAlCentro) * fuerzaEmpuje;
                rb.AddForce(fuerzaMundial, ForceMode.VelocityChange);
            }
            else
            {
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(RutinaEmpujon(naveTransform, direccionAlCentro));
                }
            }
        }

        private IEnumerator RutinaEmpujon(Transform nave, Vector3 dirLocal)
        {
            float tiempo = 0f;
            while (tiempo < duracionEmpuje && nave != null)
            {
                nave.localPosition += dirLocal * fuerzaEmpuje * Time.deltaTime;
                tiempo += Time.deltaTime;
                yield return null;
            }
        }
    }
}