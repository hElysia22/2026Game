using UnityEngine;

public class PlayerAttackCombo : MonoBehaviour
{
    public PlayerController player;
    public Transform hitboxOrigin;
    public LayerMask enemyLayer;

    private int comboIndex;
    private float timer;
    private bool active;
    private bool queued;
    private bool inWindow;
    private GameObject currentHitbox;

    public bool IsActive => active;

    public void Begin()
    {
        comboIndex = 0;
        StartStep();
    }

    public void Tick(bool attackPressedThisFrame)
    {
        if (!active) return;
        if (player.data.combo == null || player.data.combo.Length == 0) { End(); return; }

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
        timer = 0f;
        active = true;
        queued = false;
        inWindow = false;
        var step = player.data.combo[comboIndex];
        if (step.sfx != null) AudioSource.PlayClipAtPoint(step.sfx, transform.position);
        // Animator 参数由 PlayerController.UpdateAnimator 设置，
        // 若要区分 Attack1/2/3，可以在这里 animator.SetInteger("AttackIndex", comboIndex)
    }

    // 动画事件调用
    public void AE_OpenHitbox()
    {
        if (!active) return;
        CloseHitbox();
        var step = player.data.combo[comboIndex];
        var go = new GameObject("PlayerHitbox");
        go.transform.SetParent(hitboxOrigin, false);
        // 偏移使用局部坐标，朝向翻转由玩家父节点的 scale.x 统一处理。
        go.transform.localPosition = new Vector3(step.hitboxOffset.x, step.hitboxOffset.y, 0);

        var hb = go.AddComponent<Hitbox>();
        hb.damage = Mathf.RoundToInt(step.damage);
        hb.size = step.hitboxSize;
        hb.knockback = step.knockback;
        hb.invincibleTime = 0.5f;
        hb.targetLayer = enemyLayer;
        hb.hitSfx = step.sfx;
        hb.hitVfx = step.vfxPrefab;
        hb.owner = player.gameObject;

        currentHitbox = go;
    }

    public void AE_CloseHitbox() => CloseHitbox();

    void CloseHitbox()
    {
        if (currentHitbox != null) Destroy(currentHitbox);
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

        for (int i = 0; i < player.data.combo.Length; i++)
        {
            var step = player.data.combo[i];
            Vector3 center = hitboxOrigin.TransformPoint(new Vector3(
                step.hitboxOffset.x,
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
            Gizmos.DrawCube(center, step.hitboxSize);
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);
            Gizmos.DrawWireCube(center, step.hitboxSize);

            // 标个序号
#if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 0.3f, $"Atk{i + 1}");
#endif
        }
    }

    void End() => ForceCancel();
}
