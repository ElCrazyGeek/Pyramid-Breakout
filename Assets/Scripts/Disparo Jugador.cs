using UnityEngine;
using UnityEngine.InputSystem;

public class DisparoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform[] puntosDisparo; // Cañón izquierdo / derecho
    [SerializeField] public Transform mirillaTarget;   // Objeto de la mirilla

    [Header("Control de Delay")]
    [SerializeField] private float delayEntreDisparos = 0.25f;
    private float tiempoSiguienteDisparo = 0f;

    [Header("Estado")]
    public bool puedeDisparar = true;

    public void OnDisparo(InputValue value)
    {
        // Bloquea el disparo si está deshabilitado o si el GameManager está en modo Carrera
        if (!puedeDisparar) return;
        if (GameManager.Instance != null && GameManager.Instance.ModoActual == ModoVuelo.Carrera) return;

        if (value.isPressed && Time.time >= tiempoSiguienteDisparo)
        {
            Disparar();
            tiempoSiguienteDisparo = Time.time + delayEntreDisparos;
        }
    }

    private void Disparar()
    {
        if (prefabProyectil == null || puntosDisparo.Length == 0) return;

        Camera cam = Camera.main;

        Vector3 direccionDisparo = (mirillaTarget != null)
            ? (mirillaTarget.position - transform.position).normalized
            : (cam != null ? cam.transform.forward : transform.forward);

        Quaternion rotacionFinal = Quaternion.LookRotation(direccionDisparo);

        foreach (Transform punto in puntosDisparo)
        {

            Instantiate(prefabProyectil, punto.position, rotacionFinal);
        }
    }
}