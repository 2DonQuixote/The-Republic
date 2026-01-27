using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))] // 强制要求有胶囊体
public class PlayerController : MonoBehaviour
{
    [Header("移动速度配置")]
    [SerializeField] private float crouchSpeed = 2f; // 潜行速度
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("翻滚配置")]
    [SerializeField] private float rollSpeed = 15f;
    [SerializeField] private float rollDuration = 0.5f;
    [SerializeField] private float rollCooldown = 1f;

    [Header("潜行配置 (高级)")]
    [SerializeField] private float crouchHeight = 1.2f; // 蹲下时的高度
    [SerializeField] private float standHeight = 1.8f;  // 站立时的高度
    [SerializeField] private float centerOffsetY = 0.9f; // 站立时的中心点Y
    [SerializeField] private float crouchCenterOffsetY = 0.6f; // 蹲下时的中心点Y

    [Header("状态与引用")]
    [SerializeField] private bool isCombatMode = false;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Animator animator;

    // 内部变量
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider; // 胶囊体引用
    private Camera mainCamera;
    private Vector3 moveInput;
    private Vector3 rollDirection;

    // 状态标记
    private bool isRolling = false;
    private float lastRollTime = -99f;
    private bool isCrouching = false; // 是否正在蹲着

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>(); // 获取胶囊体
        mainCamera = Camera.main;

        // 初始化记录一下站立高度，防止填错
        if (capsuleCollider != null) standHeight = capsuleCollider.height;
    }

    void Update()
    {
        if (isRolling) return;

        // 1. 获取输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0, v).normalized;

        // 2. 检测翻滚 (空格)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // ---【新增】潜行锁 ---
            // 如果正在蹲着，直接忽略翻滚指令，什么都不做
            if (isCrouching) return;

            // 只有没蹲着，才继续检查冷却、执行翻滚
            if (Time.time >= lastRollTime + rollCooldown)
            {
                StartCoroutine(RollRoutine());
                return;
            }
        }

        // 3. 检测潜行 (按住 Ctrl)
        // 逻辑优先级：如果有移动输入 -> 跑(Shift) > 蹲(Ctrl) > 走(默认)
        // 但通常蹲下的优先级最高，按住Ctrl就不能跑
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (!isCrouching) CrouchDown(); // 如果之前没蹲，现在执行蹲下逻辑
        }
        else
        {
            if (isCrouching) StandUp(); // 如果之前蹲了，松开Ctrl就站起来
        }

        // 4. 计算当前速度
        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (moveInput.magnitude > 0 && Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }

        // 5. 执行移动和旋转
        UpdateMovement(moveInput * currentSpeed);

        // 旋转：蹲下时也可以旋转，逻辑不变
        if (isCombatMode) TurnToMouse();
        else TurnToMoveDirection();

        // 6. 更新动画
        UpdateAnimation(moveInput.magnitude > 0);
    }

    void FixedUpdate()
    {
        if (!isRolling)
        {
            rb.velocity = new Vector3(moveInput.x * (isCrouching ? crouchSpeed : (Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed)), rb.velocity.y, moveInput.z * (isCrouching ? crouchSpeed : (Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed)));
            // 修正：上面的逻辑有点乱，直接用Update里算好的速度虽然也行，但最好重新传参。
            // 简化写法：我们这里直接用 UpdateMovement 设置的 velocity 也可以，
            // 但为了物理稳定，建议把rb.velocity的赋值只写在 FixedUpdate 里。
            // 这里为了教学方便，沿用之前的模式，只清理角速度。
            rb.angularVelocity = Vector3.zero;
        }
    }

    // 把rb.velocity的赋值搬到这里更规范
    private void UpdateMovement(Vector3 targetVelocity)
    {
        if (!isRolling)
        {
            rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
        }
    }

    // --- 蹲下与起立的核心逻辑 ---
    void CrouchDown()
    {
        isCrouching = true;
        // 调整物理胶囊体（变矮）
        if (capsuleCollider != null)
        {
            capsuleCollider.height = crouchHeight;
            capsuleCollider.center = new Vector3(0, crouchCenterOffsetY, 0);
        }
    }

    void StandUp()
    {
        isCrouching = false;
        // 恢复物理胶囊体
        if (capsuleCollider != null)
        {
            capsuleCollider.height = standHeight;
            capsuleCollider.center = new Vector3(0, centerOffsetY, 0);
        }
    }

    // --- 动画更新 ---
    private void UpdateAnimation(bool isMoving)
    {
        if (animator == null) return;

        // 传递蹲下状态
        animator.SetBool("IsCrouching", isCrouching);

        // 传递速度 (0=站, 0.5=走, 1=跑)
        // 注意：蹲下时，我们也传个速度值给 BlendTree，通常蹲下走我们希望也是播放"Walk"类型的速度
        float animationValue = 0f;

        if (isMoving)
        {
            if (isCrouching) animationValue = 0.5f; // 蹲走通常对应 BlendTree 里的中间值
            else if (Input.GetKey(KeyCode.LeftShift)) animationValue = 1f; // 跑
            else animationValue = 0.5f; // 走
        }

        animator.SetFloat("Speed", animationValue, 0.1f, Time.deltaTime);
    }

    // --- 翻滚与旋转保持不变 (略去重复代码，确保你保留了之前的 RollRoutine 等) ---
    // (请确保你把之前脚本里的 IEnumerator RollRoutine, TurnToMouse, TurnToMoveDirection 都复制进来了！)

    // 为了方便你复制，我把这几个核心方法再贴一次：

    IEnumerator RollRoutine()
    {
        isRolling = true;
        lastRollTime = Time.time;
        animator.SetTrigger("Roll");

        if (moveInput.magnitude > 0.1f) rollDirection = moveInput;
        else rollDirection = transform.forward;

        transform.rotation = Quaternion.LookRotation(rollDirection);

        float timer = 0f;
        while (timer < rollDuration)
        {
            rb.velocity = new Vector3(rollDirection.x * rollSpeed, rb.velocity.y, rollDirection.z * rollSpeed);
            timer += Time.deltaTime;
            yield return null;
        }
        isRolling = false;
        rb.velocity = Vector3.zero;
    }

    private void TurnToMoveDirection()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void TurnToMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Vector3 targetPoint = hit.point;
            targetPoint.y = transform.position.y;
            transform.LookAt(targetPoint);
        }
    }
}