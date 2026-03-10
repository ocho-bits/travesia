using System.Collections;
using UnityEngine;

public sealed class PlayerAnimationDriver2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform graphicsTransform;
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private PlayerInput2D input;
    [SerializeField] private GroundCheck2D groundCheck;
    [SerializeField] private Rigidbody2D rb;

    [Header("Facing")]
    [SerializeField] private bool preferSpriteFlip = true;
    [SerializeField] private float facingDeadZone = 0.02f;

    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int OverlayHash = Animator.StringToHash("Overlay");

    private const float JumpQueueWindow = 0.2f;
    private const float JumpTakeoffVelocityThreshold = 0.05f;

    private Coroutine overlayRoutine;
    private Vector3 graphicsBaseScale = Vector3.one;
    private bool wasGrounded;
    private bool jumpQueued;
    private float jumpQueueExpiresAt;

    private bool loggedMissingAnimator;
    private bool loggedMissingMotor;
    private bool loggedMissingGroundCheck;
    private bool loggedMissingRb;
    private bool loggedMissingSpriteTarget;

    private void Reset()
    {
        AutoResolveReferences();
    }

    private void Awake()
    {
        AutoResolveReferences();

        if (graphicsTransform != null)
        {
            graphicsBaseScale = graphicsTransform.localScale;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (groundCheck != null)
        {
            wasGrounded = groundCheck.IsGrounded;
        }
    }

    private void Update()
    {
        if (!ValidateReferences())
        {
            return;
        }

        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);
        bool grounded = groundCheck.IsGrounded;

        animator.SetFloat(SpeedHash, horizontalSpeed);
        animator.SetBool(GroundedHash, grounded);

        // Queue the player's existing jump input and fire Jump only when the
        // motor has actually begun takeoff, keeping animation aligned to physics.
        if (input != null && input.JumpPressedThisFrame)
        {
            jumpQueued = true;
            jumpQueueExpiresAt = Time.time + JumpQueueWindow;
        }

        if (jumpQueued && Time.time > jumpQueueExpiresAt)
        {
            jumpQueued = false;
        }

        if (jumpQueued && !grounded && rb.linearVelocity.y > JumpTakeoffVelocityThreshold)
        {
            animator.SetTrigger(JumpHash);
            jumpQueued = false;
        }

        if (!wasGrounded && grounded)
        {
            jumpQueued = false;
        }

        UpdateFacing();
        wasGrounded = grounded;
    }

    public void SetOverlay(int id)
    {
        if (animator == null)
        {
            if (!loggedMissingAnimator)
            {
                Debug.LogError("[PlayerAnimationDriver2D] Animator is missing. Cannot set overlay.", this);
                loggedMissingAnimator = true;
            }

            return;
        }

        animator.SetInteger(OverlayHash, id);
    }

    public void ClearOverlay()
    {
        SetOverlay(0);
    }

    public void PlayOverlayForSeconds(int id, float seconds)
    {
        if (overlayRoutine != null)
        {
            StopCoroutine(overlayRoutine);
            overlayRoutine = null;
        }

        overlayRoutine = StartCoroutine(PlayOverlayRoutine(id, seconds));
    }

    private IEnumerator PlayOverlayRoutine(int id, float seconds)
    {
        SetOverlay(id);
        yield return new WaitForSeconds(seconds);
        ClearOverlay();
        overlayRoutine = null;
    }

    private void UpdateFacing()
    {
        float facingInput = rb.linearVelocity.x;

        if (Mathf.Abs(facingInput) < facingDeadZone && input != null)
        {
            facingInput = input.MoveX;
        }

        if (Mathf.Abs(facingInput) < facingDeadZone)
        {
            return;
        }

        bool faceLeft = facingInput < 0f;

        if (preferSpriteFlip && spriteRenderer != null)
        {
            spriteRenderer.flipX = faceLeft;
            return;
        }

        if (graphicsTransform != null)
        {
            Vector3 scale = graphicsBaseScale;
            scale.x = Mathf.Abs(graphicsBaseScale.x) * (faceLeft ? -1f : 1f);
            graphicsTransform.localScale = scale;
            return;
        }

        if (!loggedMissingSpriteTarget)
        {
            Debug.LogError("[PlayerAnimationDriver2D] No SpriteRenderer or Graphics Transform available for facing flip.", this);
            loggedMissingSpriteTarget = true;
        }
    }

    private void AutoResolveReferences()
    {
        if (motor == null)
        {
            motor = GetComponent<PlayerMotor2D>();
        }

        if (input == null)
        {
            input = GetComponent<PlayerInput2D>();
        }

        if (groundCheck == null)
        {
            groundCheck = GetComponentInChildren<GroundCheck2D>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        Transform graphics = transform.Find("Graphics");
        if (graphicsTransform == null)
        {
            graphicsTransform = graphics;
        }

        if (spriteRenderer == null)
        {
            if (graphicsTransform != null)
            {
                spriteRenderer = graphicsTransform.GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (animator == null)
        {
            if (graphicsTransform != null)
            {
                animator = graphicsTransform.GetComponent<Animator>();
            }

            if (animator == null && spriteRenderer != null)
            {
                animator = spriteRenderer.GetComponent<Animator>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    private bool ValidateReferences()
    {
        bool ok = true;

        if (motor == null)
        {
            if (!loggedMissingMotor)
            {
                Debug.LogError("[PlayerAnimationDriver2D] Missing PlayerMotor2D reference.", this);
                loggedMissingMotor = true;
            }

            ok = false;
        }

        if (groundCheck == null)
        {
            if (!loggedMissingGroundCheck)
            {
                Debug.LogError("[PlayerAnimationDriver2D] Missing GroundCheck2D reference. Cannot drive Grounded parameter.", this);
                loggedMissingGroundCheck = true;
            }

            ok = false;
        }

        if (rb == null)
        {
            if (!loggedMissingRb)
            {
                Debug.LogError("[PlayerAnimationDriver2D] Missing Rigidbody2D reference. Cannot drive Speed/facing.", this);
                loggedMissingRb = true;
            }

            ok = false;
        }

        if (animator == null)
        {
            if (!loggedMissingAnimator)
            {
                Debug.LogError("[PlayerAnimationDriver2D] Missing Animator reference.", this);
                loggedMissingAnimator = true;
            }

            ok = false;
        }

        return ok;
    }
}
