using System;
using UnityEngine;

[Serializable]
public class EquipmentSlotWidgets
{
    public EquipmentSlot slot;
    public UnityEngine.UI.Image background;
    public UnityEngine.UI.Image icon;
    public UnityEngine.UI.Image spirit;
    public TMPro.TMP_Text name;
    public TMPro.TMP_Text status;
    public GameObject selectedMark;
}

/// <summary>Canvas 上的常驻视图；面板子对象隐藏时仍保持事件订阅。</summary>
public class EquipmentPanelView : MonoBehaviour
{
    public EquipmentMenuController menu;
    public EquipmentUIStyle style;
    public GameObject panelRoot;
    public GameObject emptyDetail;
    public GameObject selectedDetail;
    public EquipmentSlotWidgets[] slots;
    public UnityEngine.UI.Image preview;
    public TMPro.TMP_Text title;
    public TMPro.TMP_Text upgradeLabel;
    public TMPro.TMP_Text description;
    public TMPro.TMP_Text requirement;
    public TMPro.TMP_Text summary;
    public TMPro.TMP_Text statusMessage;
    public UnityEngine.UI.Button actionButton;
    public TMPro.TMP_Text actionLabel;
    public UnityEngine.UI.Image[] panelSpirits;

    private int selected = -1;
    private PlayerEquipment equipment;
    public bool HasSelection => selected >= 0;
    public EquipmentSlot SelectedSlot => (EquipmentSlot)selected;

    void OnEnable()
    {
        if (menu == null) menu = FindObjectOfType<EquipmentMenuController>();
        if (menu == null) return;
        equipment = menu.equipment;
        menu.OnOpenChanged += HandleOpen;
        if (equipment != null) equipment.OnEquipmentChanged += Refresh;
        HandleOpen(menu.IsOpen);
    }

    void OnDisable()
    {
        if (menu != null) menu.OnOpenChanged -= HandleOpen;
        if (equipment != null) equipment.OnEquipmentChanged -= Refresh;
    }

    void HandleOpen(bool open)
    {
        if (panelRoot != null) panelRoot.SetActive(open);
        if (open) Refresh();
    }

    public void SelectWeapon() => Select(EquipmentSlot.Weapon);
    public void SelectScarf() => Select(EquipmentSlot.Scarf);
    public void SelectClothes() => Select(EquipmentSlot.Clothes);
    public void OpenMenu() { if (menu != null) menu.Open(); }
    public void CloseMenu() { if (menu != null) menu.Close(); }

    public void Select(EquipmentSlot slot)
    {
        if (slot < EquipmentSlot.Weapon || slot > EquipmentSlot.Clothes) return;
        selected = (int)slot;
        Refresh();
    }

    public void ApplySpirit()
    {
        if (!HasSelection || equipment == null) return;
        bool success = equipment.IsEnhanced(SelectedSlot)
            ? equipment.TryUnequipSpirit(SelectedSlot)
            : equipment.TryEquipSpirit(SelectedSlot);
        Refresh();
        if (!success && statusMessage != null) statusMessage.text = "没有可用的灵，请先从其他装备取回。";
    }

    public void Refresh()
    {
        if (equipment == null && menu != null) equipment = menu.equipment;
        if (equipment == null || style == null) return;
        foreach (var row in slots)
        {
            var entry = style.Get(row.slot);
            if (entry == null) continue;
            bool enhanced = equipment.IsEnhanced(row.slot);
            row.icon.sprite = enhanced ? entry.enhancedIcon : entry.baseIcon;
            row.name.text = enhanced ? entry.enhancedName : entry.baseName;
            row.status.text = enhanced ? "已装入灵  1 / 1" : "未装入灵  0 / 1";
            row.spirit.sprite = enhanced ? style.spiritOwned : style.spiritMissing;
            row.background.color = selected == (int)row.slot ? style.selectedColor : style.slotColor;
            row.selectedMark.SetActive(selected == (int)row.slot);
        }
        summary.text = $"已获得 {equipment.TotalSpirits} / {PlayerEquipment.MaxSpirits}    可用 {equipment.AvailableSpirits}";
        for (int i = 0; i < panelSpirits.Length; i++)
        {
            panelSpirits[i].sprite = i < equipment.TotalSpirits ? style.spiritOwned : style.spiritMissing;
            panelSpirits[i].color = Color.white;
        }
        emptyDetail.SetActive(!HasSelection);
        selectedDetail.SetActive(HasSelection);
        if (!HasSelection) return;

        var item = style.Get(SelectedSlot);
        bool equipped = equipment.IsEnhanced(SelectedSlot);
        title.text = equipped ? item.enhancedName : item.baseName;
        preview.sprite = equipped ? item.enhancedIcon : item.baseIcon;
        upgradeLabel.text = equipped ? "灵已寄于此物 · 强化中" : $"装入灵后化为 {item.enhancedName}";
        description.text = item.description + "\n\n" + EffectText(SelectedSlot);
        requirement.text = equipped ? "已装入：1 个灵    取回后可自由分配" : $"强化需要：1 个灵    当前可用：{equipment.AvailableSpirits} 个";
        actionLabel.text = equipped ? "取回灵" : "装入灵";
        actionButton.interactable = equipped || equipment.AvailableSpirits > 0;
        statusMessage.text = equipped ? "取回灵后，装备恢复原本的形态。"
            : equipment.AvailableSpirits > 0 ? "装入一个灵，即可获得下方介绍的强化效果。"
            : equipment.TotalSpirits == 0 ? "尚未获得灵。找到灵后，就能强化装备。" : "两个灵已分配，请先从其他装备取回。";
    }

    private string EffectText(EquipmentSlot slot)
    {
        var d = equipment.data;
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                return $"长剑强化\n每段攻击伤害 +{(d != null ? d.swordDamageBonus : 2)}\n攻击距离 +{(d != null ? d.swordReachBonus : 0.5f):0.##} · 攻击框宽度 +{(d != null ? d.swordWidthBonus : 0.5f):0.##}";
            case EquipmentSlot.Scarf:
                return $"斗篷强化\n获得一次空中跳跃，可实现二段跳。\n空中起跳力度 ×{(d != null ? d.airJumpForceMultiplier : 1):0.##}";
            default:
                return $"护甲强化\n每次受到的伤害减少 {(d != null ? d.armorDamageReduction : 1)} 点。\n每次命中至少承受 1 点伤害。";
        }
    }
}
