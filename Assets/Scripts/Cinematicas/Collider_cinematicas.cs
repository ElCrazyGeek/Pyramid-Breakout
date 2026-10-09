using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// --- ESTRUCTURAS DE DATOS DE LA CINEMÁTICA ---
[System.Serializable]
public struct EstadoActorEnLinea
{
    [Tooltip("El ID del actor configurado en Cinematicas_nojugables (ej. 'Lyra', 'Orion')")]
    public string idActor;
    public bool visible;                   // True = aparece/se queda, False = ¡desaparece de la pantalla!
    public Sprite nuevaExpresion;          // Si pones un PNG aquí, actualiza su pose/expresión
    public RectTransform moverASlot;       // Opcional: objeto vacío hacia donde se mueve en este diálogo
}

[System.Serializable]
public struct LineaVN
{
    [Header("Diálogo")]
    public string idActorHablante;         // Quién habla (se ilumina y da el salto de énfasis)
    public string nombreAMostrar;          // Lo que se escribe en el recuadro de nombre (ej. 'Lyra')
    [TextArea(2, 4)]
    public string texto;

    [Header("Escenario / Render")]
    public Sprite fondoEscena;             // Opcional: render o ilustración 2D

    [Header("Control de Actores en esta línea")]
    [Tooltip("Define quién aparece, quién cambia de cara y quién desaparece en este diálogo.")]
    public EstadoActorEnLinea[] cambiosActores;

    [Header("Tiempo")]
    [Tooltip("0 = Espera a que el jugador pulse Espacio/Clic. Mayor a 0 = Pasa solo al terminar los segundos.")]
    public float tiempoEnPantalla;
}

// --- TRIGGER DE FIN / INTERMEDIO DE NIVEL ---
public class Collider_cinematicas : MonoBehaviour
{
[Header("Transición de Escena")]
    [Tooltip("Marca la casilla si este trigger debe cargar otra escena al terminar la cinemática.")]
    [SerializeField] private bool cambiarDeEscena = true;
    [SerializeField] private string nombreSiguienteNivel;

    [Header("Cinemática")]
    [SerializeField] private LineaVN[] dialogoFinal;

    [Header("Control de Nave")]
    [Tooltip("Si NO cambia de escena, ¿deseas que el jugador vuelva a pilotar la nave al terminar?")]
    [SerializeField] private bool reactivarControlesAlTerminar = false;

    private bool activado = false;
    private AsyncOperation operacionCarga;
    private Movimiento controllerMovimiento; // Única referencia necesaria para la nave
    private void OnTriggerEnter(Collider other)
    {
        if (activado) return;

        // Comprobación de la nave usando Tag o buscando el script Movimiento
        bool esJugador = other.CompareTag("Player") ||
                         other.GetComponentInParent<Movimiento>() != null ||
                         other.GetComponentInChildren<Movimiento>() != null;

        if (esJugador)
        {
            activado = true;

            // 1. Apagar controles del jugador
            controllerMovimiento = other.GetComponentInParent<Movimiento>() ?? other.GetComponentInChildren<Movimiento>();
            if (controllerMovimiento != null) controllerMovimiento.enabled = false;

            // 2. Iniciar precarga si se marcó cambio de escena
            if (cambiarDeEscena && !string.IsNullOrEmpty(nombreSiguienteNivel))
            {
                StartCoroutine(PrecargarEscenaSegundoPlano());
            }

            // 3. Disparar la cinemática en el Canvas
            if (Cinematicas_nojugables.Instance != null)
            {
                Cinematicas_nojugables.Instance.IniciarCinematicaVN(dialogoFinal, () =>
                {
                    // Callback al finalizar el último diálogo:
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

                        Debug.Log("Cinemática completada. La partida continúa en la misma escena.");
                    }
                });
            }
            else
            {
                Debug.LogError("Error: No se encontró Cinematicas_nojugables.Instance activa en la escena.");
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

        Debug.Log("Escena siguiente precargada en memoria al 100%. Lista para cuando termine el diálogo.");
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