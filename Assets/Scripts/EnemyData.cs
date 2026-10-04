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