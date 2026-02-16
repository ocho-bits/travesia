using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class OrthoZoomTrigger : MonoBehaviour
{
    [SerializeField] private OrthoZoomDirector2D director;
    [SerializeField] private float targetZoom = 6.0f;
    [SerializeField] private bool snap = false;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (director == null)
            director = Camera.main != null ? Camera.main.GetComponent<OrthoZoomDirector2D>() : null;
        Debug.Log($"ZoomTrigger enter by: {other.name} tag={other.tag}");

        director?.SetZoom(targetZoom, snap);
    }
    
}