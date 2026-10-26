using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct MensajeRadio
{
    public string nombreEmisor;
    public Sprite retrato;
    [TextArea(2, 3)]
    public string mensaje;
    public float tiempoEnPantalla; // Cuántos segundos permanece antes de ocultarse
    public AudioClip sfxEntradaRadio;
}

public class Dialogos_Manager : MonoBehaviour
{
    public static Dialogos_Manager Instance { get; private set; }

    [Header("Referencias de UI (Interfaz)")]
    [SerializeField] private CanvasGroup contenedorUI; // CanvasGroup en el objeto 'Interfaz'
    [SerializeField] private Image imagenRetrato;       // 'Imagen personaje'
    [SerializeField] private TextMeshProUGUI textoNombre;// 'Nombre escrito'
    [SerializeField] private TextMeshProUGUI textoCuerpo;// 'Dialogo texto'

    [Header("Ajustes de Lectura")]
    [SerializeField] private float velocidadTipeo = 0.03f;
    [SerializeField] private float velocidadFade = 5f;

    private Coroutine rutinaActual;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (contenedorUI != null)
        {
            contenedorUI.alpha = 0f;
            contenedorUI.gameObject.SetActive(false);
        }
    }

    public void TransmitirMensaje(MensajeRadio mensaje)
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(RutinaMostrarMensaje(mensaje));
    }

    private IEnumerator RutinaMostrarMensaje(MensajeRadio msg)
    {
        // 1. Configurar datos
        if (textoNombre != null) textoNombre.text = msg.nombreEmisor;
        if (imagenRetrato != null)
        {
            imagenRetrato.gameObject.SetActive(msg.retrato != null);
            imagenRetrato.sprite = msg.retrato;
        }

        if (msg.sfxEntradaRadio != null && Audio_cinematicas.Instance != null)
        {
            Audio_cinematicas.Instance.PlaySFX(msg.sfxEntradaRadio);
        }

        // 2. Fade In de la ventana de transmisión
        if (contenedorUI != null)
        {
            contenedorUI.gameObject.SetActive(true);
            while (contenedorUI.alpha < 1f)
            {
                contenedorUI.alpha = Mathf.MoveTowards(contenedorUI.alpha, 1f, velocidadFade * Time.deltaTime);
                yield return null;
            }
        }

        // 3. Efecto máquina de escribir (Typewriter) con sonido de radio
        if (textoCuerpo != null)
        {
            textoCuerpo.text = "";
            for (int i = 0; i < msg.mensaje.Length; i++)
            {
                textoCuerpo.text += msg.mensaje[i];

                if (i % 2 == 0 && !char.IsWhiteSpace(msg.mensaje[i]) && Audio_cinematicas.Instance != null)
                {
                    Audio_cinematicas.Instance.PlaySonidoTipeo();
                }

                yield return new WaitForSeconds(velocidadTipeo);
            }
        }

        // 4. Tiempo de lectura visible durante el combate
        float duracionEspera = msg.tiempoEnPantalla > 0f ? msg.tiempoEnPantalla : 3.5f;
        yield return new WaitForSeconds(duracionEspera);

        // 5. Fade Out y ocultado
        if (contenedorUI != null)
        {
            while (contenedorUI.alpha > 0f)
            {
                contenedorUI.alpha = Mathf.MoveTowards(contenedorUI.alpha, 0f, velocidadFade * Time.deltaTime);
                yield return null;
            }
            contenedorUI.gameObject.SetActive(false);
        }

        rutinaActual = null;
    }
}