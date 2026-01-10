using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 5f;
    public float attackRange = 1f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    private Rigidbody2D rb;
    private float lastAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);

        if (distanceX <= attackRange)
        {
            TryAttack();
        }
        else if (distanceX <= chaseRange)
        {
            ChasePlayer();
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void ChasePlayer()
    {
        float direction = Mathf.Sign(player.position.x - transform.position.x);

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // Flip sprite
        transform.localScale = new Vector3(
            direction,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    private void TryAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        HealthSystem health = player.GetComponent<HealthSystem>();
        if (health != null)
        {
            Debug.Log($"{name} attacks player");
            health.TakeDamage(attackDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
