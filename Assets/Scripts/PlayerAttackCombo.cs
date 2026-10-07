using UnityEngine;

public class PlayerAttackCombo : MonoBehaviour
{
    public PlayerController player;
    public Transform hitboxOrigin;
    public LayerMask enemyLayer;
    [Tooltip("未接入 Animator 时，攻击期间自动开启攻击框；有 Animator 时由动画事件控制。")]
    public bool timedHitboxWithoutAnimator = true;
    [Tooltip("攻击动画剪辑名；用于按 duration 自动匹配播放速度，让动画随攻击状态一起结束。")]
    public string attackClipName = "Char_Attack";
    [Tooltip("Animator 中控制攻击动画速度的 float 参数名。")]
    public string attackSpeedParameter = "AttackSpeed";

    private int comboIndex;
    private float timer;
    private bool active;
    private bool queued;
    private bool inWindow;
    private GameObject currentHitbox;

    public bool IsActive => active;

    public void Begin()
    {
        if (player == null || player.data == null || player.data.combo == null || player.data.combo.Length == 0)
        { End(); return; }
        comboIndex = 0;
        StartStep();
    }

    public void Tick(bool attackPressedThisFrame)
    {
        if (!active) return;
        if (player.data.combo == null || player.data.combo.Length == 0) { End(); return; }

        if (timedHitboxWithoutAnimator && player.animator == null && currentHitbox == null)
            AE_OpenHitbox();
        timer += Time.deltaTime;
        var step = player.data.combo[comboIndex];

        inWindow = timer >= step.comboWindowStart && timer <= step.comboWindowEnd;

        // 用参数，不再用 Input.GetButtonDown
        if (attackPressedThisFrame && inWindow && comboIndex < player.data.combo.Length - 1)
            queued = true;

        if (queued && inWindow && comboIndex < player.data.combo.Length - 1)
        {
            comboIndex++;
            StartStep();
            return;
        }

        if (timer >= step.duration) End();
    }

    void StartStep()
    {
        CloseHitbox();
        timer = 0f;
        active = true;
        queued = false;
        inWindow = false;
        var step = player.data.combo[comboIndex];
        var equipment = player.GetComponent<PlayerEquipment>();
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayPlayerAttack(equipment != null && equipment.IsEnhanced(EquipmentSlot.Weapon), step.sfx);
        else if (step.sfx != null) AudioSource.PlayClipAtPoint(step.sfx, transform.position);
        // Animator 参数由 PlayerController.UpdateAnimator 设置，
        // 若要区分 Attack1/2/3，可以在这里 animator.SetInteger("AttackIndex", comboIndex)
        SyncAttackAnimationSpeed(step);
    }

    /// <summary>
    /// 让攻击动画在配置的 duration 内正好播完：动画随攻击状态一起开始、一起结束。
    /// 以后改 CharacterData 的 duration 不需要再手改动画长度。
    /// </summary>
    void SyncAttackAnimationSpeed(AttackStep step)
    {
        if (player == null || player.animator == null || step == null || step.duration <= 0f) return;
        var controller = player.animator.runtimeAnimatorController;
        if (controller == null || string.IsNullOrEmpty(attackSpeedParameter)) return;

        float clipLength = 0f;
        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name == attackClipName) { clipLength = clip.length; break; }
        }
        if (clipLength <= 0f) return;

        player.animator.SetFloat(attackSpeedParameter, clipLength / step.duration);
    }

    // 动画事件调用
    public void AE_OpenHitbox()
    {
        if (!active || hitboxOrigin == null) return;
        CloseHitbox();
        var step = player.data.combo[comboIndex];
        var go = new GameObject("PlayerHitbox");
        go.transform.SetParent(hitboxOrigin, false);
        // 偏移使用局部坐标，朝向翻转由玩家父节点的 scale.x 统一处理。
        var equipment = player.GetComponent<PlayerEquipment>();
        float reachBonus = equipment != null ? equipment.ReachBonus : 0;
        float widthBonus = equipment != null ? equipment.WidthBonus : 0;
        int damageBonus = equipment != null ? equipment.DamageBonus : 0;
        go.transform.localPosition = new Vector3(step.hitboxOffset.x + reachBonus, step.hitboxOffset.y, 0);

        // 每次开启攻击框读取装备快照，不修改共享的 AttackStep。
        var hb = go.AddComponent<Hitbox>();
        hb.damage = Mathf.RoundToInt(step.damage) + damageBonus;
        hb.size = new Vector2(step.hitboxSize.x + widthBonus, step.hitboxSize.y);
        hb.knockback = step.knockback;
        hb.invincibleTime = 0.5f;
        hb.targetLayer = enemyLayer;
        // 挥动和命中是两个事件，命中不能重复播放 AttackStep 的挥动音效。
        hb.hitSfx = GameAudio.Instance != null ? GameAudio.Instance.hit : null;
        hb.hitStopTime = Mathf.Max(0, player.data.hitStopTime);
        hb.hitVfx = step.vfxPrefab;
        hb.owner = player.gameObject;

        currentHitbox = go;
    }

    public void AE_CloseHitbox() => CloseHitbox();

    void CloseHitbox()
    {
        if (currentHitbox != null)
        {
            currentHitbox.SetActive(false);
            if (Application.isPlaying) Destroy(currentHitbox);
            else DestroyImmediate(currentHitbox);
        }
        currentHitbox = null;
    }

    public void ForceCancel()
    {
        active = false;
        queued = false;
        inWindow = false;
        comboIndex = 0;
        CloseHitbox();
    }
    void OnDrawGizmosSelected()
    {
        if (player == null || player.data == null) return;
        if (player.data.combo == null) return;
        if (hitboxOrigin == null) return;

        var equipment = player.GetComponent<PlayerEquipment>();
        for (int i = 0; i < player.data.combo.Length; i++)
        {
            var step = player.data.combo[i];
            Vector3 center = hitboxOrigin.TransformPoint(new Vector3(
                step.hitboxOffset.x + (equipment != null ? equipment.ReachBonus : 0),
                step.hitboxOffset.y,
                0
            ));

            // 用不同颜色区分三段
            Color c = i switch
            {
                0 => new Color(1f, 0.3f, 0.3f, 0.3f),  // 红
                1 => new Color(0.3f, 1f, 0.3f, 0.3f),  // 绿
                2 => new Color(0.3f, 0.3f, 1f, 0.3f),  // 蓝
                _ => new Color(1f, 1f, 0f, 0.3f)
            };

            Gizmos.color = c;
            Vector2 size = step.hitboxSize + new Vector2(equipment != null ? equipment.WidthBonus : 0, 0);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);
            Gizmos.DrawWireCube(center, size);

            // 标个序号
#if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 0.3f, $"Atk{i + 1}");
#endif
        }
    }

    void End() => ForceCancel();
}
