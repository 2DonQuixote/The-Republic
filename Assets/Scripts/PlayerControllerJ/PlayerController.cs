using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("=== 核心配置 ===")]
    // 🔥 这里拖入你的 WeaponItem (例如 Sword_Beginner)
    public WeaponItem currentWeapon;

    // 🔥🔥 [新增] 右手骨骼挂载点 (记得在Inspector里把 Hand_R 拖进去！)
    [Header("=== 模型挂载点 ===")]
    public Transform rightHandTransform;

    [Header("=== 移动参数 ===")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float rotateSpeed = 15f;
    public bool useRootMotion = true;

    [Header("=== 通用手感配置 ===")]
    [Tooltip("输入缓存时间(秒)")]
    public float inputBufferTime = 0.8f;

    [Header("=== 翻滚配置 ===")]
    public float rollForce = 15f;
    public float rollDuration = 0.6f;
    public float rollDrag = 3f;
    public float rollCooldown = 1.0f;
    [Tooltip("翻滚转攻击允许的时间点 (0.7 = 翻滚动作播放70%后可出刀)")]
    [Range(0f, 1f)] public float rollAttackWindow = 0.7f;

    // --- 内部状态 ---
    private GameControls controls;
    private Vector2 moveInput;
    private Rigidbody rb;
    private Animator animator;

    // 🔥🔥 [新增] 记录当前生成的模型，方便切换时销毁旧的
    private GameObject currentWeaponModel;

    // 状态标记
    private bool isCrouching;
    private bool isRunning;
    private bool isRolling;
    private bool isAttacking;
    private bool canMove = true;

    // 战斗状态
    private int comboCount = 0; // 当前连击段数 (0=第一刀, 1=第二刀...)
    private bool currentAttackIsHeavy = false;

    // 计时器
    private float lastInputTime = -100f;
    private float lastAttackStartTime;
    private float rollStartTime;
    private float lastRollTime = -100f;
    private float defaultDrag;
    private bool bufferedInputIsHeavy = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        defaultDrag = rb.drag;
        controls = new GameControls();

        SetupInput();
    }

    private void Start()
    {
        // 🔥 初始化武器 (关键！)
        EquipWeapon(currentWeapon);
    }

    // 🔥🔥 [修改] 装备武器：现在不仅换动画，还会换模型
    public void EquipWeapon(WeaponItem newWeapon)
    {
        currentWeapon = newWeapon;

        // 1. 切换动画控制器
        if (currentWeapon != null && currentWeapon.weaponAnimator != null)
        {
            animator.runtimeAnimatorController = currentWeapon.weaponAnimator;
        }

        // 2. 生成武器模型
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }

        if (currentWeapon != null && currentWeapon.modelPrefab != null)
        {
            if (rightHandTransform != null)
            {
                currentWeaponModel = Instantiate(currentWeapon.modelPrefab, rightHandTransform);
                currentWeaponModel.transform.localPosition = Vector3.zero;
                currentWeaponModel.transform.localRotation = Quaternion.identity;
                // 注意：这里用了 Scale 1，请确保使用了“父子隔离法”做的预制体
                currentWeaponModel.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogError("请在 Inspector 面板的 PlayerController 组件里，把 'Right Hand Transform' (Hand_R) 拖进去！");
            }
        }

        // 🔥 切换武器时，强制重置连招
        ResetCombo();
        CancelInvoke(nameof(ResetCombo));
    }

    private void SetupInput()
    {
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Gameplay.Run.performed += ctx => isRunning = true;
        controls.Gameplay.Run.canceled += ctx => isRunning = false;
        controls.Gameplay.Crouch.performed += ctx => ToggleCrouch(!isCrouching);

        controls.Gameplay.Roll.performed += ctx => HandleRollInput();
        controls.Gameplay.Attack.performed += ctx => BufferAttackInput(false);
        controls.Gameplay.HeavyAttack.performed += ctx => BufferAttackInput(true);
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    private void Update()
    {
        if (!canMove) return;
        CheckBufferedInput();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;
        if (isRolling || isAttacking) return;

        HandleMovement();
        HandleRotation();
    }

    // ================= 核心战斗逻辑 (修改部分) =================

    private void BufferAttackInput(bool isHeavy)
    {
        if (!canMove) return;
        if (!isRolling || (Time.time - rollStartTime >= rollDuration * rollAttackWindow))
        {
            lastInputTime = Time.time;
            bufferedInputIsHeavy = isHeavy;
        }
    }

    private void CheckBufferedInput()
    {
        bool hasBufferedInput = (Time.time - lastInputTime) < inputBufferTime;
        if (!hasBufferedInput) return;

        if (isRolling)
        {
            if ((Time.time - rollStartTime) >= rollDuration * rollAttackWindow)
            {
                ExecuteAttack(bufferedInputIsHeavy);
                lastInputTime = -100f;
            }
        }
        else if (CanAttackNow())
        {
            ExecuteAttack(bufferedInputIsHeavy);
            lastInputTime = -100f;
        }
    }

    private bool CanAttackNow()
    {
        if (isRolling) return false;
        if (!isAttacking) return true;

        if (currentWeapon == null) return false;

        AttackAction currentAction = GetCurrentActionData();
        if (currentAction == null) return true;

        float timePlayed = Time.time - lastAttackStartTime;
        return timePlayed >= (currentAction.totalDuration * currentAction.comboWindowStart);
    }

    // 🔥 [核心修改] 执行攻击逻辑
    private void ExecuteAttack(bool isHeavy)
    {
        if (currentWeapon == null) return;

        // 1. 只要攻击了，就取消“重置倒计时”，因为玩家接上了
        CancelInvoke(nameof(ResetCombo));

        PrepareAttackState();
        currentAttackIsHeavy = isHeavy;

        AttackAction action = null;

        // 2. 获取动作数据
        if (isHeavy)
        {
            comboCount = 0; // 重击通常不参与轻击连招，直接算0
            action = currentWeapon.GetHeavyAttack(0);
        }
        else
        {
            // 如果连招段数超过了配置数量，归零
            if (comboCount >= currentWeapon.lightAttacks.Count) comboCount = 0;

            // 获取当前这一段的动作
            action = currentWeapon.GetLightAttack(comboCount);
        }

        if (action == null)
        {
            Debug.LogWarning("未找到攻击动作配置！");
            OnAttackEnd();
            return;
        }

        // 3. 播放动画
        animator.CrossFade(action.animName, action.transitionDuration);

        // 4. 设置硬直结束时间
        Invoke(nameof(OnAttackEnd), action.totalDuration);

        // 5. 🔥 [关键] 为“下一刀”做准备：计数+1
        if (!isHeavy)
        {
            comboCount++;
        }
    }

    private AttackAction GetCurrentActionData()
    {
        if (currentWeapon == null) return null;
        if (currentAttackIsHeavy) return currentWeapon.GetHeavyAttack(0);

        // 注意：因为我们在 ExecuteAttack 结尾才 comboCount++，
        // 所以查询当前正在播放的动作时，应该是 comboCount - 1 (需防越界)
        int index = Mathf.Clamp(comboCount - 1, 0, currentWeapon.lightAttacks.Count - 1);
        // 如果是刚刚重置完还没打第一下（极少情况），就返0
        if (comboCount == 0) index = 0;

        return currentWeapon.GetLightAttack(index);
    }

    private void PrepareAttackState()
    {
        if (isRolling)
        {
            isRolling = false;
            rb.drag = defaultDrag;
            CancelInvoke(nameof(OnRollEnd));
        }

        FaceMouseInstant();

        isAttacking = true;
        rb.velocity = Vector3.zero;
        lastAttackStartTime = Time.time;

        CancelInvoke(nameof(OnAttackEnd));
    }

    private void HandleRollInput()
    {
        if (!canMove) return;
        if (Time.time < lastRollTime + rollCooldown) return;
        if (isRolling) return;

        if (isAttacking)
        {
            AttackAction currentAction = GetCurrentActionData();
            if (currentAction != null)
            {
                float timePlayed = Time.time - lastAttackStartTime;
                if (timePlayed < currentAction.totalDuration * currentAction.rollCancelStartTime)
                {
                    return;
                }
            }
        }

        PerformRoll();
    }

    private void PerformRoll()
    {
        isAttacking = false;

        // 翻滚时，立即重置连招（或者你可以选择不重置，看需求）
        ResetCombo();

        CancelInvoke(nameof(OnAttackEnd));

        isRolling = true;
        lastRollTime = Time.time;
        rollStartTime = Time.time;

        animator.CrossFade("Roll", 0.1f);
        rb.drag = rollDrag;

        Vector3 rollDir = transform.forward;
        if (moveInput.magnitude > 0.1f)
            rollDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        transform.rotation = Quaternion.LookRotation(rollDir);
        rb.AddForce(rollDir * rollForce, ForceMode.Impulse);

        Invoke(nameof(OnRollEnd), rollDuration);
    }

    // ================= 状态回调 =================

    // 🔥 [核心修改] 攻击动作结束
    public void OnAttackEnd()
    {
        isAttacking = false;

        // 动作做完了，开启“倒计时”
        // 如果 2 秒（配置的时间）内没有再次攻击，ResetCombo 就会被调用，连招归零
        Invoke(nameof(ResetCombo), currentWeapon.comboResetTime);
    }

    // 🔥 [新增] 专门用来重置连招的方法
    private void ResetCombo()
    {
        comboCount = 0;
        // Debug.Log("连招已超时重置");
    }

    public void OnRollEnd()
    {
        isRolling = false;
        rb.drag = defaultDrag;
    }

    // ================= 基础移动逻辑 =================

    private void HandleMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            float currentSpeed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
            Vector3 targetDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            rb.MovePosition(rb.position + targetDir * currentSpeed * Time.fixedDeltaTime);

            float animSpeed = isRunning ? 1f : 0.5f;
            if (isCrouching) animSpeed = 0.5f;
            animator.SetFloat("Speed", moveInput.magnitude * animSpeed, 0.1f, Time.fixedDeltaTime);
        }
        else
        {
            animator.SetFloat("Speed", 0, 0.1f, Time.fixedDeltaTime);
        }
    }

    private void HandleRotation()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 targetDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            rb.rotation = Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(targetDir), rotateSpeed * Time.fixedDeltaTime);
        }
    }

    private void FaceMouseInstant()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (new Plane(Vector3.up, new Vector3(0, transform.position.y, 0)).Raycast(ray, out float enter))
        {
            Vector3 lookDir = (ray.GetPoint(enter) - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) rb.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    private void ToggleCrouch(bool state)
    {
        isCrouching = state;
        animator.SetBool("IsCrouch", isCrouching);
    }

    private void OnAnimatorMove()
    {
        if ((isAttacking || isRolling) && useRootMotion && canMove)
        {
            Vector3 newPos = rb.position + animator.deltaPosition;
            newPos.y = rb.position.y;
            rb.MovePosition(newPos);
        }
    }
}