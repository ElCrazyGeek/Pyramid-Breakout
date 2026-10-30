using System.Collections;
using UnityEngine;
using TMPro;

public class ControladorSubtitulos : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private TextMeshProUGUI campoTexto;
    [SerializeField] private GameObject panelFondo;

    [Header("Referencias de Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sonido de letra (opcional, para efecto máquina de escribir/radio)")]
    [SerializeField] private AudioClip sonidoLetra;

    [Header("Configuración Máquina de Escribir")]
    [SerializeField] private float velocidadTexto = 0.03f;

    private Coroutine rutinaEscribir;

    /// <summary>
    /// Muestra el texto de golpe y reproduce un clip de voz.
    /// Útil para llamadas desde UnityEvents que no aceptan múltiples parámetros.
    /// </summary>
    public void MostrarTextoSimple(string texto)
    {
        if (rutinaEscribir != null) StopCoroutine(rutinaEscribir);
        if (panelFondo != null) panelFondo.SetActive(true);

        campoTexto.text = texto;
    }

    /// <summary>
    /// Muestra el texto con efecto máquina de escribir letra por letra y beeps.
    /// </summary>
    public void EscribirTexto(string texto)
    {
        if (panelFondo != null) panelFondo.SetActive(true);
        if (rutinaEscribir != null) StopCoroutine(rutinaEscribir);
        rutinaEscribir = StartCoroutine(RutinaEscribir(texto));
    }

    /// <summary>
    /// Reproduce un clip de voz o audio específico.
    /// </summary>
    public void ReproducirVoz(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator RutinaEscribir(string textoCompleto)
    {
        campoTexto.text = "";

        foreach (char letra in textoCompleto)
        {
            campoTexto.text += letra;

            if (sonidoLetra != null && audioSource != null && !char.IsWhiteSpace(letra))
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f); // Variación para que no suene robótico
                audioSource.PlayOneShot(sonidoLetra);
            }

            yield return new WaitForSeconds(velocidadTexto);
        }
    }

    /// <summary>
    /// Oculta el panel y limpia el texto (vincúlalo a una señal de fin de diálogo).
    /// </summary>
    public void OcultarTexto()
    {
        if (rutinaEscribir != null) StopCoroutine(rutinaEscribir);
        if (panelFondo != null) panelFondo.SetActive(false);
        campoTexto.text = "";
    }
}