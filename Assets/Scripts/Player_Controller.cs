using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    public float speed =15.0f;
    Vector2 moveLimits = new Vector2(8f, 4.5f);

    public Vector2 PlayerInput;

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = value.Get<Vector2>();
    }


    void Update()
    {
        mover();

    }


    void mover() { 
        Vector3 newPosition = transform.localPosition + 
        (Vector3)PlayerInput * speed * Time.deltaTime;

        newPosition.x = Mathf.Clamp(newPosition.x, -moveLimits.x, moveLimits.x);
        newPosition.y = Mathf.Clamp(newPosition.y, -moveLimits.y, moveLimits.y);

        transform.localPosition = newPosition;

    }
}
