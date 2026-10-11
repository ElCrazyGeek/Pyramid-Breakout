using UnityEngine;

public enum CondicionDialogo
{
    AlColisionar,        // Trigger de posición en el riel
    AlDestruirse,        // Cuando este enemigo u objeto muere
    PorSalud,            // Cuando la salud baja de cierto umbral
    PorTemporizador      // Tras X segundos de iniciar el nivel o evento
}

public class Dialogo_disparador : MonoBehaviour
{
    [Header("Condición de Disparo")]
    [SerializeField] private CondicionDialogo condicion = CondicionDialogo.AlColisionar;

    [Header("Parámetros Condicionales")]
    [Tooltip("Tiempo en segundos para activar si es 'PorTemporizador'.")]
    [SerializeField] private float tiempoActivacion = 5f;

    [Tooltip("Salud objetivo si es 'PorSalud' (ej. 50 para cuando baje al 50% de HP).")]
    [SerializeField] private float umbralSalud = 50f;

    [Header("Contenido del Diálogo")]
    [SerializeField] private MensajeRadio mensajeRadio;

    private bool yaSeActivo = false;
    private float cronometro = 0f;

    void Update()
    {
        if (yaSeActivo) return;

        // Evaluación de la condición por tiempo
        if (condicion == CondicionDialogo.PorTemporizador)
        {
            cronometro += Time.deltaTime;
            if (cronometro >= tiempoActivacion)
            {
                DispararMensaje();
            }
        }
    }

    // 1. Condición: Colisión con la nave (Trigger en el riel)
    private void OnTriggerEnter(Collider other)
    {
        if (yaSeActivo || condicion != CondicionDialogo.AlColisionar) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<Movimiento>() != null)
        {
            DispararMensaje();
        }
    }

    // 2. Condición: Al ser destruido (Muerte de jefe o torreta)
    private void OnDestroy()
    {
        // Se asegura de disparar solo durante gameplay y no al cerrar el juego
        if (!yaSeActivo && condicion == CondicionDialogo.AlDestruirse && gameObject.scene.isLoaded)
        {
            DispararMensaje();
        }
    }

    // 3. Condición: Llamada manual desde tu script de salud
    public void VerificarSalud(float saludActual)
    {
        if (yaSeActivo || condicion != CondicionDialogo.PorSalud) return;

        if (saludActual <= umbralSalud)
        {
            DispararMensaje();
        }
    }

    private void DispararMensaje()
    {
        yaSeActivo = true;

        if (Dialogos_Manager.Instance != null)
        {
            Dialogos_Manager.Instance.TransmitirMensaje(mensajeRadio);
        }
    }
}