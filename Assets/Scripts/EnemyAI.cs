using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("Data")]
    public EnemyData data;

    [Header("Refs")]
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask playerLayer;
    public Transform attackOrigin;
    public GameObject hitboxPrefab;
    public Animator animator;

    public enum EnemyState { Patrol, Attack, Hurt, Dead }

    private Rigidbody2D rb;
    private Health health;
    private Transform player;
    private EnemyState state = EnemyState.Patrol;

    private int facing = 1;
    private float patrolStartX;
    private float attackCooldownTimer;
    private float attackStateTimer;
    private float hurtTimer;
    private bool hitboxOpened;

    public EnemyState CurrentState => state;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        health.maxHP = data.maxHP;
        patrolStartX = transform.position.x;

        attackCooldownTimer = data.attackCooldown;
    }

    void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (state == EnemyState.Dead) return;

        if (attackCooldownTimer > 0) attackCooldownTimer -= Time.deltaTime;
        if (hurtTimer > 0) hurtTimer -= Time.deltaTime;

        switch (state)
        {
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Attack: UpdateAttack(); break;
            case EnemyState.Hurt: UpdateHurt(); break;
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (state == EnemyState.Dead) return;

        switch (state)
        {
            case EnemyState.Patrol: FixedPatrol(); break;
            case EnemyState.Attack: FixedIdle(); break;
            case EnemyState.Hurt: FixedIdle(); break;
        }
    }

    // ============================================================
    // 扇形检测
    // ============================================================
    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;

        if (data.ignoreY)
            toPlayer = new Vector2(toPlayer.x, 0);

        float dist = toPlayer.magnitude;
        if (dist > data.detectRange) return false;
        if (dist < 0.01f) return true;

        Vector2 forward = new Vector2(facing, 0);
        float angle = Vector2.Angle(forward, toPlayer);

        return angle <= data.detectAngle * 0.5f;
    }

    bool InAttackRange()
    {
        if (player == null) return false;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        if (data.ignoreY)
            toPlayer = new Vector2(toPlayer.x, 0);

        return toPlayer.magnitude <= data.attackRange;
    }

    // ============================================================
    // Patrol
    // ============================================================
    void UpdatePatrol()
    {
        if (player == null) return;

        if (CanSeePlayer())
        {
            int dir = player.position.x > transform.position.x ? 1 : -1;
            if (dir != facing)
            {
                facing = dir;
                transform.localScale = new Vector3(facing, 1, 1);
            }

            if (InAttackRange() && attackCooldownTimer <= 0)
                EnterAttack();
        }
    }

    void FixedPatrol()
    {
        float offset = transform.position.x - patrolStartX;

        if (Mathf.Abs(offset) >= data.patrolDistance || WallAhead() || CliffAhead())
            Flip();

        rb.velocity = new Vector2(facing * data.moveSpeed, rb.velocity.y);
    }

    // ============================================================
    // Attack
    // ============================================================
    void EnterAttack()
    {
        state = EnemyState.Attack;
        attackStateTimer = data.attackWindup + data.attackRecovery;
        attackCooldownTimer = data.attackCooldown;
        hitboxOpened = false;

        if (player != null)
        {
            facing = player.position.x > transform.position.x ? 1 : -1;
            transform.localScale = new Vector3(facing, 1, 1);
        }
    }

    void UpdateAttack()
    {
        attackStateTimer -= Time.deltaTime;
        rb.velocity = new Vector2(0, rb.velocity.y);

        float windupElapsed = (data.attackWindup + data.attackRecovery) - attackStateTimer;
        if (!hitboxOpened && windupElapsed >= data.attackWindup)
        {
            hitboxOpened = true;
            OpenHitbox();
        }

        if (attackStateTimer <= 0)
        {
            state = EnemyState.Patrol;
            attackCooldownTimer = data.attackCooldown;
        }
    }

    void OpenHitbox()
    {
        if (attackOrigin == null) return;

        GameObject go;

        if (hitboxPrefab != null)
        {
            go = Instantiate(hitboxPrefab, attackOrigin.position, attackOrigin.rotation);
        }
        else
        {
            go = new GameObject("EnemyHitbox");
            go.transform.position = attackOrigin.position;

            int layer = LayerMask.NameToLayer("EnemyHitbox");
            if (layer >= 0) go.layer = layer;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f);
            box.isTrigger = true;

            go.AddComponent<Hitbox>();
        }

        var hb = go.GetComponent<Hitbox>();
        if (hb != null)
        {
            hb.damage = data.attackDamage;
            hb.knockback = data.knockback;
            hb.invincibleTime = 0.5f;
            hb.targetLayer = playerLayer;
            hb.owner = gameObject;
        }

        Destroy(go, 0.15f);
    }

    // ============================================================
    // Hurt
    // ============================================================
    void EnterHurt()
    {
        state = EnemyState.Hurt;
        hurtTimer = data.hurtDuration;
    }

    void UpdateHurt()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);

        if (hurtTimer <= 0)
            state = EnemyState.Patrol;
    }

    // ============================================================
    // Dead
    // ============================================================
    void HandleDeath()
    {
        state = EnemyState.Dead;
        rb.velocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 1f);
    }

    void HandleDamaged()
    {
        if (state == EnemyState.Dead) return;
        if (state != EnemyState.Hurt)
            EnterHurt();
    }

    // ============================================================
    // 通用
    // ============================================================
    void FixedIdle()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    bool WallAhead() => Physics2D.OverlapCircle(wallCheck.position, 0.1f, wallLayer);
    bool CliffAhead() => !Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

    void Flip()
    {
        facing *= -1;
        transform.localScale = new Vector3(facing, 1, 1);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetInteger("State", (int)state);
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    public void AE_EnemyOpenHitbox() => OpenHitbox();

    // ============================================================
    // Gizmos 调试
    // ============================================================
    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Vector3 origin = transform.position;
        Vector3 forward = new Vector3(facing, 0, 0);

        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);

        int segments = 24;
        float half = data.detectAngle * 0.5f;
        Vector3 prev = origin + Quaternion.Euler(0, 0, -half) * forward * data.detectRange;

        Gizmos.DrawLine(origin, prev);

        for (int i = 1; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 next = origin + Quaternion.Euler(0, 0, a) * forward * data.detectRange;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Gizmos.DrawLine(origin, prev);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(origin, data.attackRange);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
        Vector3 left = new Vector3(patrolStartX - data.patrolDistance, origin.y, 0);
        Vector3 right = new Vector3(patrolStartX + data.patrolDistance, origin.y, 0);
        Gizmos.DrawLine(left, right);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, 0.1f);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(wallCheck.position, 0.1f);
        }
    }
}