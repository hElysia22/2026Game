using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public int damage = 1;
    public Vector2 size = Vector2.one;
    public float knockback = 3f;
    public float invincibleTime = 0.8f;
    public LayerMask targetLayer;
    public GameObject hitVfx;
    public AudioClip hitSfx;
    [Min(0), Tooltip("有效命中的顿帧秒数，0 关闭。")]
    public float hitStopTime = 0.03f;
    public GameObject owner;
    [Tooltip("是否在选中时绘制判定框线框；纯逻辑攻击框可以关掉。")]
    public bool showGizmo = true;

    private readonly HashSet<Collider2D> hitTargets = new();

    void OnEnable() => hitTargets.Clear();

    void Update()
    {
        var hits = Physics2D.OverlapBoxAll(transform.position, size, 0f, targetLayer);
        foreach (var h in hits)
        {
            if (h.gameObject == owner) continue;
            if (hitTargets.Contains(h)) continue;

            var health = h.GetComponent<Health>();
            if (health == null) continue;

            // 无敌（受击后的无敌时间）或已死亡：这次判定不生效，也不播放受击音效 / 特效 / 顿帧。
            // 注意不要把它记进 hitTargets：否则这次判定被白白消耗掉，无敌结束后还得等下一次重新启用。
            if (health.IsInvincible || health.IsDead) continue;
            hitTargets.Add(h);

            int dir = owner != null
                ? (int)Mathf.Sign(h.transform.position.x - owner.transform.position.x)
                : 1;
            if (dir == 0) dir = 1;

            Vector2 kb = new Vector2(dir * knockback, knockback * 0.5f);
            if (health.TryTakeDamage(damage, invincibleTime, kb))
            {
                if (GameAudio.Instance != null) GameAudio.Instance.PlayHit(hitSfx);
                else if (hitSfx != null) AudioSource.PlayClipAtPoint(hitSfx, transform.position);
                if (hitVfx != null) Instantiate(hitVfx, h.transform.position, Quaternion.identity);
                HitStop.Instance?.Play(hitStopTime);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
