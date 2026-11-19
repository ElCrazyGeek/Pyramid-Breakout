using System.Collections.Generic;
using UnityEngine;
using Pyramid.Levels;

public sealed class PuertaContacto : MonoBehaviour
{
    [Min(0f)][SerializeField] private float danioAbierta = 10f;
    [Min(0f)][SerializeField] private float cooldownContacto = 0.5f;

    private readonly Dictionary<Health, float> proximoGolpePorJugador = new Dictionary<Health, float>();
    private bool abierta;

    public void MarcarAbierta()
    {
        abierta = true;
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
