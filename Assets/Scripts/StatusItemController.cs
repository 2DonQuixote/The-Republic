using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StatusItemController : MonoBehaviour
{
    public Image durationBar;
    public Text statusText;

    // 一个私有变量，用来记录当前的动画，方便打断它
    private Tween _barTween;

    public void Setup(string content, float duration, Color barColor)
    {
        if (statusText != null) statusText.text = content;
        if (durationBar != null)
        {
            durationBar.color = barColor;
            durationBar.fillAmount = 1f;
        }

        // 进场动画
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        // 开始倒计时
        StartCountdown(duration);
    }

    // 🔥 新增：重置倒计时
    public void ResetTimer(float newDuration)
    {
        // 1. 杀掉旧动画 (防止它继续往下跑)
        _barTween?.Kill();

        // 2. 视觉反馈：稍微弹一下，提示玩家时间刷新了
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);

        // 3. 重新开始
        if (durationBar != null) durationBar.fillAmount = 1f;
        StartCountdown(newDuration);
    }

    // 提取出来的倒计时逻辑
    private void StartCountdown(float duration)
    {
        if (durationBar != null)
        {
            _barTween = durationBar.DOFillAmount(0f, duration)
                .SetEase(Ease.Linear)
                .OnComplete(RemoveSelf);
        }
    }

    private void RemoveSelf()
    {
        transform.DOScale(0f, 0.2f).OnComplete(() => Destroy(gameObject));
    }

    // 辅助方法：告诉外面我显示的是什么字（用来判断是否重复）
    public string GetTitle()
    {
        return statusText != null ? statusText.text : "";
    }
}