using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    private PlayerInput playerInput;
    private InputAction attackAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput.actions["Attack"];

        Debug.Log("PlayerAttack initialized. Attack action found: " + (attackAction != null));
    }

    private void OnEnable()
    {
        attackAction.performed += OnAttack;
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttack;
    }

    // 🔥 CALLED BY INPUT SYSTEM
    private void OnAttack(InputAction.CallbackContext context)
    {
        PerformAttack();
    }

    // 🔥 ACTUAL ATTACK LOGIC
    [SerializeField] private Transform attackPoint;

    private void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        Debug.Log($"Attack triggered. Hit count: {hitEnemies.Length}");

        foreach (Collider2D enemy in hitEnemies)
        {
            HealthSystem health = enemy.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log("Damaged: " + enemy.name);
            }
        }
    }


    // 🎯 VISUAL DEBUG
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector2 center = (Vector2)transform.position + direction * attackRange;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}
