using UnityEngine;
using System.Collections.Generic;

public class StatusManager : MonoBehaviour
{
    [System.Serializable]
    public class ActiveBuff
    {
        public BuffData data;
        public float timer;
        public float tickTimer;

        public ActiveBuff(BuffData data)
        {
            this.data = data;
            this.timer = data.duration;
            // 处理“立即触发”
            this.tickTimer = data.triggerImmediately ? data.tickInterval : 0f;
        }
    }

    public List<ActiveBuff> currentBuffs = new List<ActiveBuff>();
    private PlayerHealth playerHealth;

    private void Awake() => playerHealth = GetComponent<PlayerHealth>();

    private void Update()
    {
        // ... (Update 里的代码完全不用动，保持原样即可) ...
        // 为了节省篇幅这里省略，请保留你原来的 Update 逻辑
        for (int i = currentBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = currentBuffs[i];
            buff.timer -= Time.deltaTime;
            if (buff.timer <= 0) { currentBuffs.RemoveAt(i); continue; }
            if (buff.data.damagePerTick > 0)
            {
                buff.tickTimer += Time.deltaTime;
                if (buff.tickTimer >= buff.data.tickInterval)
                {
                    buff.tickTimer = 0f;
                    if (playerHealth != null) playerHealth.TakeDamage(buff.data.damagePerTick);
                }
            }
        }
    }

    // === 🔥 核心修改：ApplyBuff ===
    public void ApplyBuff(BuffData newData)
    {
        // 1. 检查是否需要“刷新”旧状态 (中毒模式)
        if (!newData.isStackable)
        {
            // 找找看有没有同名的 Buff 已经在身上了
            // 这里用 Find 查找同名配方
            // 只要配方文件是同一个 (x.data == newData)，就认为是同一种状态
            var existingBuff = currentBuffs.Find(x => x.data == newData);
            if (existingBuff != null)


            {
                // 找到了！只重置时间，不加新的
                existingBuff.timer = newData.duration;
                // 重置扣血节奏 (可选，看你想不想重置那一跳)
                if (newData.triggerImmediately) existingBuff.tickTimer = newData.tickInterval;

                Debug.Log($"刷新了状态：{newData.uiMessage}");
            }
            else
            {
                // 没找到，说明是第一次中这个毒，加进去
                currentBuffs.Add(new ActiveBuff(newData));
            }
        }
        else
        {
            // 2. 可叠加 (流血模式)：直接加个新的，不管有没有旧的
            currentBuffs.Add(new ActiveBuff(newData));
        }

        // 3. 通知 UI (UI 自己会判断是生成新的还是刷新旧的)
        if (GameStatusUI.Instance != null)
        {
            // 注意：一定要把 newData.isStackable 传过去
            GameStatusUI.Instance.ShowStatus(newData.uiMessage, newData.duration, newData.uiColor, newData.isStackable);
        }
    }

    public void ClearAllDebuffs()
    {
        currentBuffs.Clear();
        // 这里假设 HideUI 是清空所有，保持原样即可
        if (GameStatusUI.Instance != null) GameStatusUI.Instance.HideUI();
    }
}