using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct LineaVN
{
    public string nombreHablante;
    public Sprite retrato;              // PNG del personaje
    public bool personajeALaDerecha;     // False = izquierda, True = derecha
    [TextArea(3, 5)]
    public string texto;
}

public class Cinematicas_nojugables : MonoBehaviour
{
    public static Cinematicas_nojugables Instance { get; private set; }

    [Header("Panel Principal")]
    [SerializeField] private GameObject panelVisualNovel;
    [SerializeField] private Image imagenFondoOscuro; // Un panel negro con transparencia

    [Header("Imágenes de Personajes (PNGs)")]
    [SerializeField] private Image retratoIzquierda;
    [SerializeField] private Image retratoDerecha;

    [Header("Caja de Diálogo")]
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoDialogo;
    [SerializeField] private GameObject indicadorPresioneTecla; // Icono parpadeante de "flecha"

    private LineaVN[] lineasActuales;
    private int indiceLinea = 0;
    private bool enCinematica = false;
    private Action onCinematicaTerminada;

    private void Awake()
    {
        Instance = this;
        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);
    }

    public void IniciarCinematicaVN(LineaVN[] lineas, Action alTerminar = null)
    {
        if (lineas == null || lineas.Length == 0) return;

        lineasActuales = lineas;
        indiceLinea = 0;
        onCinematicaTerminada = alTerminar;
        enCinematica = true;

        // 1. Pausar el juego
        Time.timeScale = 0f;

        // 2. Encender la interfaz
        panelVisualNovel.SetActive(true);

        MostrarLineaActual();
    }

    private void Update()
    {
        if (!enCinematica) return;

        // Avanzar con Clic Izquierdo, Espacio o Enter
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            AvanzarLinea();
        }
    }

    private void MostrarLineaActual()
    {
        LineaVN linea = lineasActuales[indiceLinea];

        textoNombre.text = linea.nombreHablante;
        textoDialogo.text = linea.texto;

        // Configuración de los PNGs a los lados
        if (linea.personajeALaDerecha)
        {
            ConfigurarRetrato(retratoDerecha, linea.retrato, activo: true);
            AtenuarRetrato(retratoIzquierda); // Se apaga un poco el que no habla
        }
        else
        {
            ConfigurarRetrato(retratoIzquierda, linea.retrato, activo: true);
            AtenuarRetrato(retratoDerecha);
        }
    }

    private void ConfigurarRetrato(Image img, Sprite sprite, bool activo)
    {
        if (img == null) return;

        if (sprite != null)
        {
            img.gameObject.SetActive(true);
            img.sprite = sprite;
            img.color = Color.white; // Color normal (iluminado)
        }
        else
        {
            img.gameObject.SetActive(false);
        }
    }

    private void AtenuarRetrato(Image img)
    {
        if (img == null || !img.gameObject.activeSelf) return;
        // Efecto clásico VN: el que no habla se oscurece al 50%
        img.color = new Color(0.4f, 0.4f, 0.4f, 1f);
    }

    private void AvanzarLinea()
    {
        indiceLinea++;

        if (indiceLinea < lineasActuales.Length)
        {
            MostrarLineaActual();
        }
        else
        {
            TerminarCinematica();
        }
    }

    private void TerminarCinematica()
    {
        enCinematica = false;
        panelVisualNovel.SetActive(false);

        // Reanudar el tiempo del motor
        Time.timeScale = 1f;

        // Ejecutar acción posterior (ej. Cargar siguiente nivel, abrir pantalla de victoria)
        onCinematicaTerminada?.Invoke();
    }
}