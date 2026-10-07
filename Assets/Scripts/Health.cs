using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHP = 3;
    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;
    public bool IsInvincible { get; private set; }

    public event Action<int, int> OnHPChanged;
    public event Action OnDamaged;
    public event Action OnDeath;

    private float invincibleTimer;

    void Awake() => ResetHealth(maxHP);

    /// <summary>出生/重生时恢复生命；角色数据中的 maxHP 是玩家生命上限的来源。</summary>
    public void ResetHealth(int maximum)
    {
        maxHP = Mathf.Max(1, maximum);
        CurrentHP = maxHP;
        IsInvincible = false;
        invincibleTimer = 0;
        OnHPChanged?.Invoke(CurrentHP, maxHP);
    }

    void Update()
    {
        if (IsInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0) IsInvincible = false;
        }
    }

    public bool TryTakeDamage(int amount, float invincibleTime, Vector2 knockback, bool ignoreInvincibility = false)
    {
        if (IsDead || (IsInvincible && !ignoreInvincibility) || amount <= 0) return false;
        var equipment = GetComponent<PlayerEquipment>();
        if (equipment != null) amount = Mathf.Max(1, amount - equipment.DamageReduction);

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, maxHP);
        OnDamaged?.Invoke();

        if (invincibleTime > 0)
        {
            IsInvincible = true;
            invincibleTimer = invincibleTime;
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null && knockback != Vector2.zero)
            rb.velocity = new Vector2(knockback.x, knockback.y);

        if (IsDead) OnDeath?.Invoke();
        return true;
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnHPChanged?.Invoke(CurrentHP, maxHP);
    }

    /// <summary>受伤后强制一段无敌时间（玩家用 CharacterData.invincibleTime）。</summary>
    public void ApplyInvincibility(float seconds)
    {
        if (seconds <= 0f) return;
        IsInvincible = true;
        invincibleTimer = Mathf.Max(invincibleTimer, seconds);
    }
}