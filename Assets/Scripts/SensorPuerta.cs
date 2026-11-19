using UnityEngine;
using Pyramid.Levels;

public class SensorPuerta : MonoBehaviour, IDamageable
{
    [SerializeField] private Animator animatorPuerta;
    [SerializeField] private string triggerAbrir = "Abrir";
    [Min(1)][SerializeField] private int impactosNecesarios = 1;

    private int impactosRecibidos;
    private bool abierta;
    private PuertaContacto contactoPuerta;

    private void Awake()
    {
        if (animatorPuerta == null)
            animatorPuerta = GetComponentInParent<Animator>();

        contactoPuerta = GetComponentInParent<PuertaContacto>();

        if (animatorPuerta == null)
            Debug.LogError("SensorPuerta: no se encontró el Animator de la puerta.", this);

        if (contactoPuerta == null)
            Debug.LogError("SensorPuerta: no se encontró PuertaContacto en la raíz de la puerta.", this);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || abierta)
            return;

        impactosRecibidos++;
        Debug.Log($"Impactos en el sensor: {impactosRecibidos}/{impactosNecesarios}");

        if (impactosRecibidos < impactosNecesarios)
            return;

        abierta = true;
        contactoPuerta?.MarcarAbierta();

        if (animatorPuerta != null)
            animatorPuerta.SetTrigger(triggerAbrir);
    }
}