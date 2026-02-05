using System;
using UnityEngine;

public static class GameEventManager
{
    // 定义两种状态：游戏进行中 / 互动休息中
    public enum GameState { Gameplay, Resting }

    // 定义一个事件
    public static event Action<GameState> OnStateChanged;

    // 触发事件的方法
    public static void SetState(GameState newState)
    {
        OnStateChanged?.Invoke(newState);

        // 可选：如果是 Resting，显示鼠标；Gameplay 隐藏鼠标
        if (newState == GameState.Resting)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 根据你的游戏需求调整
            Cursor.visible = false;
        }
    }
}