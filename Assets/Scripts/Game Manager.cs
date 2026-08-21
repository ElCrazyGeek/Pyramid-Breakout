using UnityEngine;
public enum ModoVuelo { Riel, Libre }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Cambiarcamara cambiarCamara;
    [SerializeField] private Player_rieles Player_rieles;
    [SerializeField] private Player_mundolibre Player_Libre; 

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
        bool esLibre = modo == ModoVuelo.Libre;

        Player_rieles.enabled = !esLibre;
        Player_Libre.enabled = esLibre;

        if (esLibre)
            cambiarCamara.ActivarModoLibre();
        else
            cambiarCamara.ActivarModoRiel();
    }
    
}
