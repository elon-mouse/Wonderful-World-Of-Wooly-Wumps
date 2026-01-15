using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Attack Point")]
    [SerializeField] private Transform attackPoint;

    private PlayerInput playerInput;
    private InputAction attackAction;
    private Animator animator;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Boss"))
        {
            other.GetComponent<BossFight>()?.TakeHit();
        }
    }
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
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
    public void OnAttack(InputAction.CallbackContext context)
    {
        animator.SetBool("isAttacking", true);
        Invoke(nameof(ResetAttack), 0.2f);
        PerformAttack();
    }
    private void ResetAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    // 🔥 ACTUAL ATTACK LOGIC
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
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
