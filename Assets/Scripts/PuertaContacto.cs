using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pyramid.Levels;

public sealed class PuertaContacto : MonoBehaviour, IDamageable
{
    [Header("Apertura por Disparos del Jugador")]
    [Min(1)][SerializeField] private int disparosParaAbrir = 1;
    [SerializeField] private string tagJugador = "Player";
    [SerializeField] private Animator animatorPuerta;
    [SerializeField] private string triggerAbrirAnimator = "Abrir";
    [SerializeField] private UnityEvent alAbrirse;

    [Header("Colliders de la Puerta")]
    [Tooltip("Opcional: arrastra colliders específicos. Si se deja vacío, tomará todos los de este objeto y sus hijos.")]
    [SerializeField] private Collider[] collidersPuerta;
    [Tooltip("Si está marcado, al abrirse los colliders pasan a ser Is Trigger para no detener la nave.")]
    [SerializeField] private bool convertirATriggerAlAbrir = true;

    [Header("Daño por Contacto")]
    [Tooltip("Daño que recibe el jugador si roza la puerta cuando ya está abierta (sin detener su avance)")]
    [Min(0f)][SerializeField] private float danioAbierta = 10f;
    [Min(0f)][SerializeField] private float cooldownContacto = 0.5f;

    private readonly Dictionary<Health, float> proximoGolpePorJugador = new Dictionary<Health, float>();
    private bool abierta;
    private int impactosRecibidos;

    public bool EstaAbierta => abierta;

    private void Awake()
    {
        if (collidersPuerta == null || collidersPuerta.Length == 0)
        {
            collidersPuerta = GetComponentsInChildren<Collider>(true);
        }
    }

    public void TakeDamage(float amount) { }

    private void OnTriggerEnter(Collider other)
    {
        EvaluarImpactoTrigger(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluarDisparoJugador(collision.gameObject);
    }

    private void EvaluarImpactoTrigger(GameObject objetoImpacto)
    {
        if (objetoImpacto == null)
            return;

        // 1. Si está cerrada, evaluar si es un disparo del jugador para abrirla
        if (!abierta)
        {
            EvaluarDisparoJugador(objetoImpacto);
            return;
        }

        // 2. Si ya está abierta (y sus colliders son Trigger), dañar al jugador sin frenarlo si la roza
        var saludJugador = objetoImpacto.GetComponentInParent<Health>() ?? objetoImpacto.transform.root.GetComponentInChildren<Health>();
        if (saludJugador != null && (objetoImpacto.CompareTag(tagJugador) || objetoImpacto.transform.root.CompareTag(tagJugador)))
        {
            ProcesarContacto(saludJugador);
        }
    }

    private void EvaluarDisparoJugador(GameObject objetoImpacto)
    {
        if (abierta || objetoImpacto == null)
            return;

        if (!EsDisparoDelJugador(objetoImpacto))
            return;

        impactosRecibidos++;
        if (impactosRecibidos >= disparosParaAbrir)
        {
            MarcarAbierta();
        }
    }

    private bool EsDisparoDelJugador(GameObject obj)
    {
        var proyectilNormal = obj.GetComponentInParent<Proyectil>();
        if (proyectilNormal != null)
            return EsOrigenJugador(proyectilNormal.tagOrigen, proyectilNormal.origen);

        var proyectilRayo = obj.GetComponentInParent<ProyectilRayo>();
        if (proyectilRayo != null)
            return EsOrigenJugador(proyectilRayo.tagOrigen, proyectilRayo.origen);

        var proyectilCargado = obj.GetComponentInParent<ProyectilCargado>();
        if (proyectilCargado != null)
            return EsOrigenJugador(proyectilCargado.tagOrigen, proyectilCargado.origen);

        return false;
    }

    private bool EsOrigenJugador(string tagOrigen, GameObject origen)
    {
        if (!string.IsNullOrEmpty(tagOrigen) && tagOrigen == tagJugador)
            return true;

        if (origen != null && (origen.CompareTag(tagJugador) || origen.transform.root.CompareTag(tagJugador)))
            return true;

        return false;
    }

    public void MarcarAbierta()
    {
        if (abierta)
            return;

        abierta = true;

        if (convertirATriggerAlAbrir && collidersPuerta != null)
        {
            foreach (var col in collidersPuerta)
            {
                if (col != null)
                {
                    col.isTrigger = true;
                }
            }
        }

        if (animatorPuerta != null && !string.IsNullOrEmpty(triggerAbrirAnimator))
        {
            animatorPuerta.SetTrigger(triggerAbrirAnimator);
        }

        alAbrirse?.Invoke();
    }

    public void ProcesarContacto(Health saludJugador)
    {
        if (saludJugador == null || saludJugador.Current <= 0f)
            return;

        if (proximoGolpePorJugador.TryGetValue(saludJugador, out float proximoGolpe) && Time.time < proximoGolpe)
            return;

        proximoGolpePorJugador[saludJugador] = Time.time + Mathf.Max(0f, cooldownContacto);

        // Si está abierta aplica danioAbierta; si está cerrada aplica daño letal completo
        saludJugador.TakeDamage(abierta ? danioAbierta : saludJugador.Current);
    }
}