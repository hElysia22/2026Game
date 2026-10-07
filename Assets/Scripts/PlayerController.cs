using UnityEngine;

public enum PlayerState { Idle, Run, Jump, Fall, Attack, Hurt, Dead, Climb }

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Data")]
    public CharacterData data;

    [Header("Ladder")]
    public Transform ladderCheck;
    public LayerMask ladderLayer;
    public float ladderCheckRadius = 0.3f;

    private Ladder currentLadder;
    private bool nearLadder;
    private float ladderCooldownTimer;

    [Header("Refs")]
    public PlayerInputReader input;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public PlayerAttackCombo attackCombo;
    public Animator animator;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public bool IsGrounded { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public bool IsDead => CurrentState == PlayerState.Dead;

    private Rigidbody2D rb;
    private Health health;
    private PlayerEquipment equipment;
    private bool groundJumpConsumed;
    private bool airJumpUsed;

    // 输入缓存
    private float moveInput;
    private bool jumpPressed;
    private bool attackPressed;

    // 计时器
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float hurtTimer;

    // 跳跃切断
    private bool jumpCutApplied;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        health.ResetHealth(data.maxHP);
        equipment = GetComponent<PlayerEquipment>();
        rb.gravityScale = data.gravityScale;
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

    void Update()
    {
        if (input != null && input.IsGameplayInputBlocked)
        {
            moveInput = 0;
            jumpBufferTimer = 0;
            attackPressed = false;
            UpdateAnimator();
            return;
        }
        ReadInput();
        CheckLadder();

        bool attackThisFrame = attackPressed;
        attackPressed = false;

        CheckGround();
        UpdateTimers();

        switch (CurrentState)
        {
            case PlayerState.Idle: UpdateIdle(attackThisFrame); break;
            case PlayerState.Run: UpdateRun(attackThisFrame); break;
            case PlayerState.Jump: UpdateJump(attackThisFrame); break;
            case PlayerState.Fall: UpdateFall(attackThisFrame); break;
            case PlayerState.Attack: UpdateAttack(attackThisFrame); break;
            case PlayerState.Hurt: UpdateHurt(); break;
            case PlayerState.Dead: UpdateDead(); break;
            case PlayerState.Climb: UpdateClimb(attackThisFrame); break;
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (input != null && input.IsGameplayInputBlocked) return;
        switch (CurrentState)
        {
            case PlayerState.Idle: FixedIdle(); break;
            case PlayerState.Run: FixedRun(); break;
            case PlayerState.Jump: FixedJump(); break;
            case PlayerState.Fall: FixedFall(); break;
            case PlayerState.Attack: FixedAttack(); break;
            case PlayerState.Hurt: FixedHurt(); break;
            case PlayerState.Dead: FixedDead(); break;
            case PlayerState.Climb: FixedClimb(); break;
        }
    }

    // ------------------------------------------------------------
    // 输入与检测
    // ------------------------------------------------------------

    void ReadInput()
    {
        if (input == null) return;

        moveInput = input.MoveInput;

        if (input.ConsumeJump())
            jumpBufferTimer = data.jumpBufferTime;

        if (input.ConsumeAttack())
            attackPressed = true;
    }

    void CheckLadder()
    {
        if (ladderCheck == null)
        {
            nearLadder = false;
            currentLadder = null;
            return;
        }

        var hits = Physics2D.OverlapCircleAll(ladderCheck.position, ladderCheckRadius, ladderLayer);
        nearLadder = hits.Length > 0;
        currentLadder = nearLadder ? hits[0].GetComponent<Ladder>() : null;
    }

    void CheckGround()
    {
        bool wasGrounded = IsGrounded;
        // 起跳后探针尚未离地时不能重置跳跃次数。
        IsGrounded = rb.velocity.y <= 0.01f && Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        if (IsGrounded)
        {
            coyoteTimer = data.coyoteTime;
            groundJumpConsumed = false;
            airJumpUsed = false;
        }
        else if (wasGrounded && !groundJumpConsumed)
            coyoteTimer = data.coyoteTime;
    }

    void UpdateTimers()
    {
        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
        if (hurtTimer > 0) hurtTimer -= Time.deltaTime;
        if (ladderCooldownTimer > 0) ladderCooldownTimer -= Time.deltaTime;
    }

    // ------------------------------------------------------------
    // 状态切换
    // ------------------------------------------------------------

    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        OnExitState(CurrentState);
        CurrentState = newState;
        OnEnterState(newState);
    }

    void OnEnterState(PlayerState s)
    {
        switch (s)
        {
            case PlayerState.Attack:
                attackCombo?.Begin();
                break;
            case PlayerState.Hurt:
                // 受击要“倒下 + 起身”整段播完：取配置时长与动画长度的较大者。
                hurtTimer = Mathf.Max(data.hurtDuration, ClipLength("Char_Hurt"));
                break;
            case PlayerState.Climb:
                rb.gravityScale = 0f;
                rb.velocity = Vector2.zero;
                break;
        }
    }

    void OnExitState(PlayerState s)
    {
        switch (s)
        {
            case PlayerState.Attack:
                attackCombo?.ForceCancel();
                break;
            case PlayerState.Climb:
                rb.gravityScale = data.gravityScale;
                break;
        }
    }

    // ------------------------------------------------------------
    // 各状态 Update
    // ------------------------------------------------------------

    void UpdateIdle(bool attackThisFrame)
    {
        if (TryConsumeAttack(attackThisFrame)) return;
        if (TryEnterClimb()) return;
        if (TryConsumeJump()) return;

        if (Mathf.Abs(moveInput) > 0.01f) { ChangeState(PlayerState.Run); return; }
        if (!IsGrounded) { ChangeState(PlayerState.Fall); return; }
    }

    void UpdateRun(bool attackThisFrame)
    {
        if (TryConsumeAttack(attackThisFrame)) return;
        if (TryEnterClimb()) return;
        if (TryConsumeJump()) return;

        if (Mathf.Abs(moveInput) < 0.01f) { ChangeState(PlayerState.Idle); return; }
        if (!IsGrounded) { ChangeState(PlayerState.Fall); return; }
    }

    void UpdateJump(bool attackThisFrame)
    {
        if (attackCombo != null && attackCombo.IsActive) return;
        if (TryConsumeAttack(attackThisFrame)) return;

        if (TryConsumeJump()) return;

        if (input != null && !input.JumpHeld && !jumpCutApplied && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * data.jumpCutMultiplier);
            jumpCutApplied = true;
        }

        if (rb.velocity.y <= 0)
            ChangeState(PlayerState.Fall);
    }

    void UpdateFall(bool attackThisFrame)
    {
        if (TryConsumeAttack(attackThisFrame)) return;
        if (TryEnterClimb()) return;

        if (IsGrounded && rb.velocity.y <= 0)
        {
            ChangeState(PlayerState.Idle);
            return;
        }

        if (TryConsumeJump()) return;
    }

    void UpdateAttack(bool attackThisFrame)
    {
        attackCombo?.Tick(attackThisFrame);

        if (attackCombo == null || !attackCombo.IsActive)
            ChangeState(IsGrounded ? PlayerState.Idle : PlayerState.Fall);
    }

    void UpdateHurt()
    {
        if (hurtTimer <= 0)
            ChangeState(IsGrounded ? PlayerState.Idle : PlayerState.Fall);
    }

    void UpdateDead()
    {
        // 死亡状态不做输入响应
    }

    void UpdateClimb(bool attackThisFrame)
    {
        // 1. 跳
        if (TryConsumeJump()) return;

        // 2. 攻击
        if (TryConsumeAttack(attackThisFrame)) return;

        // 3. 离开梯子区域
        if (!nearLadder)
        {
            ChangeState(PlayerState.Fall);
            return;
        }

        // 4. 落地且按下 → 下梯
        if (IsGrounded && input != null && input.MoveVertical < -0.1f)
        {
            ChangeState(PlayerState.Idle);
            return;
        }
    }

    // ------------------------------------------------------------
    // 各状态 FixedUpdate
    // ------------------------------------------------------------

    void FixedIdle() => ApplyFriction();
    void FixedRun() => ApplyMovement();
    void FixedJump() => ApplyAirMovement();
    void FixedFall() => ApplyAirMovement();
    void FixedAttack() => ApplyFriction();
    void FixedHurt() => ApplyFriction();

    void FixedDead()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    void FixedClimb()
    {
        if (input == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float vx = moveInput * data.climbHorizontalSpeed;
        float vy = input.MoveVertical * data.climbSpeed;
        rb.velocity = new Vector2(vx, vy);
    }

    // ------------------------------------------------------------
    // 动作
    // ------------------------------------------------------------

    bool TryConsumeAttack(bool attackThisFrame)
    {
        if (!attackThisFrame) return false;
        if (CurrentState == PlayerState.Attack) return false;

        ChangeState(PlayerState.Attack);
        return true;
    }

    bool TryEnterClimb()
    {
        if (ladderCooldownTimer > 0) return false;
        if (!nearLadder) return false;
        if (input == null) return false;
        if (Mathf.Abs(input.MoveVertical) < 0.1f) return false;

        ChangeState(PlayerState.Climb);
        return true;
    }

    bool TryConsumeJump()
    {
        if (jumpBufferTimer <= 0) return false;

        // 梯子上按跳 → 跳出
        if (CurrentState == PlayerState.Climb)
        {
            JumpOffLadder();
            return true;
        }

        bool groundJump = !groundJumpConsumed && (coyoteTimer > 0 || IsGrounded);
        // 空中跳（二段跳）是围巾槽斗篷灵的能力：没装备就不能空中跳（沿用 PlayerEquipment.HasAirJump）。
        if (!groundJump && (equipment == null || !equipment.HasAirJump || airJumpUsed)) return false;

        if (groundJump) groundJumpConsumed = true;
        else airJumpUsed = true;
        jumpBufferTimer = 0;
        coyoteTimer = 0;
        jumpCutApplied = false;
        IsGrounded = false;

        float force = data.jumpForce * (groundJump ? 1f : (equipment != null ? equipment.AirJumpForceMultiplier : 1f));
        rb.velocity = new Vector2(rb.velocity.x, force);
        // 空中再按跳时重播起跳动画，否则会停在上一段动作的末帧。
        if (!groundJump && CurrentState == PlayerState.Jump) PlayJumpAnimationFromStart();
        ChangeState(PlayerState.Jump);
        return true;
    }

    /// <summary>二段跳时重播起跳动画，避免停留在上一段动作的末帧。</summary>
    void PlayJumpAnimationFromStart()
    {
        if (animator == null) return;
        animator.Play("Jump", 0, 0f);
    }

    void JumpOffLadder()
    {
        // 梯子跳算本轮的基础跳跃，清掉残留土狼时间。
        // 斗篷仍可追加一次空中跳；不能额外再执行一次地面跳。
        groundJumpConsumed = true;
        airJumpUsed = false;
        coyoteTimer = 0;
        jumpBufferTimer = 0;
        jumpCutApplied = false;
        IsGrounded = false;
        ladderCooldownTimer = 0.3f;   // 防止跳出后立刻被吸回梯子

        rb.gravityScale = data.gravityScale;

        float vx = rb.velocity.x;
        if (input != null && Mathf.Abs(moveInput) > 0.1f)
            vx = moveInput * data.moveSpeed;

        rb.velocity = new Vector2(vx, data.jumpForce);
        ChangeState(PlayerState.Jump);
    }

    void ApplyMovement()
    {
        float target = moveInput * data.moveSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? data.acceleration : data.deceleration;
        float newX = Mathf.MoveTowards(rb.velocity.x, target, rate * Time.fixedDeltaTime);
        rb.velocity = new Vector2(newX, rb.velocity.y);

        if (Mathf.Abs(moveInput) > 0.01f)
            SetFacing((int)Mathf.Sign(moveInput));
    }

    void ApplyAirMovement()
    {
        float target = moveInput * data.moveSpeed;
        float newX = Mathf.MoveTowards(rb.velocity.x, target, data.acceleration * Time.fixedDeltaTime);

        float gravityMul = rb.velocity.y < 0 ? data.fallGravityMultiplier : 1f;
        float newY = rb.velocity.y - Physics2D.gravity.y * (gravityMul - 1f) * Time.fixedDeltaTime;
        newY = Mathf.Max(newY, -data.maxFallSpeed);

        rb.velocity = new Vector2(newX, newY);

        if (Mathf.Abs(moveInput) > 0.01f)
            SetFacing((int)Mathf.Sign(moveInput));
    }

    void ApplyFriction()
    {
        float newX = Mathf.MoveTowards(rb.velocity.x, 0, data.deceleration * Time.fixedDeltaTime);
        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    void SetFacing(int dir)
    {
        if (dir == 0) return;
        FacingDirection = dir;
        transform.localScale = new Vector3(dir, 1, 1);
    }

    /// <summary>取 Animator 上某个剪辑的长度（找不到返回 0）。</summary>
    float ClipLength(string clipName)
    {
        if (animator == null) return 0f;
        var controller = animator.runtimeAnimatorController;
        if (controller == null) return 0f;
        foreach (var clip in controller.animationClips)
            if (clip != null && clip.name == clipName) return clip.length;
        return 0f;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetInteger("State", (int)CurrentState);
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
        animator.SetBool("IsGrounded", IsGrounded);
    }

    // ------------------------------------------------------------
    // 事件
    // ------------------------------------------------------------

    /// <summary>掉落返回安全点，清理攻击和跳跃缓存，保留生命与装备。</summary>
    public void ResetAfterFall(Vector3 safePosition)
    {
        if (attackCombo != null) attackCombo.ForceCancel();
        rb.position = safePosition;
        transform.position = safePosition;
        rb.velocity = Vector2.zero;
        rb.gravityScale = data.gravityScale;
        currentLadder = null;
        nearLadder = false;
        groundJumpConsumed = false;
        airJumpUsed = false;
        jumpCutApplied = false;
        moveInput = 0;
        jumpPressed = attackPressed = false;
        coyoteTimer = jumpBufferTimer = hurtTimer = 0;
        IsGrounded = false;
        CurrentState = PlayerState.Idle;
        Physics2D.SyncTransforms();
    }

    void HandleDamaged()
    {
        if (CurrentState == PlayerState.Dead) return;
        // 每次受击都给一段无敌时间，避免连续判定瞬间清空血量。
        health.ApplyInvincibility(data.invincibleTime);
        ChangeState(PlayerState.Hurt);
    }

    void HandleDeath()
    {
        ChangeState(PlayerState.Dead);
    }
}