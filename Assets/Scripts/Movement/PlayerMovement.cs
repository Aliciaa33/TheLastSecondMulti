using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // 速度变量
    // 移动速度
    public float moveSpeed = 5f;
    // 跑步时的移动速度
    public float runSpeed = 8f;
    // 旋转速度，使转向更平滑
    public float rotationSpeed = 500f;

    // 跳跃变量
    // 跳跃力（如果需要跳跃功能）
    private float jumpForce = 7f;
    // 检测角色是否站在地面上
    private bool isGrounded;
    // 用于检测地面的射线起点（在角色脚下）
    public Transform groundCheck;
    // 检测地面的半径
    public float groundDistance = 0.2f;
    // 指定什么层算是“地面”
    public LayerMask groundMask;

    // 引用刚体组件，用于物理移动
    private Rigidbody rb;
    // 存储玩家输入
    private Vector3 movement;
    private int jumpCount = 0;

    // 在游戏开始时调用
    void Start()
    {
        // 获取附加在同一个GameObject上的Rigidbody组件
        rb = GetComponent<Rigidbody>();

        // 强制设置到地面高度
        //Vector3 fixedPosition = transform.position;
        //fixedPosition.y = 0.9f;
        //transform.position = fixedPosition;

        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on " + gameObject.name);
        }
    }

    // 每一帧调用一次，用于获取输入
    void Update()
    {
        // --- 地面检测（用于跳跃）---
        // 记录上一帧的地面状态
        bool wasGrounded = isGrounded;

        RaycastHit hit;
        // 利用射线检测更加精准地判断角色是否在地面上
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, out hit, groundDistance, groundMask);

        /*
        // === 添加调试代码在这里 ===
        // 在Scene视图中显示射线
        Debug.DrawRay(groundCheck.position, Vector3.down * groundDistance, isGrounded ? Color.green : Color.red);

        // 在Console中显示地面检测状态
        Debug.Log("IsGrounded: " + isGrounded + " | GroundCheck Position: " + groundCheck.position + " | Player Position: " + transform.position.y);
        // === 调试代码结束 ===
        */

        if (isGrounded && !wasGrounded)
            jumpCount = 0; // 重置跳跃次数


        // 获取输入
        float moveHorizontal = Input.GetAxisRaw("Horizontal"); // A/D 或 左右箭头
        float moveVertical = Input.GetAxisRaw("Vertical");     // W/S 或 上下箭头

        // 创建一个基于世界方向的移动向量
        movement = new Vector3(moveHorizontal, 0.0f, moveVertical).normalized;

        // --- 跳跃输入检测 ---
        // 当按下空格键，并且角色在地面上时，才允许跳跃
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded || (!isGrounded && jumpCount == 1))
                performJump();
        }
    }

    void performJump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        // 给刚体一个瞬间的、向上的力
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpCount++;
    }

    // 固定时间步长调用，用于物理计算
    void FixedUpdate()
    {
        // 调用移动函数
        MoveCharacter(movement);

        // 如果玩家有输入，则旋转角色面向移动方向
        if (movement != Vector3.zero)
        {
            RotateCharacter(movement);
        }
    }

    // 移动角色
    void MoveCharacter(Vector3 direction)
    {
        float currSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
        // 使用刚体的MovePosition进行物理移动，避免穿透
        // 计算目标位置：当前位置 + 方向 * 速度 * 固定时间
        Vector3 targetPosition = rb.position + direction * currSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }

    // 旋转角色面向移动方向
    void RotateCharacter(Vector3 direction)
    {
        // 计算目标旋转，使角色面向移动方向
        // Quaternion.LookRotation 创建一个旋转，使其前向轴（Z轴）指向给定的方向
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        // 使用球形插值平滑地旋转到目标方向
        rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }
}