using UnityEngine;
using UnityEngine.SceneManagement;

public class Collider_cinematicas: MonoBehaviour
{
    [Header("Diálogo de Cierre")]
    [SerializeField] private LineaVN[] dialogoFinal;

    [Header("Siguiente Escena")]
    [SerializeField] private string nombreSiguienteNivel;

    private bool activado = false;



    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Algo entró al trigger: {other.gameObject.name} con Tag: {other.tag}");

        if (activado) return;

        // Detecta tanto por Tag como por script en hijos o padres
        bool esJugador = other.CompareTag("Player") ||
                         other.GetComponentInParent<Movimiento>() != null ||
                         other.GetComponentInChildren<Movimiento>() != null;

        if (esJugador)
        {
            Debug.Log("¡Nave detectada con éxito!");
            activado = true;

            var controller = other.GetComponentInParent<Movimiento>() ?? other.GetComponentInChildren<Movimiento>();
            if (controller != null) controller.enabled = false;

            if (Cinematicas_nojugables.Instance != null)
            {
                Cinematicas_nojugables.Instance.IniciarCinematicaVN(dialogoFinal, () =>
                {
                    if (!string.IsNullOrEmpty(nombreSiguienteNivel))
                    {
                        SceneManager.LoadScene(nombreSiguienteNivel);
                    }
                });
            }
            else
            {
                Debug.LogError("Error: Cinematicas_nojugables.Instance es NULL. Verifica que el script esté activo en el Canvas.");
            }
        }
    }

    private void SceneManagement(string escena)
    {
        SceneManager.LoadScene(escena);
    }
}