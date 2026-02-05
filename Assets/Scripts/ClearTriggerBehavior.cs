using UnityEngine;

public class ClearTriggerBehavior : StateMachineBehaviour
{
    [Tooltip("进入该状态时，要清除哪个 Trigger？")]
    public string triggerName = "Attack"; // 确保这里填的名字和你 Parameter 里的一样

    // OnStateEnter 在进入状态的第一帧调用
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 🔥 核心逻辑：一进门，就销毁所有的“开门指令”
        animator.ResetTrigger(triggerName);
    }
}