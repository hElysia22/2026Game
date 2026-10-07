using System;
using UnityEngine;

[Serializable]
public class EquipmentPresentation
{
    public EquipmentSlot slot;
    public string baseName;
    public string enhancedName;
    [TextArea(2, 4)] public string description;
    public Sprite baseIcon;
    public Sprite enhancedIcon;
}

[CreateAssetMenu(menuName = "Game/UI/Equipment Style")]
public class EquipmentUIStyle : ScriptableObject
{
    public TMPro.TMP_FontAsset font;
    public Sprite bag;
    public Sprite spiritOwned;
    public Sprite spiritMissing;
    public Sprite healthFull;
    public Sprite healthDamaged;
    public Sprite healthOrnament;
    [Min(1)] public int healthPerEye = 1;
    public Color textColor = new Color(1f, 0.967f, 0.82f);
    public Color mutedColor = new Color(0.69f, 0.73f, 0.64f);
    public Color accentColor = new Color(0.85f, 0.67f, 0.36f);
    public Color slotColor = new Color(0.12f, 0.23f, 0.20f);
    public Color selectedColor = new Color(0.26f, 0.36f, 0.27f);
    public EquipmentPresentation[] entries;

    public EquipmentPresentation Get(EquipmentSlot slot)
    {
        if (entries == null) return null;
        foreach (var entry in entries) if (entry != null && entry.slot == slot) return entry;
        return null;
    }
}
