using UnityEngine;
using Unity.Cinemachine;
using System;


public class Cambiarcamara : MonoBehaviour
{

    [SerializeField] public CinemachineCamera camaraRiel;
    [SerializeField] public CinemachineCamera camaraLibre;
    [SerializeField] public GameObject MirillaRiel;

    [SerializeField] public GameObject MirillaLibre;

    public Boolean modoLibre;

    /*private void Update()
    {
        if (modoLibre != true) { 
            Mirilla = MirillaRiel;
        }
    }*/ 
    public void ActivarModoLibre()
    {
        camaraLibre.Priority = 20;
        camaraRiel.Priority = 10;
        modoLibre = true;
    }

    public void ActivarModoRiel()
    {
        camaraRiel.Priority = 20;
        camaraLibre.Priority = 10;
        modoLibre = false;
    }
}
