using UnityEngine;

[ExecuteAlways]
public class RiverReflectionFollow : MonoBehaviour
{
    [Header("Assign these")]
    public Camera mainCam;

    [Header("Waterline (world-space)")]
    public Transform waterSurface; // empty transform placed on the river surface line
    public float yOffset = 0f;

    Camera _cam;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!mainCam || !waterSurface || !_cam) return;

        // Match main camera lens settings
        _cam.orthographic = mainCam.orthographic;
        _cam.orthographicSize = mainCam.orthographicSize;
        _cam.nearClipPlane = mainCam.nearClipPlane;
        _cam.farClipPlane  = mainCam.farClipPlane;

        // Mirror position around the waterline
        float waterY = waterSurface.position.y + yOffset;

        Vector3 p = mainCam.transform.position;
        p.y = waterY - (p.y - waterY);
        transform.position = p;

        // Same rotation for 2D sidescroller
        transform.rotation = mainCam.transform.rotation;
    }
}