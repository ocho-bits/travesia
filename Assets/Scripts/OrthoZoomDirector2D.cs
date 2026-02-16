using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class OrthoZoomDirector2D : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private float minOrtho = 4.5f;
    [SerializeField] private float maxOrtho = 9.0f;
    [SerializeField] private float defaultZoom = 7.0f;
    [SerializeField] private float smoothTime = 0.25f;

    private Camera _cam;
    private float _desired;
    private float _vel;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;

        _desired = Mathf.Clamp(defaultZoom, minOrtho, maxOrtho);
        _cam.orthographicSize = _desired; // snap at start
    }

    void LateUpdate()
    {
        _cam.orthographicSize =
            Mathf.SmoothDamp(_cam.orthographicSize, _desired, ref _vel, smoothTime);
    }

    public void SetZoom(float orthoSize, bool snap = false)
    {
        _desired = Mathf.Clamp(orthoSize, minOrtho, maxOrtho);
        if (snap) _cam.orthographicSize = _desired;
    }
}