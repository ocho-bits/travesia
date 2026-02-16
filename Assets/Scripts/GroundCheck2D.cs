using UnityEngine;

public sealed class GroundCheck2D : MonoBehaviour
{
    [SerializeField] private Transform groundPoint;
    [SerializeField] private Vector2 boxSize = new Vector2(0.45f, 0.1f);
    [SerializeField] private LayerMask groundMask;

    public bool IsGrounded { get; private set; }

    void FixedUpdate()
    {
        if (groundPoint == null) return;
        IsGrounded = Physics2D.OverlapBox(groundPoint.position, boxSize, 0f, groundMask) != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundPoint.position, boxSize);
    }
#endif
}