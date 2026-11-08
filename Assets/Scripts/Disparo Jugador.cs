using UnityEngine;
using Pyramid.Levels;
using UnityEngine.InputSystem;

public class DisparoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform[] puntosDisparo; // Cañón izquierdo / derecho
    [SerializeField] public RectTransform mirillaLejanaUI; // Retícula lejana (Impacto)

    [Header("Control de Cadencia")]
    [SerializeField] private float delayEntreDisparos = 0.22f;
    private float tiempoSiguienteDisparo = 0f;

    [Header("Ajustes de Apuntado")]
    [Tooltip("Distancia al horizonte a la que convergen los disparos si apuntas al cielo")]
    [SerializeField] private float distanciaHorizonte = 250f;
    [Tooltip("Capas con las que choca el rayo de apuntado")]
    [SerializeField] private LayerMask capasImpacto = ~0;

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

        // 1. Trazar rayo desde la cámara a través del centro de la mirilla exterior
        Ray rayoApuntado = (mirillaLejanaUI != null)
            ? RectTransformUtility.ScreenPointToRay(cam, mirillaLejanaUI.position)
            : new Ray(cam.transform.position, cam.transform.forward);

        Vector3 puntoImpactoObjetivo;

        // 2. Si hay un enemigo o muro bajo la mira, el blanco exacto es su superficie
        if (Physics.Raycast(rayoApuntado, out RaycastHit hit, distanciaHorizonte, capasImpacto))
        {
            puntoImpactoObjetivo = hit.point;
        }
        else
        {
            // Si apuntas al vacío, el objetivo es el fondo en el horizonte
            puntoImpactoObjetivo = rayoApuntado.GetPoint(distanciaHorizonte);
        }

        // 3. Cada cañón dispara directo hacia el punto de convergencia
        foreach (Transform punto in puntosDisparo)
        {
            Vector3 direccionDisparo = (puntoImpactoObjetivo - punto.position).normalized;
            Quaternion rotacionBala = Quaternion.LookRotation(direccionDisparo);

            GameObject bala = Instantiate(prefabProyectil, punto.position, rotacionBala);

            // Configuramos la bala para proteger al jugador de auto-daño
            Proyectil compProyectil = bala.GetComponent<Proyectil>();
            if (compProyectil != null)
            {
                compProyectil.origen = transform.root.gameObject;
                compProyectil.tagOrigen = "Player";
            }

            if (LevelSession.Active != null)
            {
                LevelSession.Active.Objects.Track(bala);
            }
        }
    }
}