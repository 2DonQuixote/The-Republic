using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // 必须引用

public class GhostBarAnimator : MonoBehaviour
{
    [Header("UI 引用")]
    public Image frontFillImage; // 红条
    public Image backGhostImage; // 黄条

    [Header("★ 关键：把你要震动的物体拖到这里！")]
    [Tooltip("对应参考代码里的 duBuRect")]
    public RectTransform containerRect;

    [Header("动画时间")]
    public float frontDuration = 0.5f;
    public float ghostDelay = 0.3f;
    public float ghostDuration = 1.0f;

    [Header("受击反馈 (完美复刻参考代码)")]
    [Tooltip("缩放大小：参考值 0.1 (即放大10%)")]
    public float punchScale = 0.1f;

    [Tooltip("持续时间：参考值 0.12")]
    public float punchDuration = 0.12f;

    [Tooltip("震动频次：参考值 8")]
    public int punchVibrato = 8;

    [Tooltip("弹性值：0到1之间。参考值 0.6")]
    [Range(0, 1)] // 限制范围，防止你填100报错
    public float punchElasticity = 0.6f;

    // 内部变量
    private Tween _frontTween;
    private Tween _ghostTween;
    private Tween _punchTween;
    private float _currentFillAmount = 1f;
    private RectTransform _selfRect;

    private void Awake()
    {
        // 如果你没拖 containerRect，我自动抓一下当前的，防止报错
        if (containerRect == null) containerRect = GetComponent<RectTransform>();
    }

    public void Initialize(float current, float max)
    {
        _currentFillAmount = (max <= 0) ? 0 : current / max;
        RefreshUIImmediate();
    }

    public void RefreshUIImmediate()
    {
        if (frontFillImage != null) frontFillImage.fillAmount = _currentFillAmount;
        if (backGhostImage != null) backGhostImage.fillAmount = _currentFillAmount;
    }

    // 核心入口
    public void SetValue(float current, float max)
    {
        float targetFill = (max <= 0) ? 0 : Mathf.Clamp01(current / max);

        // 判断是否受伤 (新血量 < 旧血量)
        // 只有受伤才震动，回血不震
        bool isDamage = targetFill < (_currentFillAmount - 0.001f);

        // 1. 清理旧动画
        _frontTween?.Kill();
        _ghostTween?.Kill();

        // 2. 红条动画
        if (frontFillImage != null)
        {
            _frontTween = frontFillImage.DOFillAmount(targetFill, frontDuration)
                .SetEase(Ease.OutCubic);
        }

        // 3. 黄条动画
        if (backGhostImage != null)
        {
            _ghostTween = backGhostImage.DOFillAmount(targetFill, ghostDuration)
                .SetDelay(ghostDelay)
                .SetEase(Ease.OutCubic);
        }

        // ==============================================
        // ★ 核心抄作业部分：完全一致的 Punch Scale 逻辑
        // ==============================================
        if (isDamage && containerRect != null)
        {
            // 1. 杀掉上一次震动
            _punchTween?.Kill();

            // 2.【关键】强制归位！
            // 防止连续挨打时血条越来越大，回不去原样
            containerRect.localScale = Vector3.one;

            // 3. 执行 Q 弹缩放
            // Vector3.one * punchScale 意思就是 X/Y/Z 轴都放大
            _punchTween = containerRect.DOPunchScale(Vector3.one * punchScale, punchDuration, punchVibrato, punchElasticity);
        }

        _currentFillAmount = targetFill;
    }
}