using UnityEngine;

public enum CondicionDialogo
{
    AlColisionarTrigger,        // Cruzar un cubo invisible en el riel
    PorTemporizador,            // Tras X segundos de misión
    SaludBajaLyra,              // Lyra sufre daño y baja de cierto HP
    MuerteLyra,                 // Lyra es derribada
    SaludBajaEnemigo,           // Un enemigo/jefe baja de cierto HP
    AlDestruirEnemigo           // Al rematar a este enemigo específico
}

public class DisparadorDialogoInGame : MonoBehaviour
{
    [Header("Condición de Disparo")]
    [SerializeField] private CondicionDialogo condicion;

    [Header("Ajustes por Tiempo o Salud")]
    [Tooltip("Tiempo en segundos si la condición es 'PorTemporizador'.")]
    [SerializeField] private float tiempoActivacion = 5f;

    [Tooltip("Porcentaje o valor de salud (ej. 40 para cuando baje al 40%).")]
    [SerializeField] private float umbralSalud = 40f;

    [Header("Contenido de la Transmisión")]
    [SerializeField] private MensajeRadio mensajeRadio;

    private bool yaSeActivo = false;
    private float cronometro = 0f;

    private void Update()
    {
        if (yaSeActivo) return;

        // 1. Condición: Temporizador
        if (condicion == CondicionDialogo.PorTemporizador)
        {
            cronometro += Time.deltaTime;
            if (cronometro >= tiempoActivacion)
            {
                DispararTransmision();
            }
        }
    }

    // 2. Condición: Colisión con la nave en el riel
    private void OnTriggerEnter(Collider other)
    {
        if (yaSeActivo || condicion != CondicionDialogo.AlColisionarTrigger) return;

        bool esJugador = other.CompareTag("Player") ||
                         other.GetComponentInParent<Movimiento>() != null ||
                         other.GetComponentInChildren<Movimiento>() != null;

        if (esJugador)
        {
            DispararTransmision();
        }
    }

    // 3. Condición: Al morir el enemigo
    private void OnDestroy()
    {
        if (!yaSeActivo && condicion == CondicionDialogo.AlDestruirEnemigo && gameObject.scene.isLoaded)
        {
            DispararTransmision();
        }
    }

    // --- MÉTODOS PÚBLICOS PARA CONECTAR A TUS SCRIPTS DE VIDA ---

    /// <summary>
    /// Llamar desde el script de salud del Enemigo/Jefe pasando su porcentaje actual (0 - 100).
    /// </summary>
    public void NotificarSaludEnemigo(float saludActual)
    {
        if (yaSeActivo || condicion != CondicionDialogo.SaludBajaEnemigo) return;

        if (saludActual <= umbralSalud)
        {
            DispararTransmision();
        }
    }


    public void NotificarSaludLyra(float saludActual)
    {
        if (yaSeActivo || condicion != CondicionDialogo.SaludBajaLyra) return;

        if (saludActual <= umbralSalud)
        {
            DispararTransmision();
        }
    }


    public void NotificarMuerteLyra()
    {
        if (yaSeActivo || condicion != CondicionDialogo.MuerteLyra) return;

        DispararTransmision();
    }

    private void DispararTransmision()
    {
        yaSeActivo = true;

        if (Dialogos_Manager.Instance != null)
        {
            Dialogos_Manager.Instance.TransmitirMensaje(mensajeRadio);
        }
        else
        {
            Debug.LogWarning("No se encontró GestorDialogosInGame en la escena para transmitir la radio.");
        }
    }
}