using UnityEngine;
using Unity.Cinemachine;

public class Cambiarcamara : MonoBehaviour
{
    [Header("Cámaras Cinemachine")]
    [SerializeField] public CinemachineCamera camaraRiel;
    [SerializeField] public CinemachineCamera camaraLibre;
    [SerializeField] public CinemachineCamera camaraCarrera;

    [Header("Mirillas")]
    [SerializeField] public GameObject MirillaRiel;
    [SerializeField] public GameObject MirillaLibre;

    [Header("Referencia al Disparo")]
    [SerializeField] private DisparoJugador disparoJugador; // <-- Arrastra tu script DisparoJugador aquí

    public void ActivarModoLibre()
    {
        SetPrioridades(camLibre: 20, camRiel: 10, camCarrera: 10);

        // Activar la mirilla correspondiente
        if (MirillaLibre != null) MirillaLibre.SetActive(true);
        if (MirillaRiel != null) MirillaRiel.SetActive(false);

        // Actualizar el objetivo de convergencia de disparo
        if (disparoJugador != null && MirillaLibre != null)
        {
            disparoJugador.mirillaTarget = MirillaLibre.transform;
        }
    }

    public void ActivarModoRiel()
    {
        SetPrioridades(camLibre: 10, camRiel: 20, camCarrera: 10);

        // Activar la mirilla correspondiente
        if (MirillaRiel != null) MirillaRiel.SetActive(true);
        if (MirillaLibre != null) MirillaLibre.SetActive(false);

        // Actualizar el objetivo de convergencia de disparo
        if (disparoJugador != null && MirillaRiel != null)
        {
            disparoJugador.mirillaTarget = MirillaRiel.transform;
        }
    }

    public void ActivarModoCarrera()
    {
        SetPrioridades(camLibre: 10, camRiel: 10, camCarrera: 20);

        // Apagar ambas mirillas en modo carrera
        if (MirillaRiel != null) MirillaRiel.SetActive(false);
        if (MirillaLibre != null) MirillaLibre.SetActive(false);
    }

    private void SetPrioridades(int camLibre, int camRiel, int camCarrera)
    {
        if (camaraLibre != null) camaraLibre.Priority = camLibre;
        if (camaraRiel != null) camaraRiel.Priority = camRiel;
        if (camaraCarrera != null) camaraCarrera.Priority = camCarrera;
    }
}