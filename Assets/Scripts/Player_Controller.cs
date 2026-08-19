using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Player_Controller : MonoBehaviour
{
    [Header("Movimiento del jugador")]
    [SerializeField] 
    public float speed =15.0f;
    Vector2 moveLimits = new Vector2(8f, 4.5f);

    [Header("Incrinacion")]
    public float InclinacionX = 15f;
    public float InclinacionZ = 15f;

    public float InclinacionSpeed = 15f;

    public Vector2 PlayerInput;

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = value.Get<Vector2>();
    }


    void Update()
    {
        mover();
        InclinacionJugador();

    }


    void mover() { 
        Vector3 newPosition = transform.localPosition + 
        (Vector3)PlayerInput * speed * Time.deltaTime;

        newPosition.x = Mathf.Clamp(newPosition.x, -moveLimits.x, moveLimits.x);
        newPosition.y = Mathf.Clamp(newPosition.y, -moveLimits.y, moveLimits.y);

        transform.localPosition = newPosition;

    }

    void InclinacionJugador()
    {
        float targerPitch = -PlayerInput.y * InclinacionX;
        float targerRoll = -PlayerInput.x * InclinacionZ;

        // Supongamos que tu modelo viene girado -90 grados en Y de origen:
        float offsetMod3D = 90f; // Ajusta este número (90f, -90f o 180f) según como mire tu modelo

        Quaternion targetRotation = Quaternion.Euler(targerPitch - 90f, offsetMod3D, targerRoll );
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, InclinacionSpeed * Time.deltaTime);
    }
}
