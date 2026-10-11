using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Collider_cinematicas : MonoBehaviour
{
    [Header("Transición de Escena")]
    [Tooltip("Marca la casilla si este trigger debe cargar otra escena al terminar la cinemática.")]
    [SerializeField] private bool cambiarDeEscena = true;
    [SerializeField] private string nombreSiguienteNivel;

    [Header("Secuencia de Tomas (Storyboard)")]
    [SerializeField] private TomaCinematica[] tomasFinales; // <-- Solo debe existir ESTA declaración

    [Header("Control de Nave")]
    [Tooltip("Si NO cambia de escena, ¿deseas que el jugador vuelva a pilotar la nave al terminar?")]
    [SerializeField] private bool reactivarControlesAlTerminar = false;

    private bool activado = false;
    private AsyncOperation operacionCarga;
    private Movimiento controllerMovimiento;

    private void OnTriggerEnter(Collider other)
    {
        if (activado) return;

        bool esJugador = other.CompareTag("Player") ||
                         other.GetComponentInParent<Movimiento>() != null ||
                         other.GetComponentInChildren<Movimiento>() != null;

        if (esJugador)
        {
            activado = true;

            // 1. Apagar controles de la nave
            controllerMovimiento = other.GetComponentInParent<Movimiento>() ?? other.GetComponentInChildren<Movimiento>();
            if (controllerMovimiento != null) controllerMovimiento.enabled = false;

            // 2. Iniciar precarga si se marcó cambio de escena
            if (cambiarDeEscena && !string.IsNullOrEmpty(nombreSiguienteNivel))
            {
                StartCoroutine(PrecargarEscenaSegundoPlano());
            }

            // 3. Disparar la cinemática de tomas en el Canvas
            if (Cinematicas_nojugables.Instance != null)
            {
                Cinematicas_nojugables.Instance.IniciarCinematicaVN(tomasFinales, () =>
                {
                    if (cambiarDeEscena)
                    {
                        ActivarEscenaCargada();
                    }
                    else
                    {
                        if (reactivarControlesAlTerminar && controllerMovimiento != null)
                        {
                            controllerMovimiento.enabled = true;
                        }

                        Debug.Log("Cinemática completada. La nave continúa en la misma escena.");
                    }
                });
            }
            else
            {
                Debug.LogError("Error: No se encontró Cinematicas_nojugables.Instance en la escena.");
            }
        }
    }

    private IEnumerator PrecargarEscenaSegundoPlano()
    {
        operacionCarga = SceneManager.LoadSceneAsync(nombreSiguienteNivel);
        operacionCarga.allowSceneActivation = false;

        while (operacionCarga.progress < 0.9f)
        {
            yield return null;
        }

        Debug.Log("Escena siguiente precargada en memoria al 100%.");
    }

    private void ActivarEscenaCargada()
    {
        if (operacionCarga != null)
        {
            operacionCarga.allowSceneActivation = true;
        }
        else if (!string.IsNullOrEmpty(nombreSiguienteNivel))
        {
            SceneManager.LoadScene(nombreSiguienteNivel);
        }
    }
}