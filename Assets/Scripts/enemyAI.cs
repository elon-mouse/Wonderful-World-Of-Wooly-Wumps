using UnityEngine;

public class EnemyAI2D : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3f;
    public float chaseRange = 8f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;

    private Rigidbody2D rb;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        float distance = Vector2.Distance(player.position, transform.position);

        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Debug.Log("Enemy attacks!");
                lastAttackTime = Time.time;
            }
        }
        else if (distance <= chaseRange)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
