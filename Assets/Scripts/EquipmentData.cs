using UnityEngine;

[CreateAssetMenu(menuName = "Game/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("长剑：在每段基础攻击上追加")]
    [Min(0), Tooltip("伤害加值，不修改 CharacterData 中的原始攻击数据。")]
    public int swordDamageBonus = 2;
    [Min(0), Tooltip("攻击框中心沿玩家局部正 X 方向前移，朝向由父节点统一翻转。")]
    public float swordReachBonus = 0.5f;
    [Min(0), Tooltip("攻击框水平宽度加值，高度不变。")]
    public float swordWidthBonus = 0.5f;

    [Header("斗篷：增加一次空中跳跃")]
    [Min(0.01f), Tooltip("空中跳跃速度相对 CharacterData.jumpForce 的倍率。")]
    public float airJumpForceMultiplier = 1f;

    [Header("护甲")]
    [Min(0), Tooltip("每次正伤害的固定减伤值；减伤后至少承受 1 点伤害。")]
    public int armorDamageReduction = 1;
}
