using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class MinimapCameraFitter : MonoBehaviour
{
    public MinimapBounds bounds;
    public float cameraHeight = 300f;
    public float extraPadding = 10f;

    private Camera cam;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        FitNow();
    }

    private void OnValidate()
    {
        cam = GetComponent<Camera>();
        FitNow();
    }

    [ContextMenu("Fit Camera To Bounds")]
    public void FitNow()
    {
        if (cam == null || bounds == null)
            return;

        cam.orthographic = true;

        Vector3 center = bounds.CenterWorld;
        transform.position = new Vector3(center.x, cameraHeight, center.z);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        float aspect = cam.aspect;
        if (aspect <= 0.01f)
            aspect = 1f;

        float width = bounds.Width + extraPadding * 2f;
        float depth = bounds.Depth + extraPadding * 2f;

        float sizeByDepth = depth * 0.5f;
        float sizeByWidth = width * 0.5f / aspect;

        cam.orthographicSize = Mathf.Max(sizeByDepth, sizeByWidth);

        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = Mathf.Max(cameraHeight + 500f, 1000f);
    }
}