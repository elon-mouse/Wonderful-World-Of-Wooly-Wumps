using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public HealthBarUI healthBar; // Optional (player / enemies)

    void Start()
    {
        ResetHealth();
        Debug.Log(gameObject.name + " health initialized at " + currentHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
            healthBar.ResetBar();

        Debug.Log($"[HealthSystem] {name} health reset to {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth, maxHealth);

        Debug.Log(gameObject.name + " took " + amount + " damage. Remaining health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died.");

        // Player logic
        if (CompareTag("Player"))
        {
            Debug.Log("[HealthSystem] Player died — waiting for respawn");
            return;
        }

        // Enemy logic
        Destroy(gameObject);
    }
}
