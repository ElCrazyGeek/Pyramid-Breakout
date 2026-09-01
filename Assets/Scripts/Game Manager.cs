using UnityEngine;

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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        AplicarModo(modoActual);
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