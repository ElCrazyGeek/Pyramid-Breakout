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

    public void ActivarModoLibre()
    {
        SetPrioridades(camLibre: 20, camRiel: 10, camCarrera: 10);
        if (MirillaLibre != null) MirillaLibre.SetActive(true);
        if (MirillaRiel != null) MirillaRiel.SetActive(false);
    }

    public void ActivarModoRiel()
    {
        SetPrioridades(camLibre: 10, camRiel: 20, camCarrera: 10);
        if (MirillaRiel != null) MirillaRiel.SetActive(true);
        if (MirillaLibre != null) MirillaLibre.SetActive(false);
    }

    public void ActivarModoCarrera()
    {
        SetPrioridades(camLibre: 10, camRiel: 10, camCarrera: 20);
        // En carreras normalmente no se usa mirilla de apuntado
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