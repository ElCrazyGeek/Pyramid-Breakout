using Unity.Cinemachine;
using UnityEngine;

public class Camaralibre : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineFollow follow;
    [SerializeField] private CinemachineRotationComposer rotationComposer; // <- nuevo
    [SerializeField] private Player_mundolibre player;

    [Header("Peek lateral (mundo)")]
    [SerializeField] private float peekMaxOffset = 15f;
    [SerializeField] private float peekSmoothSpeed = 6f;

    [Header("Encuadre lateral (pantalla)")]
    [SerializeField] private float screenPeekMaxOffset = 0.2f; // 0 = centro, 0.5 ≈ borde de pantalla
    [SerializeField] private float screenPeekSmoothSpeed = 5f;

    private float baseOffsetX;
    private Vector2 baseScreenPosition;

    void Start()
    {
        if (follow != null)
            baseOffsetX = follow.FollowOffset.x;

        if (rotationComposer != null)
            baseScreenPosition = rotationComposer.Composition.ScreenPosition;
    }

    void LateUpdate()
    {
        if (player == null) return;

        float inputX = player.PlayerInput.x;

        // 1. Peek en el mundo (ya lo tenías)
        if (follow != null)
        {
            float targetPeek = inputX * peekMaxOffset;
            Vector3 offset = follow.FollowOffset;
            offset.x = Mathf.Lerp(offset.x, baseOffsetX + targetPeek, peekSmoothSpeed * Time.deltaTime);
            follow.FollowOffset = offset;
        }

        // 2. Encuadre: mueve dónde aparece la nave en pantalla
        if (rotationComposer != null)
        {
            var comp = rotationComposer.Composition;
            float targetScreenX = -inputX * screenPeekMaxOffset; // signo invertido: gira izq -> nave se ve a la derecha
            comp.ScreenPosition.x = Mathf.Lerp(comp.ScreenPosition.x, baseScreenPosition.x + targetScreenX, screenPeekSmoothSpeed * Time.deltaTime);
            rotationComposer.Composition = comp;
        }
    }
}
