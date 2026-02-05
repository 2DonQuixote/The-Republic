using UnityEngine;

public class ZoneBuffTrigger : MonoBehaviour
{
    [Header("把配置好的 Buff 文件拖到这里")]
    public BuffData buffToApply;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var statusMgr = other.GetComponent<StatusManager>();
            if (statusMgr != null && buffToApply != null)
            {
                // 把配方塞给玩家，剩下的事玩家自己处理
                statusMgr.ApplyBuff(buffToApply);
            }
        }
    }
}