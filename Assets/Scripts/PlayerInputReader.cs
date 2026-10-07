using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerControls controls;

    public float MoveInput { get; private set; }
    public bool JumpHeld { get; private set; }
    public float MoveVertical { get; private set; }

    private bool jumpPressed;
    private bool attackPressed;
    private bool pausePressed;
    private bool equipmentPressed;
    public bool IsGameplayInputBlocked { get; private set; }

    void Awake()
    {
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        // 域重载（Play 中改脚本 / 重新编译）会清空非序列化字段，这里兜底重建，避免输入整体失效。
        if (controls == null) controls = new PlayerControls();
        controls.Player.Enable();

        controls.Player.Jump.performed += OnJump;
        controls.Player.Attack.performed += OnAttack;
        controls.Player.Pause.performed += OnPause;
        controls.Player.Equipment.performed += OnEquipment;
    }

    void OnDisable()
    {
        if (controls == null) return;

        controls.Player.Jump.performed -= OnJump;
        controls.Player.Attack.performed -= OnAttack;
        controls.Player.Pause.performed -= OnPause;
        controls.Player.Equipment.performed -= OnEquipment;
        equipmentPressed = false;

        controls.Player.Disable();
    }

    void Update()
    {
        if (controls == null) return;

        if (IsGameplayInputBlocked)
        {
            MoveInput = 0;
            MoveVertical = 0;
            JumpHeld = false;
            return;
        }
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        MoveInput = move.x;
        MoveVertical = move.y;
        JumpHeld = controls.Player.Jump.IsPressed();
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (IsGameplayInputBlocked) return;
        Debug.Log("Jump performed");
        jumpPressed = true;
    }
    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (IsGameplayInputBlocked) return;
        Debug.Log("Attack");
        attackPressed = true; 
    }
    void OnPause(InputAction.CallbackContext ctx)
    {
        Debug.Log("Pause performed");
        pausePressed = true;
    }

    void OnEquipment(InputAction.CallbackContext ctx)
    {
        equipmentPressed = true;
    }

    public void SetGameplayInputBlocked(bool blocked)
    {
        IsGameplayInputBlocked = blocked;
        jumpPressed = false;
        attackPressed = false;
        MoveInput = 0;
        MoveVertical = 0;
        JumpHeld = false;
    }

    public bool ConsumeEquipment() { bool v = equipmentPressed; equipmentPressed = false; return v; }
    public bool ConsumeJump() { bool v = jumpPressed; jumpPressed = false; return v; }
    public bool ConsumeAttack() { bool v = attackPressed; attackPressed = false; return v; }
    public bool ConsumePause() { bool v = pausePressed; pausePressed = false; return v; }
}