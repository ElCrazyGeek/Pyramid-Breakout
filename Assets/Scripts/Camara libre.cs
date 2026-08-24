using Unity.Cinemachine;
using UnityEngine;

public class Camaralibre : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineCamera cineCam; // <- nuevo, referencia al componente principal
    [SerializeField] private CinemachineFollow follow;
    [SerializeField] private CinemachineRotationComposer rotationComposer;
    [SerializeField] private Player_mundolibre player;

    [Header("Peek lateral (mundo)")]
    [SerializeField] private float peekMaxOffset = 15f;
    [SerializeField] private float peekSmoothSpeed = 6f;

    [Header("Encuadre lateral (pantalla)")]
    [SerializeField] private float screenPeekMaxOffset = 0.2f;
    [SerializeField] private float screenPeekSmoothSpeed = 5f;

    [Header("Margen vertical (eje Y) — manejado FUERA de Cinemachine")]
    [SerializeField] private float alturaOffset = 3.5f; // qué tan arriba de la nave, en Y absoluto
    [SerializeField] private float margenY = 8f;
    [SerializeField] private float seguimientoSuavidadY = 2f;

    private float baseOffsetX;
    private Vector2 baseScreenPosition;
    private float fixedPlayerY;
    private float currentCamY;

    void Start()
    {
        if (follow != null)
            baseOffsetX = follow.FollowOffset.x;

        if (rotationComposer != null)
            baseScreenPosition = rotationComposer.Composition.ScreenPosition;

        if (player != null)
            fixedPlayerY = player.transform.position.y;

        currentCamY = transform.position.y;
    }

    void AjustarAlturaCamara(CinemachineBrain brain)
    {
        if (player == null || cineCam == null) return;
        if (brain.ActiveVirtualCamera as UnityEngine.Object != (UnityEngine.Object)cineCam) return;

        // 1. Distancia vertical actual respecto a la altura de referencia
        float deltaY = player.transform.position.y - fixedPlayerY;

        // 2. Si la nave supera el margen (hacia arriba o abajo), calculamos cuánto se desbordó
        float excesoY = 0f;
        if (Mathf.Abs(deltaY) > margenY)
        {
            excesoY = deltaY - (Mathf.Sign(deltaY) * margenY);
            // Desplaza suavemente la altura base del riel hacia el jugador
            fixedPlayerY = Mathf.Lerp(fixedPlayerY, fixedPlayerY + excesoY, seguimientoSuavidadY * Time.deltaTime);
        }

        // 3. La altura de la cámara sigue la base fija + el offset de altura deseado
        float targetCamY = fixedPlayerY + alturaOffset;
        currentCamY = Mathf.Lerp(currentCamY, targetCamY, seguimientoSuavidadY * Time.deltaTime);

        // 4. Aplicar la posición vertical
        Vector3 pos = transform.position;
        pos.y = currentCamY;
        transform.position = pos;
    }

    void LateUpdate()
    {
        if (player == null) return;

        float inputX = player.PlayerInput.x;

        // Peek lateral — SOLO X, ya no toca Y en absoluto
        if (follow != null)
        {
            float targetPeek = inputX * peekMaxOffset;
            Vector3 offset = follow.FollowOffset;
            offset.x = Mathf.Lerp(offset.x, baseOffsetX + targetPeek, peekSmoothSpeed * Time.deltaTime);
            follow.FollowOffset = offset; // Y de este offset ya no se toca aquí
        }

        // Encuadre en pantalla (igual que antes)
        if (rotationComposer != null)
        {
            var comp = rotationComposer.Composition;
            float targetScreenX = -inputX * screenPeekMaxOffset;
            comp.ScreenPosition.x = Mathf.Lerp(comp.ScreenPosition.x, baseScreenPosition.x + targetScreenX, screenPeekSmoothSpeed * Time.deltaTime);
            rotationComposer.Composition = comp;
        }
    }

    // Se ejecuta DESPUÉS de que Cinemachine ya calculó posición/rotación de esta cámara
    void AjustarAlturaCamara(ICinemachineCamera cam, CinemachineBrain brain)
    {
        if (player == null || cineCam == null || (ICinemachineCamera)cineCam != cam) return;

        float deltaY = player.transform.position.y - fixedPlayerY;
        float excesoY = Mathf.Max(0f, Mathf.Abs(deltaY) - margenY) * Mathf.Sign(deltaY);
        float targetY = player.transform.position.y - deltaY + excesoY + alturaOffset;

        currentCamY = Mathf.Lerp(currentCamY, targetY, seguimientoSuavidadY * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.y = currentCamY;
        transform.position = pos; // sobreescribe SOLO Y, después de Cinemachine

        if (Mathf.Abs(excesoY) > 0.001f)
            fixedPlayerY = Mathf.Lerp(fixedPlayerY, player.transform.position.y - Mathf.Sign(deltaY) * margenY, seguimientoSuavidadY * Time.deltaTime);
    }
}