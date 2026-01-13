using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;

    [Header("Movement")]
    public float speed = 2f;
    public float chaseDistance = 5f;
    public float attackDistance = 1.2f;

    [Header("Attack")]
    public float attackCooldown = 1f;

    private float lastAttackTime;

    void Update()
    {
        if (!player) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // ATTACK
        if (distance <= attackDistance)
        {
            animator.SetBool("isWalking", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }

            return;
        }

        // CHASE
        if (distance <= chaseDistance)
        {
            MoveTowardPlayer();
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    void MoveTowardPlayer()
    {
        float directionX = Mathf.Sign(player.position.x - transform.position.x);

        transform.position = new Vector2(
            transform.position.x + directionX * speed * Time.deltaTime,
            transform.position.y
        );

        animator.SetBool("isWalking", true);

        // Flip sprite
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * directionX;
        transform.localScale = scale;
    }

    void Attack()
    {
        animator.SetTrigger("attack");
        // Damage logic goes here (overlap circle, raycast, etc.)
    }
}
