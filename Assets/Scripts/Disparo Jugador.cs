using UnityEngine;
using UnityEngine.InputSystem;

public class DisparoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform[] puntosDisparo; // Cañón izquierdo / derecho
    [SerializeField] private Transform mirillaTarget;   // Objeto de la mirilla

    [Header("Control de Delay")]
    [SerializeField] private float delayEntreDisparos = 0.25f;
    private float tiempoSiguienteDisparo = 0f;

    public void OnDisparo(UnityEngine.InputSystem.InputValue value)
    {
        if (value.isPressed && Time.time >= tiempoSiguienteDisparo)
        {
            DispararHaciaMirilla();
            tiempoSiguienteDisparo = Time.time + delayEntreDisparos;
        }
    }

    private void DispararHaciaMirilla()
    {
        if (prefabProyectil == null || puntosDisparo.Length == 0 || mirillaTarget == null) return;


        Vector3 destino = mirillaTarget.position;

        foreach (Transform punto in puntosDisparo)
        {

            Vector3 direccionHaciaMirilla = (destino - punto.position).normalized;

            Quaternion rotacionDisparo = Quaternion.LookRotation(direccionHaciaMirilla);

            Instantiate(prefabProyectil, punto.position, rotacionDisparo);
        }
    }
}