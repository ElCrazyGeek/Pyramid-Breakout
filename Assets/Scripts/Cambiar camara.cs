using UnityEngine;
using Unity.Cinemachine;

public class Cambiarcamara : MonoBehaviour
{
  
    [SerializeField] public CinemachineCamera camaraRiel;
    [SerializeField] public CinemachineCamera camaraLibre;

    public void ActivarModoLibre()
    {
        camaraLibre.Priority = 20;
        camaraRiel.Priority = 10;
    }

    public void ActivarModoRiel()
    {
        camaraRiel.Priority = 20;
        camaraLibre.Priority = 10;
    }
}
