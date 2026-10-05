using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 60f;
    public float deceleration = 80f;
    public float jumpForce = 14f;
    public float gravityScale = 3f;
    public float fallGravityMultiplier = 1.8f;
    public float maxFallSpeed = 20f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public float jumpCutMultiplier = 0.5f;

    [Header("Combat")]
    public int maxHP = 100;
    public float invincibleTime = 0.8f;
    public float hitStopTime = 0.08f;
    public Vector2 hurtKnockback = new Vector2(5f, 5f);

    [Header("Combo")]
    public AttackStep[] combo;

    [Header("Ladder")]
    public float climbSpeed = 4f;
    public float climbHorizontalSpeed = 2f;   // 梯子上左右移动速度，0 = 不能左右
}