using UnityEngine;

public class TriggerFinNivel : MonoBehaviour
{
    private bool activado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activado) return;

        // Detectar si quien cruzó es la nave del jugador
        bool esJugador = other.CompareTag("Player") ||
                         other.GetComponentInParent<Pyramid.Levels.Health>() != null;

        if (esJugador)
        {
            activado = true;

            // 1. Apagar controles de movimiento si tienes el componente
            var salud = other.GetComponentInParent<Pyramid.Levels.Health>();
            if (salud != null)
            {
                var movimiento = salud.GetComponent<MonoBehaviour>();
                if (movimiento != null && movimiento != salud) movimiento.enabled = false;
            }

            // 2. Encender el panel automáticamente
            if (PanelFinalUI.Instance != null)
            {
                PanelFinalUI.Instance.Mostrar();
            }
            else
            {
                // Fallback por si acaso: buscar en todos los Canvas de la escena
                var panelEncontrado = FindFirstObjectByType<PanelFinalUI>(FindObjectsInactive.Include);
                if (panelEncontrado != null)
                {
                    panelEncontrado.Mostrar();
                }
                else
                {
                    Debug.LogWarning("No se encontró ningún PanelFinalUI en el Canvas.");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}