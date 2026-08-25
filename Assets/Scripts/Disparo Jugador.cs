using UnityEngine;
using UnityEngine.InputSystem;

public class DisparoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject JugadorProyectil;
    [SerializeField] private Transform[] puntosDisparo;

    [Header("Control de Delay / Cooldown")]
    [SerializeField] private float delayEntreDisparos = 0.3f; 
    private float tiempoSiguienteDisparo = 0f;

   
    public void OnDisparo(InputValue value)
    {
     
        if (value.isPressed)
        {

            if (Time.time >= tiempoSiguienteDisparo)
            {
                Disparar();
                tiempoSiguienteDisparo = Time.time + delayEntreDisparos; 
            }
        }
    }

    private void Disparar()
    {
        if (JugadorProyectil == null || puntosDisparo.Length == 0) return;

        foreach (Transform punto in puntosDisparo)
        {
            Instantiate(JugadorProyectil, punto.position, punto.rotation);
        }
    }
}
