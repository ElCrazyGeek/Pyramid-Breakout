using UnityEngine;
using Unity.Cinemachine;

public class Cambiarcamara : MonoBehaviour
{
    [Header("Cámaras Cinemachine")]
    [SerializeField] public CinemachineCamera camaraRiel;
    [SerializeField] public CinemachineCamera camaraLibre;
    [SerializeField] public CinemachineCamera camaraCarrera;

    [Header("Mirilla Única")]
    [SerializeField] public GameObject mirilla; // Arrastra aquí tu objeto MirillaUI (o el objeto de la mirilla)

    public void ActivarModoRiel()
    {
        SetPrioridades(camLibre: 10, camRiel: 20, camCarrera: 10);

        // Mostrar la mirilla en modo Riel
        if (mirilla != null) mirilla.SetActive(true);
    }

    public void ActivarModoLibre()
    {
        SetPrioridades(camLibre: 20, camRiel: 10, camCarrera: 10);

        // Mostrar la mirilla en modo Libre
        if (mirilla != null) mirilla.SetActive(true);
    }

    public void ActivarModoCarrera()
    {
        SetPrioridades(camLibre: 10, camRiel: 10, camCarrera: 20);

        // Ocultar la mirilla en modo Carrera
        if (mirilla != null) mirilla.SetActive(false);
    }

    public void ReiniciarSeguimiento()
    {
        if (camaraRiel) camaraRiel.PreviousStateIsValid = false;
        if (camaraLibre) camaraLibre.PreviousStateIsValid = false;
        if (camaraCarrera) camaraCarrera.PreviousStateIsValid = false;
    }

    private void SetPrioridades(int camLibre, int camRiel, int camCarrera)
    {
        if (camaraLibre != null) camaraLibre.Priority = camLibre;
        if (camaraRiel != null) camaraRiel.Priority = camRiel;
        if (camaraCarrera != null) camaraCarrera.Priority = camCarrera;
    }
}