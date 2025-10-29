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
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            HealthSystem h = player.GetComponent<HealthSystem>();
            if (h != null)
            {
                h.TakeDamage(damage);
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
