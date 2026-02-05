using UnityEngine;

public class GameStatusUI : MonoBehaviour
{
    public static GameStatusUI Instance;
    public GameObject statusItemPrefab;
    public Transform container;

    private void Awake() => Instance = this;

    // 🔥 修改：增加了 isStackable 参数
    public void ShowStatus(string content, float duration, Color barColor, bool isStackable)
    {
        // === 情况 A: 不可叠加 (中毒模式) ===
        if (!isStackable)
        {
            // 遍历当前所有的条子，看看有没有名字一样的
            foreach (Transform child in container)
            {
                var controller = child.GetComponent<StatusItemController>();
                if (controller != null && controller.GetTitle() == content)
                {
                    // 找到了！重置它的时间，然后直接返回，不生成新的
                    controller.ResetTimer(duration);
                    return;
                }
            }
        }

        // === 情况 B: 可叠加 (流血模式) 或 没找到旧的 ===
        // 正常生成新条子
        GameObject newItem = Instantiate(statusItemPrefab, container);
        var ctrl = newItem.GetComponent<StatusItemController>();
        if (ctrl != null)
        {
            ctrl.Setup(content, duration, barColor);
        }
    }

    // 强制清空所有 UI (用于篝火净化)
    public void HideUI()
    {
        // 遍历容器下的所有子物体，把它们全删了
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
} // <--- class 结束的大括号