using UnityEngine;

// 专门负责：出生 -> 记录存档点 -> 死亡 -> 复活 的流程
public class GameFlowManager : MonoBehaviour
{
    // 单例模式，方便全局调用
    public static GameFlowManager Instance;

    // 核心数据：当前的重生坐标
    // [HideInInspector] 防止你在Inspector乱改，只能通过代码改
    [HideInInspector]
    public Vector3 currentRespawnPoint;

    private void Awake()
    {
        // 保证单例唯一性
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 切换场景时不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 游戏开始时，自动把玩家现在的脚下位置记为“第一存档点”
        // 这样防止玩家还没遇到篝火就死了，导致没地方复活
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentRespawnPoint = player.transform.position;
            Debug.Log($"[GameFlowManager] 初始出生点已记录: {currentRespawnPoint}");
        }
        else
        {
            Debug.LogError("[GameFlowManager] 致命错误：场景里没找到 Tag 为 'Player' 的物体！");
        }
    }

    // 提供给篝火调用的方法：更新重生点
    public void SetRespawnPoint(Vector3 newPosition)
    {
        currentRespawnPoint = newPosition;
        Debug.Log($"[GameFlowManager] 存档点已更新! 新复活坐标: {newPosition}");
    }
}