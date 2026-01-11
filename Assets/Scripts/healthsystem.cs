using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public HealthBarUI healthBar;

    void Start()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
            healthBar.ResetBar();
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (CompareTag("Player"))
        {
            PlayerRespawn respawn = GetComponent<PlayerRespawn>();
            if (respawn != null)
                respawn.ForceRespawn();
            return;
        }

        Destroy(gameObject);
    }
}
