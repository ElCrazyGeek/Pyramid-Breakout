using UnityEngine;

public class Camera_Rieles: MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.2f, -30f);

    [Header("Margen Camara")]
    [SerializeField] private float margenX = 28f;    
    [SerializeField] private float margenY = 23f;
    [SerializeField] private float seguimientoSuavidad = 5f; // qué tan rápido corrige cuando SÍ se pasa del margen

    private float fixedX;
    private float fixedY;

    void Start()
    {
        fixedX = transform.position.x + offset.x;
        fixedY = transform.position.y;
    }

    void Update()
    {
        if (playerTarget == null) return;

        // 1. Qué tan lejos está la nave del centro fijo de la cámara, en X e Y
        float deltaX = playerTarget.position.x - fixedX;
        float deltaY = playerTarget.position.y - fixedY;

        // 2. Si excede el margen, calcula cuánto se pasó (el "exceso")
        float excesoX = Mathf.Max(0f, Mathf.Abs(deltaX) - margenX) * Mathf.Sign(deltaX);
        float excesoY = Mathf.Max(0f, Mathf.Abs(deltaY) - margenY) * Mathf.Sign(deltaY);

        float targetX = fixedX + excesoX;
        float targetY = fixedY + excesoY;

        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, seguimientoSuavidad * Time.deltaTime);
        pos.y = Mathf.Lerp(pos.y, targetY, seguimientoSuavidad * Time.deltaTime);
        pos.z = playerTarget.position.z + offset.z;
        transform.position = pos;

        // 4. Actualiza el "fijo" para que el margen se mida siempre desde la posición actual de la cámara
        fixedX = pos.x;
        fixedY = pos.y;
    }
}