using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float jumpingPower = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    private float horizontal;
    private bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError("❌ PlayerInput component missing!");
            enabled = false;
            return;
        }

        if (playerInput.actions == null)
        {
            Debug.LogError("❌ PlayerInput has no Input Actions assigned!");
            enabled = false;
            return;
        }

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];

        if (moveAction == null || jumpAction == null)
        {
            Debug.LogError("❌ Move or Jump action not found. Check Action Map name!");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed += OnJump;
            jumpAction.canceled += OnJumpCancel;
        }
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.canceled -= OnJumpCancel;
        }
    }

    private void Update()
    {
        if (moveAction == null) return;

        horizontal = moveAction.ReadValue<Vector2>().x;
        Flip();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
        }
    }

    private void OnJumpCancel(InputAction.CallbackContext context)
    {
        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }

    private void Flip()
    {
        if ((isFacingRight && horizontal < 0f) || (!isFacingRight && horizontal > 0f))
        {
            isFacingRight = !isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }
    }
}
