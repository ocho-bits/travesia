using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class OrthoZoomTrigger2D : MonoBehaviour
{
    [SerializeField] private OrthoZoomDirector2D director;

    [Header("Target Zoom")]
    [SerializeField] private float targetZoom = 6.0f;

    [Header("Transition")]
    [Tooltip("Seconds to blend from current zoom to target zoom.")]
    [Min(0f)]
    [SerializeField] private float duration = 0.35f;

    [Tooltip("Curve mapping 0..1 time to 0..1 blend.")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("If true, zoom changes immediately (ignores duration/curve).")]
    [SerializeField] private bool snap = false;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (director == null && Camera.main != null)
            director = Camera.main.GetComponent<OrthoZoomDirector2D>();

        director?.SetZoom(targetZoom, duration, curve, snap);
    }
}