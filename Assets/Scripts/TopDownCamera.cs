using UnityEngine;

public class ProCameraFollow : MonoBehaviour
{
    [Header("1. 核心目标")]
    public Transform target; // 拖入你的 Player

    [Header("2. 位置调整 (所见即所得)")]
    [Range(2f, 20f)] public float height = 10f;    // 高度 (Y轴)
    [Range(2f, 20f)] public float distance = 8f;   // 水平距离 (Z轴)
    [Range(-5f, 5f)] public float horizontalOffset = 0f; // 左右偏移 (X轴，通常为0)

    [Header("3. 角度控制")]
    [Tooltip("勾选后，相机会强制死死盯着玩家，下面的角度设置将失效")]
    public bool lookAtTarget = false; // 是否强制注视目标

    [Range(30f, 90f)] public float fixedAngle = 55f; // 如果不注视目标，就用这个固定角度

    [Header("4. 手感平滑度")]
    public float positionSmoothTime = 0.2f; // 移动平滑时间 (越小越快)

    // 内部变量，用于 SmoothDamp 算法
    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        // --- 第一步：计算目标位置 ---
        // 我们根据 Inspector 里的 "高度" 和 "距离" 来算出相机应该在哪
        // 这里的逻辑是：玩家位置 + 往后退 distance 米 + 往上飞 height 米
        Vector3 finalPosition = target.position;
        finalPosition += Vector3.back * distance; // 往后退 (Z轴)
        finalPosition += Vector3.up * height;     // 往上飞 (Y轴)
        finalPosition += Vector3.right * horizontalOffset; // 左右偏移

        // --- 第二步：平滑移动 ---
        // 这一步保证相机不会瞬间闪现，而是像橡皮筋一样跟过去
        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalPosition,
            ref currentVelocity,
            positionSmoothTime
        );

        // --- 第三步：处理旋转角度 ---
        if (lookAtTarget)
        {
            // 方案A：死死盯着玩家 (适合想要一直把玩家放在画面正中心的)
            // 技巧：我们看向玩家的“脚底往上一点点”，否则看起来像盯着脚看
            Vector3 lookTarget = target.position + Vector3.up * 1.5f;
            transform.LookAt(lookTarget);
        }
        else
        {
            // 方案B：固定角度 (适合像《哈迪斯》或《暗黑》那种稳重的视角)
            // 无论玩家怎么动，相机的俯视角度永远不变，画面更稳
            transform.rotation = Quaternion.Euler(fixedAngle, 0, 0);
        }
    }
}