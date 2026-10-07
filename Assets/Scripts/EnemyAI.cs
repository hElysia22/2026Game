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

    public enum EnemyState { Patrol = 0, Attack = 1, Hurt = 2, Dead = 3, Alert = 4 }

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
    private float lostTargetTimer;

    public EnemyState CurrentState => state;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        health.ResetHealth(data.maxHP);
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
            case EnemyState.Alert: UpdateAlert(); break;
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
            case EnemyState.Alert: FixedAlert(); break;
            case EnemyState.Attack: FixedIdle(); break;
            case EnemyState.Hurt: FixedIdle(); break;
        }
    }

    // ============================================================
    // 扇形检测
    // ============================================================
    bool CanSeePlayer()
    {
        if (!HasLivingTarget()) return false;

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
        if (!HasLivingTarget()) return false;

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
        if (CanSeePlayer()) EnterAlert();
    }

    void FixedPatrol()
    {
        float offset = transform.position.x - patrolStartX;
        // 仅当向巡逻边界外移动时翻转，避免越界后一帧翻一次。
        if (offset * facing >= data.patrolDistance || WallAhead() || CliffAhead())
            Flip();
        rb.velocity = new Vector2(facing * data.moveSpeed, rb.velocity.y);
    }

    // ============================================================
    // Alert：冷却时保持距离；准备好后接近到攻击范围。
    // ============================================================
    bool HasLivingTarget()
    {
        if (player == null || !player.gameObject.activeInHierarchy) return false;
        var targetHealth = player.GetComponent<Health>();
        return targetHealth == null || !targetHealth.IsDead;
    }

    bool CanTrackTarget()
    {
        if (!HasLivingTarget()) return false;
        Vector2 delta = player.position - transform.position;
        if (data.ignoreY) delta.y = 0;
        return delta.magnitude <= Mathf.Max(data.detectRange, data.loseTargetRange);
    }

    void FacePlayer()
    {
        if (player == null) return;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        facing = dx > 0 ? 1 : -1;
        transform.localScale = new Vector3(facing, 1, 1);
    }

    void EnterAlert()
    {
        state = EnemyState.Alert;
        lostTargetTimer = 0;
        FacePlayer();
        FixedIdle();
    }

    void ReturnToPatrol()
    {
        state = EnemyState.Patrol;
        // 从脱战位置开始新的巡逻区间。
        patrolStartX = transform.position.x;
        lostTargetTimer = 0;
        FixedIdle();
    }

    void UpdateAlert()
    {
        if (!HasLivingTarget()) { ReturnToPatrol(); return; }
        if (!CanTrackTarget())
        {
            lostTargetTimer += Time.deltaTime;
            if (lostTargetTimer >= Mathf.Max(0, data.loseTargetDelay)) ReturnToPatrol();
            return;
        }
        lostTargetTimer = 0;
        FacePlayer();
        if (attackCooldownTimer <= 0 && InAttackRange()) EnterAttack();
    }

    void FixedAlert()
    {
        if (!CanTrackTarget()) { FixedIdle(); return; }
        FacePlayer();
        float distance = Mathf.Abs(player.position.x - transform.position.x);
        int moveDirection = 0;
        if (attackCooldownTimer <= 0)
        {
            // 进入有效攻击距离前仍保持警戒；留一点余量避免边界抖动。
            if (!InAttackRange() && distance > 0.01f) moveDirection = facing;
        }
        else
        {
            float desired = Mathf.Max(0, data.alertDistance);
            float tolerance = Mathf.Max(0, data.alertDistanceTolerance);
            if (distance > desired + tolerance) moveDirection = facing;
            else if (distance < desired - tolerance) moveDirection = -facing;
        }
        if (moveDirection != 0 && MovementBlocked(moveDirection)) moveDirection = 0;
        rb.velocity = new Vector2(moveDirection * Mathf.Max(0, data.alertMoveSpeed), rb.velocity.y);
    }

    bool MovementBlocked(int direction)
    {
        // 面向玩家后退时，探针应在移动侧，不能继续检测玩家所在侧。
        if (wallCheck != null)
        {
            Vector3 wallPoint = wallCheck.position;
            wallPoint.x = transform.position.x + Mathf.Abs(wallPoint.x - transform.position.x) * direction;
            if (Physics2D.OverlapCircle(wallPoint, 0.1f, wallLayer)) return true;
        }
        if (groundCheck != null)
        {
            Vector3 groundPoint = groundCheck.position;
            var body = GetComponent<Collider2D>();
            float step = body != null ? body.bounds.extents.x + 0.05f : 0.5f;
            groundPoint.x = transform.position.x + Mathf.Max(step, Mathf.Abs(groundPoint.x - transform.position.x)) * direction;
            if (!Physics2D.OverlapCircle(groundPoint, 0.1f, groundLayer)) return true;
        }
        return false;
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
            attackCooldownTimer = data.attackCooldown;
            if (CanTrackTarget()) EnterAlert();
            else ReturnToPatrol();
        }
    }

    void OpenHitbox()
    {
        if (attackOrigin == null) return;
        GameAudio.Instance?.PlayEnemyAttack();

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
            hb.hitStopTime = Mathf.Max(0, data.hitStopTime);
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
        {
            if (CanTrackTarget()) EnterAlert();
            else ReturnToPatrol();
        }
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

    bool WallAhead() => wallCheck != null && Physics2D.OverlapCircle(wallCheck.position, 0.1f, wallLayer);
    bool CliffAhead() => groundCheck != null && !Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

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

    public void AE_EnemyOpenHitbox()
    {
        if (state != EnemyState.Attack || hitboxOpened) return;
        hitboxOpened = true;
        OpenHitbox();
    }

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
