public interface IInteractable
{
    void Interact(); // 定义一个“互动”动作
    string GetPrompt(); // 获取提示文本，如 "按 E 休息"
}