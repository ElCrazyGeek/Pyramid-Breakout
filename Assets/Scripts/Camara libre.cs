using Unity.Cinemachine;
using UnityEngine;

public class Camaralibre : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineFollow follow;
    [SerializeField] private CinemachineRotationComposer rotationComposer;
    [SerializeField] private Player_mundolibre player;

    [Header("Peek lateral (Desplazamiento horizontal)")]
    [SerializeField] private float peekMaxOffset = 4f;       // Desplazamiento máximo en X detrás de la nave
    [SerializeField] private float peekSmoothSpeed = 5f;

    [Header("Encuadre lateral (Screen Composer)")]
    [SerializeField] private float screenPeekMaxOffset = 0.15f; // Desvío sutil en pantalla
    [SerializeField] private float screenPeekSmoothSpeed = 5f;

    [Header("Rango Vertical (Holgura en Y)")]
    [SerializeField] private bool limitarSeguimientoY = true;
    [SerializeField] private float margenVerticalY = 3.5f;   // Rango libre antes de arrastrar en Y
    [SerializeField] private float velocidadSuavizadoY = 2f;

    private float baseOffsetX;
    private Vector2 baseScreenPosition;
    private float baseYRef;

    void Start()
    {
        if (follow != null)
            baseOffsetX = follow.FollowOffset.x;

        if (rotationComposer != null)
            baseScreenPosition = rotationComposer.Composition.ScreenPosition;

        if (player != null)
            baseYRef = player.transform.position.y;
    }

    // Usar LateUpdate es indispensable para que la cámara no tiemble con la nave
    void LateUpdate()
    {
        if (player == null) return;

        float inputX = player.PlayerInput.x;

        // 1. Peek Horizontal (Mueve la cámara físicamente a los lados en las curvas)
        if (follow != null)
        {
            float targetPeek = inputX * peekMaxOffset;
            Vector3 offset = follow.FollowOffset;
            offset.x = Mathf.Lerp(offset.x, baseOffsetX + targetPeek, peekSmoothSpeed * Time.deltaTime);

            // Holgura en Y: la cámara solo sube si la nave se sale de margenVerticalY
            if (limitarSeguimientoY)
            {
                float deltaY = player.transform.position.y - baseYRef;
                if (Mathf.Abs(deltaY) > margenVerticalY)
                {
                    float exceso = deltaY - (Mathf.Sign(deltaY) * margenVerticalY);
                    baseYRef = Mathf.Lerp(baseYRef, baseYRef + exceso, velocidadSuavizadoY * Time.deltaTime);
                }
            }

            follow.FollowOffset = offset;
        }

        // 2. Encuadre en Pantalla (Desplaza la nave hacia el borde correspondiente al virar)
        if (rotationComposer != null)
        {
            var comp = rotationComposer.Composition;
            float targetScreenX = -inputX * screenPeekMaxOffset;
            comp.ScreenPosition.x = Mathf.Lerp(comp.ScreenPosition.x, baseScreenPosition.x + targetScreenX, screenPeekSmoothSpeed * Time.deltaTime);
            rotationComposer.Composition = comp;
        }
    }
}