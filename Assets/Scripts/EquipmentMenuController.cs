using System;
using UnityEngine;

/// <summary>管理灵装卸面板的开关、游戏暂停和角色输入；由视图订阅状态变化。</summary>
[DisallowMultipleComponent]
public class EquipmentMenuController : MonoBehaviour
{
    public PlayerInputReader input;
    public PlayerEquipment equipment;
    public bool IsOpen { get; private set; }
    public event Action<bool> OnOpenChanged;
    private float previousTimeScale;
    private bool previousInputBlock;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
    }

    void Update()
    {
        if (input != null && input.ConsumeEquipment()) Toggle();
    }

    void OnDisable() => Close();
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!IsOpen);

    private void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        if (open && GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        IsOpen = open;
        if (open)
        {
            previousTimeScale = HitStop.Instance != null && HitStop.Instance.IsActive
                ? HitStop.Instance.ResumeTimeScale : Time.timeScale;
            previousInputBlock = input != null && input.IsGameplayInputBlocked;
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            if (input != null) input.SetGameplayInputBlocked(true);
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (input != null) input.SetGameplayInputBlocked(previousInputBlock);
            var health = equipment != null ? equipment.GetComponent<Health>() : null;
            bool paused = (health != null && health.IsDead)
                || (GameManager.Instance != null && GameManager.Instance.IsPaused);
            Time.timeScale = paused ? 0 : previousTimeScale;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
        OnOpenChanged?.Invoke(IsOpen);
    }
}
