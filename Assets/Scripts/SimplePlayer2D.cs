using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SimplePlayer2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform groundPoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpVelocity = 12.5f;

    [Header("Ground")]
    [SerializeField] private Vector2 groundBoxSize = new Vector2(0.45f, 0.12f);
    [SerializeField] private LayerMask groundMask = 1 << 3;

    [Header("Facing")]
    [SerializeField] private bool preferSpriteFlip = true;
    [SerializeField] private float facingDeadZone = 0.02f;

    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int OverlayHash = Animator.StringToHash("Overlay");
    private static readonly int JumpStateHash = Animator.StringToHash("Base Layer.Jump");

    private Coroutine overlayRoutine;
    private Vector3 visualBaseScale = Vector3.one;
    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;

    private void Reset()
    {
        AutoResolveReferences();
    }

    private void Awake()
    {
        AutoResolveReferences();

        if (visualRoot != null)
        {
            visualBaseScale = visualRoot.localScale;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void Update()
    {
        ReadInput();
        RefreshGrounded();
        UpdateAnimatorParameters();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        rb.linearVelocity = velocity;

        RefreshGrounded();

        if (!jumpRequested || !isGrounded)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
        jumpRequested = false;
        isGrounded = false;

        if (animator != null)
        {
            animator.SetBool(GroundedHash, false);
            animator.Play(JumpStateHash, 0, 0f);
        }
    }

    public void SetOverlay(int id)
    {
        if (animator == null)
        {
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

    private void ReadInput()
    {
        float x = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpRequested = true;
            }
        }

        if (Gamepad.current != null)
        {
            float stick = Gamepad.current.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > 0.1f)
            {
                x = stick;
            }

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                jumpRequested = true;
            }
        }

        moveInput = Mathf.Clamp(x, -1f, 1f);
    }

    private void RefreshGrounded()
    {
        if (groundPoint == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapBox(groundPoint.position, groundBoxSize, 0f, groundMask) != null;
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null || rb == null)
        {
            return;
        }

        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(GroundedHash, isGrounded);
    }

    private void UpdateFacing()
    {
        float facing = moveInput;

        if (Mathf.Abs(facing) < facingDeadZone)
        {
            facing = rb != null ? rb.linearVelocity.x : 0f;
        }

        if (Mathf.Abs(facing) < facingDeadZone)
        {
            return;
        }

        bool faceLeft = facing < 0f;

        if (preferSpriteFlip && spriteRenderer != null)
        {
            spriteRenderer.flipX = faceLeft;
            return;
        }

        if (visualRoot != null)
        {
            Vector3 scale = visualBaseScale;
            scale.x = Mathf.Abs(visualBaseScale.x) * (faceLeft ? -1f : 1f);
            visualRoot.localScale = scale;
        }
    }

    private void AutoResolveReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (visualRoot == null)
        {
            Transform visual = transform.Find("Visual");
            if (visual == null)
            {
                visual = transform.Find("Graphics");
            }

            visualRoot = visual;
        }

        if (spriteRenderer == null && visualRoot != null)
        {
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null && visualRoot != null)
        {
            animator = visualRoot.GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (groundPoint == null)
        {
            groundPoint = transform.Find("GroundPoint");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundPoint == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundPoint.position, groundBoxSize);
    }
#endif
}
