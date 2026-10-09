using System;
using System.Collections;       // <-- Resuelve el error de IEnumerator
using UnityEngine;
using UnityEngine.UI;           // <-- Resuelve el error de Image
using TMPro;

// --- CLASES Y ESTRUCTURAS SERIALIZABLES ---
[System.Serializable]
public class ActorSlot
{
    public string idActor;                 // "Lyra", "Orion", "Tercero"
    public Image imagenUI;
    public RectTransform transformSlot;    // Punto de anclaje inicial
    [HideInInspector] public RetratoAnimadoUI animacion;
}

// --- GESTOR PRINCIPAL DE CINEMÁTICAS ---
public class Cinematicas_nojugables : MonoBehaviour
{
    public static Cinematicas_nojugables Instance { get; private set; }

    [Header("Configuración de Slots de Actores")]
    [SerializeField] private ActorSlot[] actores;

    [Header("Paneles UI")]
    [SerializeField] private GameObject panelVisualNovel;
    [SerializeField] private Image imagenFondo;
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoDialogo;

    private LineaVN[] lineasActuales;
    private int indiceLinea = 0;
    private bool enCinematica = false;
    private Action alTerminarCallback;
    private Coroutine rutinaAutoAvance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inicializar slots
        foreach (var actor in actores)
        {
            if (actor.imagenUI != null)
            {
                actor.animacion = actor.imagenUI.GetComponent<RetratoAnimadoUI>();
                if (actor.transformSlot != null)
                {
                    actor.imagenUI.rectTransform.position = actor.transformSlot.position;
                }
                // Por defecto arrancan apagados hasta que una línea los llame
                actor.imagenUI.gameObject.SetActive(false);
            }
        }

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
            if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
            AvanzarLinea();
        }
    }

    private void MostrarLineaActual()
    {
        if (lineasActuales == null || indiceLinea >= lineasActuales.Length) return;

        LineaVN linea = lineasActuales[indiceLinea];

        // 1. Textos
        if (textoNombre != null) textoNombre.text = linea.nombreAMostrar;
        if (textoDialogo != null) textoDialogo.text = linea.texto;

        // 2. Fondo
        if (imagenFondo != null && linea.fondoEscena != null)
        {
            imagenFondo.gameObject.SetActive(true);
            imagenFondo.sprite = linea.fondoEscena;
        }

        // 3. Aplicar cambios a los actores específicos de esta línea
        if (linea.cambiosActores != null)
        {
            foreach (var cambio in linea.cambiosActores)
            {
                ActorSlot slot = BuscarActor(cambio.idActor);
                if (slot == null || slot.imagenUI == null) continue;

                // A. ¿Desaparece o Aparece?
                slot.imagenUI.gameObject.SetActive(cambio.visible);

                if (cambio.visible)
                {
                    // B. ¿Cambia de expresión/pose?
                    if (cambio.nuevaExpresion != null)
                    {
                        slot.imagenUI.sprite = cambio.nuevaExpresion;
                    }

                    // C. ¿Cambia de posición física (slot)?
                    if (cambio.moverASlot != null)
                    {
                        slot.imagenUI.rectTransform.position = cambio.moverASlot.position;
                    }
                }
            }
        }

        // 4. Iluminar a quien le toca hablar y atenuar a los que escuchan
        foreach (var actor in actores)
        {
            if (actor.imagenUI == null || !actor.imagenUI.gameObject.activeSelf) continue;

            bool esElHablante = actor.idActor.Equals(linea.idActorHablante, StringComparison.OrdinalIgnoreCase);

            if (esElHablante)
            {
                if (actor.animacion != null) actor.animacion.PonerEnPrimerPlano(null);
                else actor.imagenUI.color = Color.white;
            }
            else
            {
                if (actor.animacion != null) actor.animacion.Atenuar();
                else actor.imagenUI.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            }
        }

        // 5. Temporizador automático
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        if (linea.tiempoEnPantalla > 0f)
        {
            rutinaAutoAvance = StartCoroutine(TemporizadorLinea(linea.tiempoEnPantalla));
        }
    }

    private ActorSlot BuscarActor(string id)
    {
        foreach (var actor in actores)
        {
            if (actor.idActor.Equals(id, StringComparison.OrdinalIgnoreCase)) return actor;
        }
        return null;
    }

    private IEnumerator TemporizadorLinea(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        AvanzarLinea();
    }

    private void AvanzarLinea()
    {
        indiceLinea++;
        if (indiceLinea < lineasActuales.Length) MostrarLineaActual();
        else CerrarCinematica();
    }

    private void CerrarCinematica()
    {
        if (rutinaAutoAvance != null) StopCoroutine(rutinaAutoAvance);
        enCinematica = false;
        if (panelVisualNovel != null) panelVisualNovel.SetActive(false);
        Time.timeScale = 1f;

        alTerminarCallback?.Invoke();
    }
}