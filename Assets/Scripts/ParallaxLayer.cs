using UnityEngine;

public sealed class ParallaxLayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax Factors")]
    [Tooltip("How much this layer responds to camera movement. Typical X: 0.05–0.6")]
    [Range(0f, 1f)]
    [SerializeField] private float factorX = 0.2f;

    [Tooltip("Often smaller than X for platformers. Typical Y: 0–0.2")]
    [Range(0f, 1f)]
    [SerializeField] private float factorY = 0.05f;

    [Header("Options")]
    [Tooltip("If true, compensates for ortho zoom changes so parallax feel remains consistent.")]
    [SerializeField] private bool compensateForZoom = true;

    private Vector3 _lastCamPos;
    private float _lastOrthoSize = -1f;
    private Camera _cam;

    void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        _cam = Camera.main;
        if (_cam != null) _lastOrthoSize = _cam.orthographicSize;
    }

    void Start()
    {
        if (cameraTransform != null)
            _lastCamPos = cameraTransform.position;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 camPos = cameraTransform.position;
        Vector3 delta = camPos - _lastCamPos;

        float zoomScale = 1f;
        if (compensateForZoom && _cam != null && _lastOrthoSize > 0f)
        {
            // If you zoom in (smaller ortho), movement feels bigger; scale parallax to keep it stable
            zoomScale = _cam.orthographicSize / _lastOrthoSize;
            _lastOrthoSize = _cam.orthographicSize;
        }

        transform.position += new Vector3(delta.x * factorX * zoomScale, delta.y * factorY * zoomScale, 0f);

        _lastCamPos = camPos;
    }
}