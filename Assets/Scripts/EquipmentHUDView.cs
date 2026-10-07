using UnityEngine;

public class EquipmentHUDView : MonoBehaviour
{
    public EquipmentMenuController menu;
    public EquipmentUIStyle style;
    public UnityEngine.UI.Image[] spiritIcons;
    public UnityEngine.UI.Image[] eyes;
    private PlayerEquipment equipment;
    private Health health;

    void OnEnable()
    {
        if (menu == null) menu = FindObjectOfType<EquipmentMenuController>();
        if (menu == null) return;
        equipment = menu.equipment;
        health = equipment != null ? equipment.GetComponent<Health>() : null;
        if (equipment != null) equipment.OnEquipmentChanged += RefreshSpirits;
        if (health != null) health.OnHPChanged += RefreshHP;
        RefreshSpirits();
        if (health != null) RefreshHP(health.CurrentHP, health.maxHP);
    }

    void OnDisable()
    {
        if (equipment != null) equipment.OnEquipmentChanged -= RefreshSpirits;
        if (health != null) health.OnHPChanged -= RefreshHP;
    }

    public void OpenMenu() { if (menu != null) menu.Open(); }

    public void RefreshSpirits()
    {
        if (equipment == null || style == null) return;
        for (int i = 0; i < spiritIcons.Length; i++)
            spiritIcons[i].sprite = i < equipment.TotalSpirits ? style.spiritOwned : style.spiritMissing;
    }

    public void RefreshHP(int current, int maximum)
    {
        if (style == null) return;
        int perEye = Mathf.Max(1, style.healthPerEye);
        int count = Mathf.CeilToInt(maximum / (float)perEye);
        for (int i = 0; i < eyes.Length; i++)
        {
            eyes[i].gameObject.SetActive(i < count);
            int remaining = current - i * perEye;
            eyes[i].sprite = remaining >= Mathf.Min(perEye, maximum - i * perEye) ? style.healthFull : style.healthDamaged;
            // 血量未耗尽但不足一只完整眼睛时，使用受伤图案并保留亮度。
            eyes[i].color = remaining > 0 ? Color.white : new Color(0.42f, 0.42f, 0.42f, 0.65f);
        }
    }
}
