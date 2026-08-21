using UnityEngine;
using UnityEngine.InputSystem;
public class Player_rieles : MonoBehaviour
{
    [Header("Movimiento del jugador")]

    [SerializeField]
    public float speed = 15.0f;
    [SerializeField]
    Vector2 LimitesMovimiento = new Vector2(20f, 10f);

    [SerializeField]
    public float VelocidadAvance = 15f;

    [SerializeField]
    public float VelocidadActual;

    [SerializeField] public bool Frenado = false;

    [Header("Incrinacion")]
    public float InclinacionX = 15f;
    public float InclinacionZ = 15f;

    public float VelocidadInclinacion = 15f;

    public Vector2 PlayerInput;

    public void OnMovimiento(InputValue value)
    {
        PlayerInput = value.Get<Vector2>();
    }


    void Update()
    {
        mover();
        avance();
        InclinacionJugador();

    }


    void mover()
    {
        Vector3 newPosition = transform.localPosition +
        (Vector3)PlayerInput * speed * Time.deltaTime;

        newPosition.x = Mathf.Clamp(newPosition.x, -LimitesMovimiento.x, LimitesMovimiento.x);
        newPosition.y = Mathf.Clamp(newPosition.y, -LimitesMovimiento.y, LimitesMovimiento.y);

        transform.localPosition = newPosition;

    }

    void OnFreno(InputValue value)
    {
        Frenado = value.isPressed;

    }
    void avance()
    {
        float velocidadActual = VelocidadAvance;

        if (Frenado)
        {
            velocidadActual = velocidadActual * 0.2f;
        }
        else
        {
            velocidadActual = VelocidadAvance;
        }


        float avanceZ = velocidadActual * Time.deltaTime;

        Vector3 newPosition = transform.position + new Vector3(0, 0, avanceZ);
        transform.position = newPosition;

        transform.position = newPosition;
    }



    void InclinacionJugador()
    {
        float targerPitch = -PlayerInput.y * InclinacionX;
        float targerRoll = -PlayerInput.x * InclinacionZ;


        float offsetMod3D = 90f;

        Quaternion targetRotation = Quaternion.Euler(targerPitch - 90f, offsetMod3D, targerRoll);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, VelocidadInclinacion * Time.deltaTime);
    }
}
