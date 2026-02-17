using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class OrthoZoomDirector2D : MonoBehaviour
{
    [Header("Zoom Limits")]
    [SerializeField] private float minOrtho = 4.5f;
    [SerializeField] private float maxOrtho = 9.0f;

    [Header("Default Zoom (on scene start)")]
    [SerializeField] private float defaultZoom = 7.0f;

    [Header("Fallback Transition (if trigger doesn't override)")]
    [SerializeField] private float defaultDuration = 0.25f;
    [SerializeField] private AnimationCurve defaultCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Camera _cam;

    // Transition state
    private bool _isTransitioning;
    private float _startZoom;
    private float _targetZoom;
    private float _elapsed;
    private float _duration;
    private AnimationCurve _curve;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;

        float z = Mathf.Clamp(defaultZoom, minOrtho, maxOrtho);
        _cam.orthographicSize = z;
    }

    void LateUpdate()
    {
        if (!_isTransitioning) return;

        _elapsed += Time.deltaTime;
        float t = (_duration <= 0.0001f) ? 1f : Mathf.Clamp01(_elapsed / _duration);

        float k = (_curve != null) ? _curve.Evaluate(t) : t;
        _cam.orthographicSize = Mathf.Lerp(_startZoom, _targetZoom, k);

        if (t >= 1f)
            _isTransitioning = false;
    }

    // Simple API: uses director defaults
    public void SetZoom(float orthoSize, bool snap = false)
    {
        SetZoom(orthoSize, defaultDuration, defaultCurve, snap);
    }

    // Full control API: per-trigger duration + curve
    public void SetZoom(float orthoSize, float duration, AnimationCurve curve, bool snap = false)
    {
        float clamped = Mathf.Clamp(orthoSize, minOrtho, maxOrtho);

        if (snap || duration <= 0f)
        {
            _isTransitioning = false;
            _cam.orthographicSize = clamped;
            return;
        }

        _startZoom = _cam.orthographicSize;
        _targetZoom = clamped;
        _elapsed = 0f;
        _duration = duration;
        _curve = curve != null ? curve : AnimationCurve.Linear(0, 0, 1, 1);
        _isTransitioning = true;
    }
}
