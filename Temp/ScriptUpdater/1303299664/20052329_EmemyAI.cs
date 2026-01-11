using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2f;
    public float chaseRange = 5f;
    public float attackRange = 1f;
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
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        float dx = player.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);

        if (distance <= attackRange)
        {
            rb.linearVelocity = new Vector2(0f, 0f);
            TryAttack();
        }
        else if (distance <= chaseRange)
        {
            float direction = Mathf.Sign(dx);
            rb.linearVelocity = new Vector2(direction * moveSpeed, 0f);

            transform.localScale = new Vector3(
                direction,
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

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
