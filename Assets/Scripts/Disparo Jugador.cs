using UnityEngine;
using Pyramid.Levels;
using UnityEngine.InputSystem;

public class DisparoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform[] puntosDisparo; // Cañón izquierdo / derecho
    [SerializeField] public RectTransform mirillaUI;    // Arrastra aquí el RectTransform de MirillaUI

    [Header("Control de Delay")]
    [SerializeField] private float delayEntreDisparos = 0.25f;
    private float tiempoSiguienteDisparo = 0f;

    [Header("Ajustes de Apuntado")]
    [SerializeField] private float distanciaHorizonte = 200f; // Distancia cuando disparas al vacío
    [SerializeField] private LayerMask capasImpacto = ~0;     // Detecta todo por defecto

    [Header("Estado")]
    public bool puedeDisparar = true;

    public void OnDisparo(InputValue value)
    {
        if (!enabled || !puedeDisparar || Time.timeScale <= 0) return;
        if (GameManager.Instance != null && GameManager.Instance.ModoActual == ModoVuelo.Carrera) return;

        if (value.isPressed && Time.time >= tiempoSiguienteDisparo)
        {
            Disparar();
            tiempoSiguienteDisparo = Time.time + delayEntreDisparos;
        }
    }

    public void ResetForLevel() { tiempoSiguienteDisparo = 0; }

    private void Disparar()
    {
        if (prefabProyectil == null || puntosDisparo.Length == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 puntoImpactoFinal;

        if (mirillaUI != null)
        {
            // 1. Trazar un rayo desde la cámara que pasa justo a través de la posición de la mirilla en pantalla
            Ray rayo = RectTransformUtility.ScreenPointToRay(cam, mirillaUI.position);

            // 2. Si el rayo impacta un enemigo o muro en 3D, apuntar directo a ese punto
            if (Physics.Raycast(rayo, out RaycastHit hit, distanciaHorizonte, capasImpacto))
            {
                puntoImpactoFinal = hit.point;
            }
            else
            {
                // Si dispara al cielo/espacio vacío, viaja al horizonte
                puntoImpactoFinal = rayo.GetPoint(distanciaHorizonte);
            }
        }
        else
        {
            puntoImpactoFinal = cam.transform.position + (cam.transform.forward * distanciaHorizonte);
        }

        // 3. Instanciar cada bala orientada hacia ese punto de impacto real
        foreach (Transform punto in puntosDisparo)
        {
            Vector3 direccionDisparo = (puntoImpactoFinal - punto.position).normalized;
            Quaternion rotacionFinal = Quaternion.LookRotation(direccionDisparo);

            var proyectil = Instantiate(prefabProyectil, punto.position, rotacionFinal);
            if (LevelSession.Active) LevelSession.Active.Objects.Track(proyectil);
        }
    }
}