using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        Debug.Log($"[HealthSystem] {name} health reset to {currentHealth}/{maxHealth}");
    }


    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log(gameObject.name + " health initialized at " + currentHealth);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + " took " + amount + " damage. Remaining health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died.");

        // If this is the player, DO NOT destroy it
        if (CompareTag("Player"))
        {
            Debug.Log("[HealthSystem] Player died — waiting for respawn");
            return;
        }

        // Enemies still get destroyed
        Destroy(gameObject);
    }
}