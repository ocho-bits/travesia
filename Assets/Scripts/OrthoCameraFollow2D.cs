using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class OrthoCameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.0f);
    [SerializeField] private float smoothTimeX = 0.20f;
    [SerializeField] private float smoothTimeY = 0.25f;

    [Header("Axis locks")]
    [SerializeField] private bool lockY = false;

    private float _velX;
    private float _velY;

    void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        float targetX = target.position.x + offset.x;
        float targetY = lockY ? pos.y : (target.position.y + offset.y);

        float x = Mathf.SmoothDamp(pos.x, targetX, ref _velX, smoothTimeX);
        float y = Mathf.SmoothDamp(pos.y, targetY, ref _velY, smoothTimeY);

        transform.position = new Vector3(x, y, pos.z);
    }

    public void SetTarget(Transform t) => target = t;
}