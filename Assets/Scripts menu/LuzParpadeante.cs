using UnityEngine;
using UnityEngine.UI; // Aquí vive 'Graphic'

public class LuzParpadeante : MonoBehaviour
{
    // Al usar 'Graphic', ahora acepta Imágenes y Textos por igual
    public Graphic elementoAiluminar;

    void Update()
    {
        if (elementoAiluminar != null)
        {
            Color colorActual = elementoAiluminar.color;
            colorActual.a = Random.Range(0.5f, 1f);
            elementoAiluminar.color = colorActual;
        }
    }
}