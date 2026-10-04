using UnityEngine;

public enum PlayerState { Idle, Run, Jump, Fall, Attack, Hurt, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Data")]
    public CharacterData data;

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
        // 1. 读输入
        ReadInput();

        // 2. 本帧攻击输入只消费一次
        bool attackThisFrame = attackPressed;
        attackPressed = false;

        // 3. 检测与计时
        CheckGround();
        UpdateTimers();

        // 4. 按状态分发
        switch (CurrentState)
        {
            case PlayerState.Idle:
                UpdateIdle(attackThisFrame);
                break;
            case PlayerState.Run:
                UpdateRun(attackThisFrame);
                break;
            case PlayerState.Jump:
                UpdateJump(attackThisFrame);
                break;
            case PlayerState.Fall:
                UpdateFall(attackThisFrame);
                break;
            case PlayerState.Attack:
                UpdateAttack(attackThisFrame);
                break;
            case PlayerState.Hurt:
                UpdateHurt();
                break;
            case PlayerState.Dead:
                UpdateDead();
                break;
        }

        // 5. 同步动画参数
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        switch (CurrentState)
        {
            case PlayerState.Idle: FixedIdle(); break;
            case PlayerState.Run: FixedRun(); break;
            case PlayerState.Jump: FixedJump(); break;
            case PlayerState.Fall: FixedFall(); break;
            case PlayerState.Attack: FixedAttack(); break;
            case PlayerState.Hurt: FixedHurt(); break;
            case PlayerState.Dead: FixedDead(); break;
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

    void CheckGround()
    {
        bool wasGrounded = IsGrounded;
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        if (IsGrounded)
            coyoteTimer = data.coyoteTime;
        else if (wasGrounded)
            coyoteTimer = data.coyoteTime;
    }

    void UpdateTimers()
    {
        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
        if (hurtTimer > 0) hurtTimer -= Time.deltaTime;
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
                hurtTimer = 0.2f;
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
        }
    }

    // ------------------------------------------------------------
    // 各状态 Update
    // ------------------------------------------------------------

    void UpdateIdle(bool attackThisFrame)
    {
        if (TryConsumeAttack(attackThisFrame)) return;
        if (TryConsumeJump()) return;

        if (Mathf.Abs(moveInput) > 0.01f) { ChangeState(PlayerState.Run); return; }
        if (!IsGrounded) { ChangeState(PlayerState.Fall); return; }
    }

    void UpdateRun(bool attackThisFrame)
    {
        if (TryConsumeAttack(attackThisFrame)) return;
        if (TryConsumeJump()) return;

        if (Mathf.Abs(moveInput) < 0.01f) { ChangeState(PlayerState.Idle); return; }
        if (!IsGrounded) { ChangeState(PlayerState.Fall); return; }
    }

    void UpdateJump(bool attackThisFrame)
    {
        if (attackCombo != null && attackCombo.IsActive) return;
        if (TryConsumeAttack(attackThisFrame)) return;

        // 松开跳跃键提前下落
        if (!input.JumpHeld && !jumpCutApplied && rb.velocity.y > 0)
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

        if (IsGrounded && rb.velocity.y <= 0)
        {
            ChangeState(PlayerState.Idle);
            return;
        }

        if (coyoteTimer > 0 && TryConsumeJump()) return;
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

    bool TryConsumeJump()
    {
        if (jumpBufferTimer <= 0) return false;
        if (coyoteTimer <= 0 && !IsGrounded) return false;

        jumpBufferTimer = 0;
        coyoteTimer = 0;
        jumpCutApplied = false;

        rb.velocity = new Vector2(rb.velocity.x, data.jumpForce);
        ChangeState(PlayerState.Jump);
        return true;
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

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetInteger("State", (int)CurrentState);
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", IsGrounded);
    }

    // ------------------------------------------------------------
    // 事件
    // ------------------------------------------------------------

    void HandleDamaged()
    {
        if (CurrentState == PlayerState.Dead) return;
        ChangeState(PlayerState.Hurt);
    }

    void HandleDeath()
    {
        ChangeState(PlayerState.Dead);
    }
}