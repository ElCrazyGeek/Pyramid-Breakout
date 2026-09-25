using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ModoVuelo { Riel, Libre, Carrera }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Cambiarcamara cambiarCamara;
    [SerializeField] private Player_rieles Player_rieles;
    [SerializeField] private Player_mundolibre Player_Libre;
    [SerializeField] private Player_Carreras Player_Carrera;
    [SerializeField] private DisparoJugador disparoJugador;


    [SerializeField] private ModoVuelo modoActual = ModoVuelo.Riel;
    public ModoVuelo ModoActual => modoActual;

    [Header("Reinicio")]
    [Tooltip("Actívalo durante Play para pausar 3 segundos y recargar la escena actual.")]
    [SerializeField] private bool reiniciarEscena;

    private Coroutine rutinaReinicio;
    private float escalaTiempoAnterior;

    void Awake()
    {
        Instance = this;
        // Es una solicitud de una sola ejecución, no un estado que se guarda.
        reiniciarEscena = false;
    }

    void Start()
    {
        AplicarModo(modoActual);
    }

    void Update()
    {
        if (!reiniciarEscena) return;

        reiniciarEscena = false;
        if (rutinaReinicio != null) return;

        Scene escena = SceneManager.GetActiveScene();
        if (escena.buildIndex < 0)
        {
            Debug.LogError("No se puede reiniciar: agrega la escena actual a la lista de escenas del Build Profile.", this);
            return;
        }

        rutinaReinicio = StartCoroutine(ReiniciarEscenaTrasPausa(escena.buildIndex));
    }

    private IEnumerator ReiniciarEscenaTrasPausa(int indiceEscena)
    {
        escalaTiempoAnterior = Time.timeScale;
        Time.timeScale = 0f;

        // La espera debe continuar aunque el tiempo del juego esté detenido.
        yield return new WaitForSecondsRealtime(3f);

        rutinaReinicio = null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(indiceEscena, LoadSceneMode.Single);
    }

    void OnDisable()
    {
        if (rutinaReinicio == null) return;

        StopCoroutine(rutinaReinicio);
        rutinaReinicio = null;
        reiniciarEscena = false;
        Time.timeScale = escalaTiempoAnterior;
    }

    public void CambiarModo(ModoVuelo nuevoModo)
    {
        if (nuevoModo == modoActual) return;
        modoActual = nuevoModo;
        AplicarModo(nuevoModo);
    }

    private void AplicarModo(ModoVuelo modo)
    {
        // 1. Desactivar todos los controladores de vuelo
        if (Player_rieles != null) Player_rieles.enabled = (modo == ModoVuelo.Riel);
        if (Player_Libre != null) Player_Libre.enabled = (modo == ModoVuelo.Libre);
        if (Player_Carrera != null) Player_Carrera.enabled = (modo == ModoVuelo.Carrera);

        if (disparoJugador != null)
        {
            disparoJugador.enabled = (modo != ModoVuelo.Carrera);
        }
        // 2. Notificar al gestor de cámaras
        if (cambiarCamara != null)
        {
            switch (modo)
            {
                case ModoVuelo.Riel:
                    cambiarCamara.ActivarModoRiel();
                    break;
                case ModoVuelo.Libre:
                    cambiarCamara.ActivarModoLibre();
                    break;
                case ModoVuelo.Carrera:
                    cambiarCamara.ActivarModoCarrera();
                    break;
            }
        }
    }
}
