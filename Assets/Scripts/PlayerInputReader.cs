using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerControls controls;

    public float MoveInput { get; private set; }
    public bool JumpHeld { get; private set; }   // ← 新增

    private bool jumpPressed;
    private bool attackPressed;
    private bool pausePressed;

    void Awake()
    {
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Player.Enable();

        controls.Player.Jump.performed += OnJump;
        controls.Player.Attack.performed += OnAttack;
        controls.Player.Pause.performed += OnPause;
    }

    void OnDisable()
    {
        controls.Player.Jump.performed -= OnJump;
        controls.Player.Attack.performed -= OnAttack;
        controls.Player.Pause.performed -= OnPause;

        controls.Player.Disable();
    }

    void Update()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        MoveInput = move.x;
        JumpHeld = controls.Player.Jump.IsPressed();
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        Debug.Log("Jump performed");
        jumpPressed = true;
    }
    void OnAttack(InputAction.CallbackContext ctx)
    {
        Debug.Log("Attack");
        attackPressed = true; 
    }
    void OnPause(InputAction.CallbackContext ctx)
    {
        Debug.Log("Pause performed");
        pausePressed = true;
    }

    public bool ConsumeJump() { bool v = jumpPressed; jumpPressed = false; return v; }
    public bool ConsumeAttack() { bool v = attackPressed; attackPressed = false; return v; }
    public bool ConsumePause() { bool v = pausePressed; pausePressed = false; return v; }
}