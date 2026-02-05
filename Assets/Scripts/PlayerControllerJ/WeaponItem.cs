using System.Collections.Generic;
using UnityEngine;

// 这是武器配置文件
[CreateAssetMenu(menuName = "Combat/New Weapon")] // 注意这里是 Weapon
public class WeaponItem : ScriptableObject
{
    [Header("基础信息")]
    public string weaponName; // 武器名字
    public GameObject modelPrefab; // 武器模型

    [Header("动画配置")]
    // 关键！每把武器携带一个“重写控制器”
    public AnimatorOverrideController weaponAnimator;

    // 🔥 [新增] 连招重置时间：动作结束后，多长时间内算连招？
    [Tooltip("动作结束后，几秒内不攻击则重置连招？建议 1.0 - 2.0 秒")]
    public float comboResetTime = 2.0f;

    [Header("连招配置 (关键)")]
    // 👇 只要下面的 AttackAction 定义正确，这里就不会报错
    public List<AttackAction> lightAttacks;
    public List<AttackAction> heavyAttacks;

    // --- 工具方法 ---
    public AttackAction GetLightAttack(int index)
    {
        if (lightAttacks == null || lightAttacks.Count == 0) return null;
        if (index >= lightAttacks.Count) return lightAttacks[0];
        return lightAttacks[index];
    }

    public AttackAction GetHeavyAttack(int index)
    {
        if (heavyAttacks == null || heavyAttacks.Count == 0) return null;
        if (index >= heavyAttacks.Count) return heavyAttacks[0];
        return heavyAttacks[index];
    }
}

// 请把这段代码覆盖掉 WeaponItem.cs 里原本的 AttackAction 类
[System.Serializable]
public class AttackAction
{
    [Header("动画核心")]
    public string animName; // 动画名

    [Tooltip("动作融合时间。值越小越干脆(0.05)，值越大越柔和(0.2)。")]
    [Range(0f, 0.5f)] public float transitionDuration = 0.1f;

    [Header("节奏控制")]
    [Tooltip("动作总时长（秒）")]
    public float totalDuration = 1.0f;

    [Tooltip("连招窗口起点(0-1)。设为 0.6 表示动作播放 60% 后按键可触发下一击。")]
    [Range(0f, 1f)] public float comboWindowStart = 0.6f;

    // 🔥🔥🔥 你报错就是因为缺了下面这一行！补上它！🔥🔥🔥
    [Tooltip("【翻滚打断点】动作播放到 % 多少之后，才允许翻滚强制打断？(0.5 = 动作做了一半才能滚)")]
    [Range(0f, 1f)] public float rollCancelStartTime = 0.5f;

    [Header("位移与物理")]
    [Tooltip("攻击时的瞬间冲力")]
    public float impulseForce = 0f;

    [Tooltip("冲力施加的延迟时间")]
    public float impulseDelay = 0.05f;

    [Tooltip("动作期间允许移动的速度比例 (0=定住, 1=正常走)")]
    [Range(0f, 1f)] public float movementSpeedMultiplier = 0f;

    [Header("数值")]
    public float damageMultiplier = 1.0f; // 伤害倍率
}