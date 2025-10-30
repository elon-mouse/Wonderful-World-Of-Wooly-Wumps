using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float damage = 20f;
    public float attackRange = 1.5f;
    public KeyCode attackKey = KeyCode.E;
    public LayerMask enemyLayer; // Assign in Inspector

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            Debug.Log("Player pressed attack key");
            Attack();
        }
    }

    void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        Debug.Log("Attack triggered. Detected " + hitEnemies.Length + " targets.");

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("Hit " + enemy.name);
            HealthSystem h = enemy.GetComponent<HealthSystem>();
            if (h != null)
            {
                h.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning(enemy.name + " has no HealthSystem component!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
