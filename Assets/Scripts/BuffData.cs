using UnityEngine;

// 这一行让你可以在 Project 窗口右键创建
[CreateAssetMenu(fileName = "NewBuff", menuName = "Game/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("=== ☠️ 伤害逻辑配置 ===")]
    [Tooltip("持续多少秒")]
    public float duration = 5f;

    [Tooltip("每次扣多少血")]
    public int damagePerTick = 10;

    [Tooltip("几秒扣一次")]
    public float tickInterval = 1f;

    [Tooltip("勾选后，接触瞬间会立刻造成一次伤害，不用等第一秒")]
    public bool triggerImmediately = false;

    // 🔥 新增：堆叠开关
    [Header("=== 机制配置 ===")]
    [Tooltip("勾选 = 像流血一样，每次受伤都加一个新条子（叠加）。\n不勾 = 像中毒一样，只保留一个，再次受伤重置时间（刷新）。")]
    public bool isStackable = true; // 默认为 true (流血模式)

    [Header("=== 🎨 UI 表现配置 ===")]
    [Tooltip("屏幕上弹出的文字，如 '剧毒攻心！'")]
    public string uiMessage = "中毒了！！！";

    [Tooltip("进度条的颜色")]
    public Color uiColor = Color.green;
}