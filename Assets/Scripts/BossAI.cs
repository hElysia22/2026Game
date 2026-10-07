using UnityEngine;

/// <summary>
/// Boss 只有「待机 / 攻击」两个状态：待机随机 2~3 秒后拍地，
/// 拍地瞬间向左右各发一道贴地冲击波；冲击波很矮，玩家跳起来即可躲开。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
[DisallowMultipleComponent]
public class BossAI : MonoBehaviour
{
    public enum BossState { Idle = 0, Attack = 1, Dead = 2 }

    [Header("Refs")]
    public Animator animator;
    [Tooltip("左右翻转用的精灵节点；留空则翻转自身。")]
    public Transform spriteRoot;
    [Tooltip("冲击波生成点；留空则用自身位置。")]
    public Transform slamOrigin;
    [Tooltip("冲击波撞墙检测的层，通常是 Ground。")]
    public LayerMask groundLayer;
    [Tooltip("冲击波要打的目标层，通常是 Player。")]
    public LayerMask playerLayer;

    [Header("Timing")]
    [Tooltip("两次攻击之间的随机冷却区间（秒）。")]
    public Vector2 attackInterval = new Vector2(2f, 3f);
    [Min(0), Tooltip("进入战斗后第一次攻击的延迟。")]
    public float firstAttackDelay = 1.2f;
    [Tooltip("攻击状态持续时间，应与 Boss_Attack 动画长度一致。")]
    public float attackDuration = 1f;
    [Tooltip("没有 Animator 时按这个时间点拍地。")]
    public float slamTimeFallback = 0.5f;

    [Header("Shockwave")]
    public Sprite shockwaveSprite;
    public float waveSpeed = 7.5f;
    public float waveLifetime = 2.4f;
    [Tooltip("冲击波判定框宽度（世界单位）。")]
    public float waveLength = 1.5f;
    [Tooltip("冲击波判定框高度；越小越容易跳过。")]
    public float waveHeight = 0.55f;
    public float waveSpawnYOffset = 0.28f;
    public int waveDamage = 2;
    public float waveKnockback = 5f;
    public int waveSortingOrder = 5;

    [Header("Approach：冷却期间跳跃逼近")]
    public bool approachDuringCooldown = true;
    [Tooltip("离玩家多近就不再靠近。")]
    public float approachStopDistance = 3.2f;
    [Tooltip("跳跃时的水平速度：越大跳得越远。")]
    public float hopHorizontalSpeed = 3f;
    [Tooltip("起跳速度：越大跳得越高（重力由 Boss 刚体的 Gravity Scale 决定）。")]
    public float hopUpSpeed = 9f;
    [Tooltip("两次起跳之间的间隔（秒）。")]
    public float hopInterval = 2f;

    [Header("Contact damage：贴身碰撞伤害")]
    [Tooltip("子物体上的所有 Hitbox 都会生效；每个框的 size / 位置在子物体上单独调，用来贴合不规则外形。")]
    public int contactDamage = 1;
    [Tooltip("忽略与玩家的实体碰撞：落下时不会把玩家压进地面，伤害交给碰撞伤害盒。")]
    public bool ignorePlayerCollision = true;
    public float contactKnockback = 6f;
    public float contactInvincibleTime = 0.8f;
    [Min(0)] public float contactHitStopTime = 0.06f;
    [Tooltip("碰撞伤害的判定间隔：Hitbox 每次启用只对同一目标判定一次，所以按此间隔重新启用。")]
    public float contactInterval = 0.8f;

    [Header("Facing")]
    public bool faceTarget = true;

    public BossState CurrentState { get; private set; } = BossState.Idle;

    Rigidbody2D rb;
    Health health;
    Transform target;
    float cooldown;
    float attackTimer;
    float hopTimer;
    float hopGrace;
    float contactTimer;
    Hitbox[] contactHitboxes;
    bool slamDone;
    int facing = -1;
    static Sprite fallbackSprite;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        if (spriteRoot == null) spriteRoot = transform;
        if (playerLayer.value == 0)
        {
            int layer = LayerMask.NameToLayer("Player");
            if (layer >= 0) playerLayer = 1 << layer;
        }
        // 贴身碰撞伤害：子物体上所有 Hitbox 都生效（可以放多个框来贴合不规则外形）。
        contactHitboxes = GetComponentsInChildren<Hitbox>(true);
        foreach (var hb in contactHitboxes)
        {
            if (hb == null) continue;
            hb.damage = contactDamage;
            hb.knockback = contactKnockback;
            hb.invincibleTime = contactInvincibleTime;
            hb.hitStopTime = contactHitStopTime;
            hb.targetLayer = playerLayer;
            hb.owner = gameObject;
            hb.showGizmo = false;   // 纯逻辑判定框，不画线框
        }
    }

    void OnEnable() { if (health != null) health.OnDeath += HandleDeath; }
    void OnDisable() { if (health != null) health.OnDeath -= HandleDeath; }

    void Start()
    {
        cooldown = Mathf.Max(0f, firstAttackDelay);
        AcquireTarget();
        IgnorePlayerCollision();
    }

    /// <summary>忽略与玩家的实体碰撞：Boss 落在玩家头上时不会把人压进地面。</summary>
    void IgnorePlayerCollision()
    {
        if (!ignorePlayerCollision) return;
        var myCol = GetComponent<Collider2D>();
        var playerCol = target != null ? target.GetComponent<Collider2D>() : null;
        if (myCol == null || playerCol == null) return;
        Physics2D.IgnoreCollision(myCol, playerCol, true);
    }

    void Update()
    {
        if (CurrentState == BossState.Dead) return;
        if (target == null) AcquireTarget();

        if (CurrentState == BossState.Idle)
        {
            FaceTarget();
            cooldown -= Time.deltaTime;
            // 冷却期间跳跃逼近玩家。
            if (approachDuringCooldown) UpdateApproach();
            // 位移只来自跳跃：地面待机时收住水平速度，落地即停。
            if (hopGrace > 0f) hopGrace -= Time.deltaTime;
            else KeepPlanted();
            // 必须落地后才拍地：空中开攻击会把冲击波生成在半空。
            if (cooldown <= 0f && OnGround() && HasLivingTarget()) BeginAttack();
        }
        else if (CurrentState == BossState.Attack)
        {
            attackTimer += Time.deltaTime;
            // 攻击期间站定拍地。
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
            // 有 Animator 时由动画事件 AE_Slam 拍地，没有则按时间兜底。
            if (!slamDone && animator == null && attackTimer >= slamTimeFallback) Slam();
            if (attackTimer >= attackDuration) EndAttack();
        }

        UpdateContactHitbox();
        UpdateAnimator();
    }

    /// <summary>碰撞伤害：所有常驻攻击框按 contactInterval 重新启用一次（Hitbox 每次启用只对同一目标判定一次）。</summary>
    void UpdateContactHitbox()
    {
        if (contactHitboxes == null || contactHitboxes.Length == 0) return;
        contactTimer -= Time.deltaTime;
        if (contactTimer > 0f) return;
        contactTimer = Mathf.Max(0.1f, contactInterval);
        foreach (var hb in contactHitboxes)
        {
            if (hb == null) continue;
            hb.enabled = false;
            hb.enabled = true;
        }
    }

    /// <summary>冷却期间：离得远就朝玩家小跳一步（水平速度慢、落地后再跳）。</summary>
    void UpdateApproach()
    {
        if (rb == null || target == null) return;
        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) <= approachStopDistance)
        {
            hopTimer = Mathf.Max(hopTimer, 0.2f);
            return;
        }
        if (hopTimer > 0f) hopTimer -= Time.deltaTime;
        if (hopTimer > 0f) return;
        if (!OnGround()) return;

        int dir = dx > 0f ? 1 : -1;
        facing = dir;
        ApplyFacing();
        rb.velocity = new Vector2(dir * hopHorizontalSpeed, hopUpSpeed);
        hopTimer = hopInterval;
        hopGrace = 0.25f;   // 起跳后短暂不做“地面定住”，否则会立刻把跳跃速度抹掉
    }

    /// <summary>地面待机时收住水平速度：Boss 的位移只来自跳跃，不自己滑行。</summary>
    void KeepPlanted()
    {
        if (rb == null) return;
        if (!OnGround()) return;                            // 空中保留跳跃惯性
        if (Mathf.Abs(rb.velocity.x) < 0.001f) return;
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    bool OnGround()
    {
        Vector2 p = new Vector2(transform.position.x, transform.position.y - 0.12f);
        return Physics2D.OverlapCircle(p, 0.28f, groundLayer);
    }

    void AcquireTarget()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.transform;
    }

    bool HasLivingTarget()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        var h = target.GetComponent<Health>();
        return h == null || !h.IsDead;
    }

    void FaceTarget()
    {
        if (!faceTarget || target == null) return;
        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.05f) return;
        facing = dx > 0f ? 1 : -1;
        ApplyFacing();
    }

    void ApplyFacing()
    {
        // 翻转根节点：精灵子节点的缩放现在由动画曲线驱动，不能再拿它做朝向。
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * facing, s.y, s.z);
    }

    void BeginAttack()
    {
        CurrentState = BossState.Attack;
        attackTimer = 0f;
        slamDone = false;
        // 攻击期间既不能跳、也不能再攻击：连水平位置一起锁住，避免被玩家推着走。
        LockPositionX(true);
        FaceTarget();
        if (GameAudio.Instance != null) GameAudio.Instance.PlayEnemyAttack();
    }

    void EndAttack()
    {
        CurrentState = BossState.Idle;
        LockPositionX(false);
        float min = Mathf.Min(attackInterval.x, attackInterval.y);
        float max = Mathf.Max(attackInterval.x, attackInterval.y);
        cooldown = Random.Range(min, max);
    }

    /// <summary>锁/解锁水平位置：Boss 是动态刚体，锁住后不会被碰撞推动。</summary>
    void LockPositionX(bool locked)
    {
        if (rb == null) return;
        rb.velocity = new Vector2(0f, rb.velocity.y);
        rb.constraints = locked
            ? RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX
            : RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>动画事件：拍地那一帧调用。</summary>
    public void AE_Slam()
    {
        if (CurrentState != BossState.Attack) return;
        Slam();
    }

    void Slam()
    {
        if (slamDone) return;
        slamDone = true;
        SpawnWave(-1);
        SpawnWave(1);
    }

    void SpawnWave(int dir)
    {
        Vector3 origin = (slamOrigin != null ? slamOrigin.position : transform.position)
                         + new Vector3(0f, waveSpawnYOffset, 0f);

        var go = new GameObject(dir < 0 ? "Shockwave_L" : "Shockwave_R");
        go.transform.position = origin;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = shockwaveSprite != null ? shockwaveSprite : GetFallbackSprite();
        sr.flipX = dir < 0;
        sr.sortingOrder = waveSortingOrder;

        float spriteW = sr.sprite != null ? Mathf.Max(0.01f, sr.sprite.bounds.size.x) : 1f;
        float spriteH = sr.sprite != null ? Mathf.Max(0.01f, sr.sprite.bounds.size.y) : 1f;
        go.transform.localScale = new Vector3(waveLength / spriteW, waveHeight / spriteH, 1f);

        var hb = go.AddComponent<Hitbox>();
        hb.damage = waveDamage;
        hb.size = new Vector2(waveLength, waveHeight);
        hb.knockback = waveKnockback;
        hb.invincibleTime = 0.6f;
        hb.hitStopTime = 0.02f;
        hb.targetLayer = playerLayer;
        hb.owner = gameObject;

        var sw = go.AddComponent<Shockwave>();
        sw.direction = dir;
        sw.speed = waveSpeed;
        sw.lifetime = waveLifetime;
        sw.groundLayer = groundLayer;
        sw.visual = sr;
        sw.hitbox = hb;
        sw.owner = gameObject;
        sw.baseScale = go.transform.localScale;
    }

    static Sprite GetFallbackSprite()
    {
        if (fallbackSprite == null)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var px = new Color32[64];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 245, 220, 200);
            tex.SetPixels32(px);
            tex.Apply();
            fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            fallbackSprite.name = "ShockwaveFallback";
        }
        return fallbackSprite;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetInteger("State", (int)CurrentState);
    }

    void HandleDeath()
    {
        CurrentState = BossState.Dead;
        if (animator != null) animator.SetInteger("State", (int)BossState.Dead);
        if (rb != null) rb.velocity = Vector2.zero;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
        Vector3 o = (slamOrigin != null ? slamOrigin.position : transform.position) + new Vector3(0f, waveSpawnYOffset, 0f);
        Gizmos.DrawWireCube(o + new Vector3(waveLength, 0f, 0f), new Vector3(waveLength, waveHeight, 0f));
        Gizmos.DrawWireCube(o - new Vector3(waveLength, 0f, 0f), new Vector3(waveLength, waveHeight, 0f));
    }
}
