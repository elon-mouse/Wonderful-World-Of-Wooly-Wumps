using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] float damage = 20f;
    [SerializeField] float range = 1.5f;
    [SerializeField] LayerMask enemyLayer;

    PlayerInput playerInput;
    InputAction attackAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput.actions["Attack"];
    }

    void OnEnable()
    {
        attackAction.performed += OnAttack;
    }

    void OnDisable()
    {
        attackAction.performed -= OnAttack;
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            range,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            HealthSystem h = hit.GetComponent<HealthSystem>();
            if (h != null)
                h.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
