using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float damage = 20f;
    public float attackRange = 1.5f;
    public KeyCode attackKey = KeyCode.E; // Changed from Space to E
    public LayerMask enemyLayer = 999; // Assign "Enemy" layer in Inspector

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }
    }

    void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            HealthSystem h = enemy.GetComponent<HealthSystem>();
            if (h != null)
            {
                h.TakeDamage(damage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
