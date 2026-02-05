using UnityEngine;

public class DamageOnTouch : MonoBehaviour
{
    [Header("伤害配置")]
    [Tooltip("碰到一次扣多少血")]
    public float damageAmount = 20f;

    [Tooltip("是否是一次性伤害？(勾选=撞一下扣一次；不勾=持续扣血)")]
    public bool isOneShot = true;

    // 冷却时间，防止一帧内连续触发多次判定导致秒杀
    private float damageCooldown = 1.0f;
    private float lastDamageTime;

    // 核心碰撞逻辑：无论是 Collider 还是 Trigger 都可以触发
    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDealDamage(collision.gameObject);
    }

    private void TryDealDamage(GameObject target)
    {
        // 检查冷却 (简单的防抖动逻辑)
        if (Time.time < lastDamageTime + damageCooldown) return;

        // === 极其重要的一步 ===
        // 我们不关心撞到的是 Player 还是 NPC
        // 我们只问：你身上有 IDamageable 组件吗？
        IDamageable targetHealth = target.GetComponent<IDamageable>();

        if (targetHealth != null)
        {
            // 有，那就扣血！
            targetHealth.TakeDamage(damageAmount);
            lastDamageTime = Time.time;
        }
    }
}