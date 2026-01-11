using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public float damage = 10f;
    public float attackRange = 1.5f;
    public float attackRate = 1f;
    private float nextAttackTime;
    public Transform player;

    void Update()
    {
        if (player == null)
        {
            Debug.LogWarning(name + " has no player target assigned!");
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            Debug.Log(name + " is attacking player at distance " + distance);
            HealthSystem h = player.GetComponent<HealthSystem>();
            if (h != null)
            {
                h.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("Player has no HealthSystem component!");
            }

            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
