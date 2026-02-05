using UnityEngine;
using System.Collections;

// 保持继承关系不变
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("属性设置")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false; // 新增：防止尸体重复扣血

    [Header("依赖引用")]
    [SerializeField] private GhostBarAnimator healthBarAnimator;

    // 新增：需要控制玩家能不能动
    // 请在 Inspector 里把挂着 PlayerController 的物体（通常就是自己）拖进去
    [SerializeField] private MonoBehaviour playerController;
    [SerializeField] private Animator animator;

    private void Start()
    {
        // 初始化逻辑不变
        currentHealth = maxHealth;
        if (healthBarAnimator != null)
            healthBarAnimator.Initialize(currentHealth, maxHealth);

        // 尝试自动获取组件，防止忘记拖拽
        if (animator == null) animator = GetComponent<Animator>();
        // 注意：如果你用的脚本叫 "PlayerController"，请把下面这行取消注释
        // if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    public void TakeDamage(float amount)
    {
        // 如果已经死了，就不要再扣血了，防止触发两次死亡逻辑
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        // UI 更新逻辑保持不变
        if (healthBarAnimator != null)
        {
            healthBarAnimator.SetValue(currentHealth, maxHealth);
        }

        Debug.Log($"受到 {amount} 点伤害，剩余血量: {currentHealth}");

        // 触发死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(">>> 角色死亡，进入复活流程 <<<");

        // 1. 禁止移动
        if (playerController != null) playerController.enabled = false;

        // 2. 播放死亡动画 (确保你的 Animator 有 "Die" 这个 Trigger)
        if (animator != null) animator.SetTrigger("Die");

        // 3. 启动复活协程
        StartCoroutine(RespawnRoutine());
    }

    // 新增：复活协程
    IEnumerator RespawnRoutine()
    {
        // 等待 3 秒（给死亡动画和黑屏留时间）
        yield return new WaitForSeconds(3.0f);

        // --- 开始复活 ---

        // 1. 调用回满状态方法（清理状态、回血）
        HealToFull();

        // 2. 传送回存档点 (从 GameFlowManager 获取坐标)
        // 这一步是核心：如果没有 GameFlowManager，就留在原地
        if (GameFlowManager.Instance != null)
        {
            transform.position = GameFlowManager.Instance.currentRespawnPoint;
        }

        // 3. 恢复动画状态 (比如切回 Idle)
        if (animator != null) animator.Play("Locomotion"); // 或者 "Idle"

        // 4. 恢复移动控制
        if (playerController != null) playerController.enabled = true;

        isDead = false;
        Debug.Log(">>> 玩家已在存档点复活 <<<");
    }

    // 实现接口/坐火时调用的方法
    // 建议改为 public，这样 Bonfire 脚本可以直接调用它
    public void HealToFull()
    {
        // 1. 数值回满
        currentHealth = maxHealth;

        // 2. UI 强制刷新（不使用动画，直接满）
        // 或者使用 SetValue 让它慢慢涨回去也可以，看你喜好
        if (healthBarAnimator != null)
        {
            healthBarAnimator.SetValue(currentHealth, maxHealth);
        }

        Debug.Log("状态已完全恢复");
    }
}