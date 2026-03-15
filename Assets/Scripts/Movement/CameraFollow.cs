using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // 要跟随的目标（通常是你的玩家）
    public Transform target;

    // 相机与目标之间的偏移量（在后面多远的距离，在上面多高）
    public Vector3 offset = new Vector3(0f, 2f, -5f);

    // 相机移动的平滑时间（值越小，跟随越快、越紧）
    public float smoothTime = 0.3f;

    // 用于缓计算的参考速度（不要手动修改）
    private Vector3 velocity = Vector3.zero;

    void FixedUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: No target assigned!");
            return;
        }

        // 计算相机期望的目标位置
        // 这个位置是目标的位置加上一个偏移量
        Vector3 desiredPosition = target.position + offset;

        // 使用 SmoothDamp 平滑地移动到目标位置
        // SmoothDamp 会创建一个平滑的缓动效果，而不是瞬间移动
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // 让相机始终看着目标
        transform.LookAt(target.position);
    }
}