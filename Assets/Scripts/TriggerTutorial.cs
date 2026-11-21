using UnityEngine;

public class TriggerTutorial : MonoBehaviour
{
    [Header("Secuencia de Diapositivas en este Trigger")]
    [SerializeField] private DiapositivaTutorial[] diapositivas;

    [Header("Opciones")]
    [SerializeField] private bool activarSoloUnaVez = true;

    private bool activado;

    private void OnTriggerEnter(Collider other)
    {
        if (activarSoloUnaVez && activado) return;

        bool esJugador = other.CompareTag("Player") ||
                         other.GetComponentInParent<Player_rieles>() != null ||
                         other.GetComponentInParent<Player_mundolibre>() != null ||
                         other.GetComponentInParent<Pyramid.Levels.Health>() != null;

        if (!esJugador) return;

        activado = true;

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.MostrarTutorial(diapositivas);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
