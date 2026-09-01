using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Carreras : MonoBehaviour
{
    [Header("Aceleración y Velocidades")]
    [SerializeField] private float velocidadMaxima = 250f;
    [SerializeField] private float aceleracion = 20f;
    [SerializeField] private float friccionFreno = 15f; 

    [Header("Boost (Turbo)")]
    [SerializeField] private float velocidadBoost = 85f;
    [SerializeField] private float duracionBoostMax = 2.0f;

    [Header("Dirección y Curvas")]
    [SerializeField] private float velocidadGiro = 85f;
    [SerializeField] private float inclinacionAlas = 40f;
    [SerializeField] private float suavizadoInclinacion = 10f;

    [Header("Antigravedad / Adherencia a la Pista")]
    [SerializeField] private float alturaSobrePista = 1.5f;
    [SerializeField] private float fuerzaAlineacion = 12f;
    [SerializeField] private LayerMask capaPista;

    [Header("Visual")]
    [SerializeField] private Transform modeloVisual;

    private Vector2 inputMovimiento;
    private float velocidadActual = 0f;
    private float tiempoBoostRestante = 0f;
    private bool estaAcelerando = false;
    private bool estaEnBoost = false;

    // 1. Recibir dirección (A/D o Stick izquierdo)
    public void OnMovimiento(InputValue value)
    {
        inputMovimiento = value.Get<Vector2>();
    }

    // 2. Método llamado al presionar y soltar el acelerador
    public void OnAcelerar(InputValue value)
    {
        estaAcelerando = value.isPressed;
    }

    // 3. Método del Boost
    public void OnBoost(InputValue value)
    {
        if (value.isPressed && tiempoBoostRestante <= 0f && estaAcelerando)
        {
            tiempoBoostRestante = duracionBoostMax;
            estaEnBoost = true;
        }
    }

    void Update()
    {
        GestionarVelocidad();
        MoverYAlinearPista();
        AplicarInclinacionVisual();
    }

    private void GestionarVelocidad()
    {
        if (tiempoBoostRestante > 0f)
        {
            tiempoBoostRestante -= Time.deltaTime;
            // Acelerar con potencia extra durante el turbo
            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadBoost, aceleracion * 2.5f * Time.deltaTime);
        }
        else
        {
            estaEnBoost = false;

            if (estaAcelerando)
            {
                // Acelera gradualmente hasta la velocidad de crucero
                velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadMaxima, aceleracion * Time.deltaTime);
            }
            else
            {
                // Desacelera suavemente por fricción hasta 0 al soltar la tecla
                velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, friccionFreno * Time.deltaTime);
            }
        }
    }

    private void MoverYAlinearPista()
    {
        // Solo permite girar si la nave tiene algo de impulso
        if (velocidadActual > 0.1f)
        {
            float giro = inputMovimiento.x * velocidadGiro * Time.deltaTime;
            transform.Rotate(0f, giro, 0f, Space.Self);
        }

        // Avance frontal según la velocidad acumulada
        transform.position += transform.forward * velocidadActual * Time.deltaTime;

        // Adherencia al suelo de la pista mediante Raycast
        Ray rayo = new Ray(transform.position + transform.up * 1f, -transform.up);
        if (Physics.Raycast(rayo, out RaycastHit hit, 10f, capaPista))
        {
            Vector3 targetPos = hit.point + (hit.normal * alturaSobrePista);
            transform.position = Vector3.Lerp(transform.position, targetPos, fuerzaAlineacion * Time.deltaTime);

            Quaternion targetRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, fuerzaAlineacion * Time.deltaTime);
        }
    }

    private void AplicarInclinacionVisual()
    {
        if (modeloVisual == null) return;

        float roll = -inputMovimiento.x * inclinacionAlas;
        Quaternion rotLocalDeseada = Quaternion.Euler(0f, 0f, roll);
        modeloVisual.localRotation = Quaternion.Slerp(modeloVisual.localRotation, rotLocalDeseada, suavizadoInclinacion * Time.deltaTime);
    }
}