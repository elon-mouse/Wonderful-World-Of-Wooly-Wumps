using UnityEngine;

public class BossJumpAttack : MonoBehaviour
{
    public Transform player;
    public Rigidbody2D rb;
    public float jumpForce = 14f;

    public void JumpAtPlayer()
    {
        if (player == null || rb == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * jumpForce;
    }
}
