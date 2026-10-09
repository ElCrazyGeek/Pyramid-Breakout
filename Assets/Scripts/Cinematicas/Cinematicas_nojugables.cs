using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct LineaVN
{
    [Header("Personaje")]
    public string nombreHablante;
    public Sprite retrato;              
    public bool personajeALaDerecha;     

    [Header("Escenario / Render")]
    public Sprite fondoEscena;

    [Header("Texto")]
    [TextArea(3, 5)]
    public string texto;
}

public class Cinematicas_nojugables : MonoBehaviour
{
    public static Cinematicas_nojugables Instance { get; private set; }

    [Header("Paneles Principales")]
    [SerializeField] private GameObject panelVisualNovel;
    [SerializeField] private Image imagenFondo;           

    [Header("Retratos (PNGs)")]
    [SerializeField] private Image retratoIzquierda;
    [SerializeField] private Image retratoDerecha;

    [Header("Caja de Texto")]
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoDialogo;

    private LineaVN[] lineasActuales;
    private int indiceLinea = 0;
    private bool enCinematica = false;
    private Action alTerminarCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);
    }

    public void IniciarCinematicaVN(LineaVN[] lineas, Action alTerminar = null)
    {
        if (lineas == null || lineas.Length == 0) return;

        lineasActuales = lineas;
        indiceLinea = 0;
        alTerminarCallback = alTerminar;
        enCinematica = true;


        Time.timeScale = 0f;

        if (panelVisualNovel != null) panelVisualNovel.SetActive(true);

        MostrarLineaActual();
    }

    private void Update()
    {
        if (!enCinematica) return;


        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            AvanzarLinea();
        }
    }

    private void MostrarLineaActual()
    {
        if (lineasActuales == null || indiceLinea >= lineasActuales.Length) return;

        LineaVN linea = lineasActuales[indiceLinea];

        // 1. Asignar textos
        if (textoNombre != null) textoNombre.text = linea.nombreHablante;
        if (textoDialogo != null) textoDialogo.text = linea.texto;

        // 2. Fondo de escenario si la línea trae uno
        if (imagenFondo != null && linea.fondoEscena != null)
        {
            imagenFondo.gameObject.SetActive(true);
            imagenFondo.sprite = linea.fondoEscena;
        }

        // 3. Atenuación clásica de Visual Novel:
        if (linea.personajeALaDerecha)
        {
            // Habla el de la DERECHA:
            ConfigurarRetrato(retratoDerecha, linea.retrato, iluminado: true);
            AtenuarRetrato(retratoIzquierda); // Se oscurece el de la izquierda
        }
        else
        {
            // Habla el de la IZQUIERDA:
            ConfigurarRetrato(retratoIzquierda, linea.retrato, iluminado: true);
            AtenuarRetrato(retratoDerecha); // Se oscurece el de la derecha
        }
    }

    private void ConfigurarRetrato(Image img, Sprite sprite, bool iluminado)
    {
        if (img == null) return;

        if (sprite != null)
        {
            img.gameObject.SetActive(true);
            img.sprite = sprite;
            // Blanco puro = color normal 100% brillante
            img.color = Color.white;
        }
        // Si no hay sprite asignado en esa línea, conserva el anterior o déjalo activo
    }

    private void AtenuarRetrato(Image img)
    {
        if (img == null || !img.gameObject.activeSelf) return;

        // Color gris oscuro (35% de brillo) con Alfa al 100% para no hacerlo invisible
        img.color = new Color(0.35f, 0.35f, 0.35f, 1f);
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
            CerrarCinematica();
        }
    }

    private void CerrarCinematica()
    {
        enCinematica = false;
        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);


        Time.timeScale = 1f;


        alTerminarCallback?.Invoke();
    }
}