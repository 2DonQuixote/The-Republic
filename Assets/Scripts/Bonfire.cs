using UnityEngine;

public class Bonfire : MonoBehaviour
{
    [Header("设置")]
    public Transform spawnPoint;

    // 私有变量：用来记住当前走进来的玩家是谁
    private PlayerHealth targetPlayer;
    private bool isPlayerInRange = false;

    void Start()
    {
        if (spawnPoint == null) spawnPoint = this.transform;
    }

    void Update()
    {
        // 判定条件：玩家在圈内 + 按下E + 必须先获取到了玩家脚本
        if (isPlayerInRange && targetPlayer != null && Input.GetKeyDown(KeyCode.E))
        {
            RestAtBonfire();
        }
    }

    void RestAtBonfire()
    {
        Debug.Log(">>> 正在坐火休息... <<<");

        // 【新增】打印出它到底在用哪个物体当坐标
        if (spawnPoint != null)
        {
            Debug.Log($"【调试信息】使用的重生点物体名: {spawnPoint.name}, 坐标: {spawnPoint.position}");
        }
        else
        {
            Debug.LogError("【调试警告】SpawnPoint 是空的！正在使用当前物体中心！");
        }

        // 1. 更新存档点
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetRespawnPoint(spawnPoint.position);
        }

        // 2. 恢复状态
        if (targetPlayer != null)
        {
            targetPlayer.HealToFull();
        }
    }

    // --- 范围检测逻辑 ---

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            // 【关键修改】：进圈时，顺便把玩家身上的血量脚本抓取下来存着
            targetPlayer = other.GetComponent<PlayerHealth>();

            Debug.Log("进入篝火，已捕获玩家实例");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;

            // 离开圈后，清空引用，防止并在远处误触
            targetPlayer = null;

            Debug.Log("离开篝火");
        }
    }
}