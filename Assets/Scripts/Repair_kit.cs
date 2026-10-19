using UnityEngine;
using Pyramid.Levels;
public class Repair_kit : MonoBehaviour
{

    public float repairAmount = 20f; // Amount of health to repair

    void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;



        // Check if the other object has a Health component
        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            // Repair the health
            health.RepairDamage(repairAmount);
            Debug.Log($"[Repair_kit] Repaired {repairAmount} health for {other.gameObject.name}. Current health: {health.Current}/{health.maximum}");

            // Optionally, destroy the repair kit after use
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
