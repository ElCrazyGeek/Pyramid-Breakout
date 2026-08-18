using UnityEngine;
using UnityEngine.Rendering;

public class Player_Controller : MonoBehaviour
{
    public float speed =15.0f;
    Vector2 moveLimits = new Vector2(8f, 4.5f);

    public Vector2 currentInput;
    

    void Update()
    {
        GetInput();
        Mover();

    }
}
