using UnityEngine;

/// <summary>
/// 贴地滑行的冲击波：判定框很矮，玩家跳起来即可躲开；撞墙或超时消散。
/// 由 BossAI 在拍地时生成，左右各一道。
/// </summary>
[DisallowMultipleComponent]
public class Shockwave : MonoBehaviour
{
    public int direction = 1;
    public float speed = 7.5f;
    public float lifetime = 2.4f;
    public LayerMask groundLayer;
    public SpriteRenderer visual;
    public Hitbox hitbox;
    public GameObject owner;
    public Vector3 baseScale = Vector3.one;

    float life;
    bool fading;

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        life += dt;
        transform.position += new Vector3(direction * speed * dt, 0f, 0f);

        // 撞到实体墙就消散，避免穿墙继续前进。
        if (!fading && Physics2D.Raycast(transform.position, new Vector2(direction, 0f), 0.4f, groundLayer))
            fading = true;

        float t = Mathf.Clamp01(life / Mathf.Max(0.05f, lifetime));
        if (visual != null)
        {
            Color c = visual.color;
            c.a = fading ? Mathf.MoveTowards(c.a, 0f, dt * 6f) : Mathf.Lerp(0.95f, 0f, t * t);
            visual.color = c;
        }
        transform.localScale = baseScale * (1f + 0.35f * t);

        if (fading && hitbox != null) hitbox.enabled = false;

        bool fadedOut = visual == null || visual.color.a <= 0.02f;
        if (life >= lifetime || (fading && fadedOut)) Destroy(gameObject);
    }
}
