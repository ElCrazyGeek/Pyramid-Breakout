using UnityEngine;
using UnityEngine.InputSystem;

public class Movimiento : MonoBehaviour
{
    public float velocidad = 05.0f;
    public float velocidad_lados = 15.0f;


    [Header("inclinacion")]
    [SerializeField] public float inclVer;
    [SerializeField] public float inclHor;

    [SerializeField] public float velRet;

    [Header("Limites")]

    public Vector2 Limites = new Vector2(12.0f, 7.5f);

    public Vector2 PlayerInput;

    public void OnMovimiento(InputValue value) {
        PlayerInput = value.Get<Vector2>();
    }

    private void Update()
    {
        mover();
        inclinacion();
    }

    void mover() {
        float moverx = transform.localPosition.x + (PlayerInput.x * velocidad_lados * Time.deltaTime);
        float movery = transform.localPosition.y + (PlayerInput.y * velocidad_lados * Time.deltaTime);
        float moverz = transform.localPosition.z + (velocidad * Time.deltaTime);

        moverx = Mathf.Clamp(moverx, -Limites.x, Limites.x);
        movery = Mathf.Clamp(movery, -Limites.y, Limites.y);



        transform.position = new Vector3(moverx, movery, moverz); 
    }

    void inclinacion() {
        float inclinacionVer = -PlayerInput.y * inclVer;
        float inclinacionHor = -PlayerInput.x * inclHor;

        Quaternion TargetRotation = Quaternion.Euler(inclinacionVer, 0f, inclinacionHor);

        transform.localRotation = Quaternion.Slerp(transform.localRotation, TargetRotation, velRet * Time.deltaTime); 
    }

    
}
