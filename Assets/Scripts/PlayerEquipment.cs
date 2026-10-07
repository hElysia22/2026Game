using System;
using UnityEngine;

public enum EquipmentSlot { Weapon, Scarf, Clothes }

/// <summary>三件基础装备永久拥有；最多两个灵，每个槽最多消耗一个，可自由装卸。</summary>
[DisallowMultipleComponent]
public class PlayerEquipment : MonoBehaviour
{
    public const int MaxSpirits = 2;
    public EquipmentData data;
    [SerializeField, Range(0, MaxSpirits), Tooltip("进入游戏时拥有的灵数量；运行中用组件右键菜单测试获取和装卸。")]
    private int startingSpirits = MaxSpirits;

    private int totalSpirits;
    private bool weaponEnhanced;
    private bool scarfEnhanced;
    private bool clothesEnhanced;

    public int TotalSpirits => totalSpirits;
    public int UsedSpirits => (weaponEnhanced ? 1 : 0) + (scarfEnhanced ? 1 : 0) + (clothesEnhanced ? 1 : 0);
    public int AvailableSpirits => TotalSpirits - UsedSpirits;
    public bool HasAirJump => IsEnhanced(EquipmentSlot.Scarf);
    public int DamageBonus => IsEnhanced(EquipmentSlot.Weapon) ? Mathf.Max(0, data != null ? data.swordDamageBonus : 2) : 0;
    public float ReachBonus => IsEnhanced(EquipmentSlot.Weapon) ? Mathf.Max(0, data != null ? data.swordReachBonus : 0.5f) : 0f;
    public float WidthBonus => IsEnhanced(EquipmentSlot.Weapon) ? Mathf.Max(0, data != null ? data.swordWidthBonus : 0.5f) : 0f;
    public float AirJumpForceMultiplier => data != null ? Mathf.Max(0.01f, data.airJumpForceMultiplier) : 1f;
    public int DamageReduction => IsEnhanced(EquipmentSlot.Clothes) ? Mathf.Max(0, data != null ? data.armorDamageReduction : 1) : 0;
    public event Action OnEquipmentChanged;

    void Awake()
    {
        totalSpirits = Mathf.Clamp(startingSpirits, 0, MaxSpirits);
        // HUD 可能先订阅再执行装备 Awake，初始化完成后也需要刷新。
        OnEquipmentChanged?.Invoke();
    }

    /// <returns>实际增加的数量；达到两个灵上限后返回 0。</returns>
    public int CollectSpirits(int amount = 1)
    {
        int added = Mathf.Min(Mathf.Max(0, amount), MaxSpirits - totalSpirits);
        if (added == 0) return 0;
        totalSpirits += added;
        OnEquipmentChanged?.Invoke();
        return added;
    }

    public bool IsEnhanced(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return weaponEnhanced;
            case EquipmentSlot.Scarf: return scarfEnhanced;
            case EquipmentSlot.Clothes: return clothesEnhanced;
            default: return false;
        }
    }

    public bool TryEquipSpirit(EquipmentSlot slot)
    {
        if (!IsValid(slot) || IsEnhanced(slot) || AvailableSpirits <= 0) return false;
        SetEnhanced(slot, true);
        return true;
    }

    public bool TryUnequipSpirit(EquipmentSlot slot)
    {
        if (!IsValid(slot) || !IsEnhanced(slot)) return false;
        SetEnhanced(slot, false);
        return true;
    }

    private static bool IsValid(EquipmentSlot slot) => slot >= EquipmentSlot.Weapon && slot <= EquipmentSlot.Clothes;

    private void SetEnhanced(EquipmentSlot slot, bool value)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: weaponEnhanced = value; break;
            case EquipmentSlot.Scarf: scarfEnhanced = value; break;
            case EquipmentSlot.Clothes: clothesEnhanced = value; break;
        }
        OnEquipmentChanged?.Invoke();
    }

    // Inspector 组件右键菜单；只改变本次运行状态，退出 Play Mode 后恢复。
    private void DebugAction(Action action)
    {
        if (!Application.isPlaying) { Debug.Log("请进入 Play Mode 后使用装备调试菜单。", this); return; }
        action();
        DebugStatus();
    }
    [ContextMenu("装备调试/获取一个灵")]
    private void DebugCollect() => DebugAction(() => CollectSpirits());
    [ContextMenu("装备调试/武器/装入灵（长剑）")]
    private void DebugSword() => DebugAction(() => TryEquipSpirit(EquipmentSlot.Weapon));
    [ContextMenu("装备调试/武器/取回灵（匕首）")]
    private void DebugDagger() => DebugAction(() => TryUnequipSpirit(EquipmentSlot.Weapon));
    [ContextMenu("装备调试/围巾/装入灵（斗篷）")]
    private void DebugCloak() => DebugAction(() => TryEquipSpirit(EquipmentSlot.Scarf));
    [ContextMenu("装备调试/围巾/取回灵（围巾）")]
    private void DebugScarf() => DebugAction(() => TryUnequipSpirit(EquipmentSlot.Scarf));
    [ContextMenu("装备调试/衣服/装入灵（护甲）")]
    private void DebugArmor() => DebugAction(() => TryEquipSpirit(EquipmentSlot.Clothes));
    [ContextMenu("装备调试/衣服/取回灵（衣服）")]
    private void DebugClothes() => DebugAction(() => TryUnequipSpirit(EquipmentSlot.Clothes));
    [ContextMenu("装备调试/打印当前状态")]
    private void DebugStatus() => Debug.Log($"灵：{TotalSpirits}/{MaxSpirits}，空闲 {AvailableSpirits}；武器：{(weaponEnhanced ? "长剑" : "匕首")}；围巾：{(scarfEnhanced ? "斗篷" : "围巾")}；衣服：{(clothesEnhanced ? "护甲" : "衣服")}", this);
}
