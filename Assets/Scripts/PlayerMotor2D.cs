using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMotor2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInput2D input;
    [SerializeField] private GroundCheck2D groundCheck;

    [Header("Move")]
    [SerializeField] private float maxSpeed = 7.5f;
    [SerializeField] private float accel = 60f;
    [SerializeField] private float decel = 70f;
    [SerializeField] private float airControlMultiplier = 0.65f;

    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 12.5f;
    [SerializeField] private float coyoteTime = 0.10f;
    [SerializeField] private float jumpBuffer = 0.10f;
    [SerializeField] private float jumpCutMultiplier = 0.5f; // lower = stronger cut

    [Header("Gravity Feel")]
    [SerializeField] private float fallGravityMultiplier = 1.7f;
    [SerializeField] private float maxFallSpeed = 22f;

    private Rigidbody2D rb;

    private float coyoteTimer;
    private float jumpBufferTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (input == null) input = GetComponent<PlayerInput2D>();
        if (groundCheck == null) groundCheck = GetComponentInChildren<GroundCheck2D>();
    }

    void Update()
    {
        // Coyote timer
        if (groundCheck != null && groundCheck.IsGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Jump buffer timer
        if (input != null && input.JumpPressedThisFrame) jumpBufferTimer = jumpBuffer;
        else jumpBufferTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (input == null) return;

        HandleMove();
        HandleJump();
        HandleGravityFeel();
    }

    void HandleMove()
    {
        float target = input.MoveX * maxSpeed;
        float speedDiff = target - rb.linearVelocity.x;

        bool grounded = groundCheck != null && groundCheck.IsGrounded;
        float control = grounded ? 1f : airControlMultiplier;

        float rate = (Mathf.Abs(target) > 0.01f) ? accel : decel;
        float movement = speedDiff * rate * control;

        rb.AddForce(new Vector2(movement, 0f), ForceMode2D.Force);

        // Clamp horizontal speed
        float vx = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    void HandleJump()
    {
        bool canJump = coyoteTimer > 0f;
        bool wantsJump = jumpBufferTimer > 0f;

        if (canJump && wantsJump)
        {
            // Execute jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }

        // Jump cut: if player releases jump while rising, cut velocity
        if (rb.linearVelocity.y > 0f && input != null && !input.JumpHeld)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    void HandleGravityFeel()
    {
        // Faster fall
        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Vector2.down * Physics2D.gravity.y * (fallGravityMultiplier - 1f), ForceMode2D.Force);
        }

        // Clamp fall speed
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }
}
