using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Base")]
    public int maxHP = 3;
    public float moveSpeed = 3f;

    [Header("Patrol")]
    public float patrolDistance = 4f;

    [Header("Detection (Fan Shape)")]
    public float detectRange = 6f;      // 扇形半径
    public float detectAngle = 90f;     // 扇形总角度
    public bool ignoreY = true;         // 是否忽略Y轴

    [Header("Alert / 警戒")]
    [Min(0), Tooltip("警戒时接近和后退的速度，通常小于巡逻速度。")]
    public float alertMoveSpeed = 1f;
    [Min(0), Tooltip("攻击冷却期间与玩家保持的水平距离。")]
    public float alertDistance = 1.8f;
    [Min(0), Tooltip("保持距离的容差，避免反复前后抖动。")]
    public float alertDistanceTolerance = 0.2f;
    [Min(0), Tooltip("脱离此范围后开始丢失计时；实际不会小于发现范围。")]
    public float loseTargetRange = 6f;
    [Min(0), Tooltip("玩家持续离开追踪范围多久后恢复巡逻。")]
    public float loseTargetDelay = 1f;

    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.5f;
    public float attackWindup = 0.3f;
    public float attackRecovery = 0.3f;
    public int attackDamage = 1;
    public float knockback = 3f;

    [Header("Hurt")]
    public float hurtDuration = 0.2f;
}