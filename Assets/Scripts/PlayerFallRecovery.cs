using UnityEngine;

/// <summary>记录最后安全站立点；掉入断口扣血后原路返回，不重置装备。</summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerFallRecovery : MonoBehaviour
{
    [Tooltip("低于此世界高度视为掉入断口。")]
    public float fallY = -12;
    public int fallDamage = 1;
    public float edgeMargin = 0.4f;
    public Vector3 LastSafePosition { get; private set; }
    PlayerController player;
    Health health;
    Rigidbody2D body;
    Collider2D playerCollider;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        LastSafePosition = transform.position;
    }

    void FixedUpdate()
    {
        if (health.IsDead || Time.timeScale == 0) return;
        if (Mathf.Abs(body.velocity.y) > 0.5f) return;
        var hit = Physics2D.Raycast(body.position, Vector2.down, playerCollider.bounds.extents.y + 0.12f, player.groundLayer);
        if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < 0.65f) return;
        float left = hit.collider.bounds.min.x + edgeMargin;
        float right = hit.collider.bounds.max.x - edgeMargin;
        float x = left <= right ? Mathf.Clamp(body.position.x, left, right) : hit.collider.bounds.center.x;
        var safeHit = Physics2D.Raycast(new Vector2(x, body.position.y + 0.2f), Vector2.down, playerCollider.bounds.extents.y + 0.6f, player.groundLayer);
        if (safeHit.collider != hit.collider) return;
        LastSafePosition = new Vector3(x, safeHit.point.y + playerCollider.bounds.extents.y + 0.03f, transform.position.z);
    }

    void Update()
    {
        if (!health.IsDead && Time.timeScale > 0 && transform.position.y < fallY) Recover();
    }

    public void Recover()
    {
        if (health.IsDead) return;
        health.TryTakeDamage(Mathf.Max(1, fallDamage), 0.8f, Vector2.zero, true);
        if (health.IsDead) return;
        player.ResetAfterFall(LastSafePosition);
    }
}
