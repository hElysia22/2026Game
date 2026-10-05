using System;
using UnityEngine;

/// <summary>
/// 灵装卸界面的入口。后续 UI 订阅 OnOpenChanged 显示/隐藏面板，
/// 通过 Equipment 的获取、装卸接口操作装备，并订阅 OnEquipmentChanged 刷新内容。
/// </summary>
[DisallowMultipleComponent]
public class EquipmentMenuController : MonoBehaviour
{
    public PlayerInputReader input;
    public PlayerEquipment equipment;

    public bool IsOpen { get; private set; }
    public event Action<bool> OnOpenChanged;

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
        IsOpen = open;
        OnOpenChanged?.Invoke(IsOpen);
    }
}
