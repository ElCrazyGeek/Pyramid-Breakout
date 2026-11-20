using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pyramid.Levels;

public sealed class PuertaContacto : MonoBehaviour, IDamageable
{
    [Header("Apertura por Disparos del Jugador")]
    [Tooltip("Cantidad de impactos de disparo necesarios para abrir la puerta")]
    [Min(1)][SerializeField] private int disparosParaAbrir = 1;
    [SerializeField] private string tagJugador = "Player";
    [Tooltip("Opcional: animator de la puerta para disparar el trigger al abrirse")]
    [SerializeField] private Animator animatorPuerta;
    [SerializeField] private string triggerAbrirAnimator = "Abrir";
    [Tooltip("Evento opcional en Inspector (sonido, desactivar collider bloqueador, partículas, etc.)")]
    [SerializeField] private UnityEvent alAbrirse;

    [Header("Daño por Contacto con la Nave")]
    [Min(0f)][SerializeField] private float danioAbierta = 10f;
    [Min(0f)][SerializeField] private float cooldownContacto = 0.5f;

    private readonly Dictionary<Health, float> proximoGolpePorJugador = new Dictionary<Health, float>();
    private bool abierta;
    private int impactosRecibidos;

    public bool EstaAbierta => abierta;

    // Implementación de IDamageable para compatibilidad con tus proyectiles
    public void TakeDamage(float amount)
    {
        // Nota: Si un script externo llama a TakeDamage directamente sin pasar por el filtro de proyectil,
        // no abrimos aquí para garantizar que SOLO se abra con disparos del jugador.
    }

    private void OnTriggerEnter(Collider other)
    {
        EvaluarDisparoJugador(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluarDisparoJugador(collision.gameObject);
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
        // 1. Proyectil normal
        var proyectilNormal = obj.GetComponentInParent<Proyectil>();
        if (proyectilNormal != null)
            return EsOrigenJugador(proyectilNormal.tagOrigen, proyectilNormal.origen);

        // 2. Proyectil rayo
        var proyectilRayo = obj.GetComponentInParent<ProyectilRayo>();
        if (proyectilRayo != null)
            return EsOrigenJugador(proyectilRayo.tagOrigen, proyectilRayo.origen);

        // 3. Proyectil cargado (si existe en el proyecto)
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
        saludJugador.TakeDamage(abierta ? danioAbierta : saludJugador.Current);
    }
}