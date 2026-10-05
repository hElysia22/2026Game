using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EquipmentTests
{
    private GameObject actor, floor;
    private PlayerController player;
    private PlayerEquipment equipment;
    private CharacterData character;
    private EquipmentData config;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private void Set(string name, object value) => typeof(PlayerController).GetField(name, Private).SetValue(player, value);
    private bool Jump()
    {
        Set("jumpBufferTimer", 0.1f);
        return (bool)Call(player, "TryConsumeJump");
    }

    [SetUp]
    public void Setup()
    {
        actor = new GameObject("EquipmentTestPlayer");
        actor.SetActive(false);
        actor.transform.position = new Vector3(1000, 1000, 0);
        equipment = actor.AddComponent<PlayerEquipment>();
        config = ScriptableObject.CreateInstance<EquipmentData>();
        equipment.data = config;
        player = actor.AddComponent<PlayerController>();
        character = ScriptableObject.CreateInstance<CharacterData>();
        character.combo = new[] { new AttackStep { damage = 2, duration = 10, hitboxOffset = Vector2.right, hitboxSize = Vector2.one } };
        player.data = character;
        Call(player, "Awake");
        var health = actor.GetComponent<Health>();
        health.maxHP = 20;
        Call(health, "Awake");
        actor.SetActive(true);
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(floor);
        Object.DestroyImmediate(actor);
        Object.DestroyImmediate(character);
        Object.DestroyImmediate(config);
        Physics2D.SyncTransforms();
    }

    [Test]
    public void SpiritCapacityDuplicateSlotsAndRedistributionAreEnforced()
    {
        Assert.That(equipment.TryEquipSpirit(EquipmentSlot.Weapon), Is.False);
        Assert.That(equipment.CollectSpirits(-1), Is.Zero);
        Assert.That(equipment.CollectSpirits(10), Is.EqualTo(2));
        Assert.That(equipment.CollectSpirits(), Is.Zero);
        Assert.That(equipment.TryEquipSpirit(EquipmentSlot.Weapon), Is.True);
        Assert.That(equipment.TryEquipSpirit(EquipmentSlot.Weapon), Is.False);
        Assert.That(equipment.TryEquipSpirit(EquipmentSlot.Scarf), Is.True);
        Assert.That(equipment.TryEquipSpirit(EquipmentSlot.Clothes), Is.False);
        Assert.That(equipment.TryUnequipSpirit(EquipmentSlot.Weapon), Is.True);
        Assert.That(equipment.TryUnequipSpirit(EquipmentSlot.Weapon), Is.False);
        Assert.That(equipment.TryEquipSpirit(EquipmentSlot.Clothes), Is.True);
        Assert.That(equipment.AvailableSpirits, Is.Zero);
        Assert.That(equipment.TryEquipSpirit((EquipmentSlot)99), Is.False);
        Assert.That(equipment.UsedSpirits, Is.EqualTo(2));
    }

    [Test]
    public void EventsOnlyFireForSuccessfulChanges()
    {
        int changes = 0;
        equipment.OnEquipmentChanged += () => changes++;
        equipment.CollectSpirits(2);
        equipment.CollectSpirits(2);
        equipment.TryEquipSpirit(EquipmentSlot.Weapon);
        equipment.TryEquipSpirit(EquipmentSlot.Weapon);
        equipment.TryUnequipSpirit(EquipmentSlot.Weapon);
        Assert.That(changes, Is.EqualTo(3));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void SwordBuffUsesLocalOffsetOnceAndUnequipRestoresBaseAttack(int facing)
    {
        Call(player, "SetFacing", facing);
        equipment.CollectSpirits();
        equipment.TryEquipSpirit(EquipmentSlot.Weapon);
        var origin = new GameObject("Origin").transform;
        origin.SetParent(actor.transform, false);
        var combo = actor.AddComponent<PlayerAttackCombo>();
        combo.player = player;
        combo.hitboxOrigin = origin;
        combo.Begin();
        combo.AE_OpenHitbox();
        var hit = origin.GetComponentInChildren<Hitbox>(true);
        Assert.That(hit.damage, Is.EqualTo(4));
        Assert.That(hit.size.x, Is.EqualTo(1.5f));
        Assert.That(hit.transform.position.x, Is.EqualTo(1000 + facing * 1.5f).Within(0.001));
        Assert.That(character.combo[0].damage, Is.EqualTo(2));
        Assert.That(character.combo[0].hitboxOffset, Is.EqualTo(Vector2.right));
        equipment.TryUnequipSpirit(EquipmentSlot.Weapon);
        combo.AE_OpenHitbox();
        hit = origin.GetComponentInChildren<Hitbox>(true);
        Assert.That(hit.damage, Is.EqualTo(2));
        Assert.That(hit.size.x, Is.EqualTo(1));
        Assert.That(hit.transform.position.x, Is.EqualTo(1000 + facing).Within(0.001));
    }

    [Test]
    public void PrototypeWithoutAnimatorOpensAndClosesHitboxFromAttackTiming()
    {
        var origin = new GameObject("Origin").transform;
        origin.SetParent(actor.transform, false);
        var combo = actor.AddComponent<PlayerAttackCombo>();
        combo.player = player;
        combo.hitboxOrigin = origin;
        combo.Begin();
        combo.Tick(false);
        Assert.That(origin.GetComponentInChildren<Hitbox>(true), Is.Not.Null);
        combo.ForceCancel();
        Assert.That(origin.GetComponentInChildren<Hitbox>(true), Is.Null);
    }

    [Test]
    public void ArmorReducesRealHealthDamageWithOnePointMinimumAndNoHealingFromNegativeDamage()
    {
        equipment.CollectSpirits();
        equipment.TryEquipSpirit(EquipmentSlot.Clothes);
        config.armorDamageReduction = 3;
        var health = actor.GetComponent<Health>();
        Assert.That(health.TryTakeDamage(5, 0, Vector2.zero), Is.True);
        Assert.That(health.CurrentHP, Is.EqualTo(18));
        health.TryTakeDamage(1, 0, Vector2.zero);
        Assert.That(health.CurrentHP, Is.EqualTo(17));
        Assert.That(health.TryTakeDamage(-2, 0, Vector2.zero), Is.False);
        equipment.TryUnequipSpirit(EquipmentSlot.Clothes);
        health.TryTakeDamage(5, 0, Vector2.zero);
        Assert.That(health.CurrentHP, Is.EqualTo(12));
    }

    [Test]
    public void CloakGivesOneAirJumpAndReequipDoesNotRefreshIt()
    {
        Set("coyoteTimer", 0.1f);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.False, "No cloak means no air jump.");
        equipment.CollectSpirits();
        equipment.TryEquipSpirit(EquipmentSlot.Scarf);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.False);
        equipment.TryUnequipSpirit(EquipmentSlot.Scarf);
        equipment.TryEquipSpirit(EquipmentSlot.Scarf);
        Assert.That(Jump(), Is.False, "Reequipping midair must not grant unlimited jumps.");
    }

    [Test]
    public void WalkingOffLedgeGrantsOnlyOneAirJumpAfterCoyoteExpires()
    {
        equipment.CollectSpirits();
        equipment.TryEquipSpirit(EquipmentSlot.Scarf);
        Set("coyoteTimer", 0f);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.False);
    }

    [Test]
    public void LandingResetsAirJumpButAscendingGroundProbeDoesNot()
    {
        equipment.CollectSpirits();
        equipment.TryEquipSpirit(EquipmentSlot.Scarf);
        Set("coyoteTimer", 0.1f);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.True);
        player.groundCheck = actor.transform;
        player.groundLayer = 1 << 30;
        floor = new GameObject("LandingGround");
        floor.layer = 30;
        floor.AddComponent<BoxCollider2D>().size = Vector2.one;
        floor.transform.position = actor.transform.position;
        Physics2D.SyncTransforms();
        Call(player, "CheckGround");
        Assert.That(player.IsGrounded, Is.False);
        Assert.That(Jump(), Is.False);
        actor.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        Call(player, "CheckGround");
        Assert.That(player.IsGrounded, Is.True);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LadderJumpConsumesBaseJumpAndAllowsOnlyCloakAirJump(bool cloak)
    {
        if (cloak)
        {
            equipment.CollectSpirits();
            equipment.TryEquipSpirit(EquipmentSlot.Scarf);
        }
        // 地面附近进入梯子时仍可能留有土狼时间。
        Set("coyoteTimer", 0.1f);
        player.ChangeState(PlayerState.Climb);
        Assert.That(Jump(), Is.True, "Jump off ladder is always available.");
        Assert.That(actor.GetComponent<Rigidbody2D>().gravityScale, Is.EqualTo(character.gravityScale));
        Assert.That(Jump(), Is.EqualTo(cloak), "Only the cloak grants the next jump.");
        Assert.That(Jump(), Is.False, "No third jump from stale coyote time.");
    }

    [Test]
    public void LadderSupportsANewJumpWithExactlyOneCloakAirJump()
    {
        equipment.CollectSpirits();
        equipment.TryEquipSpirit(EquipmentSlot.Scarf);
        Set("coyoteTimer", 0.1f);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.False);
        player.ChangeState(PlayerState.Climb);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.True);
        Assert.That(Jump(), Is.False);
    }

    [TestCase(-5, 0)]
    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(5, 2)]
    public void StartingSpiritCountIsClampedAndInitialized(int configured, int expected)
    {
        typeof(PlayerEquipment).GetField("startingSpirits", Private).SetValue(equipment, configured);
        Call(equipment, "Awake");
        Assert.That(equipment.TotalSpirits, Is.EqualTo(expected));
        Assert.That(equipment.AvailableSpirits, Is.EqualTo(expected));
        Assert.That(equipment.UsedSpirits, Is.Zero);
    }

    [Test]
    public void RepeatedSpiritReallocationDoesNotStackBonuses()
    {
        equipment.CollectSpirits(2);
        for (int i = 0; i < 10; i++)
        {
            equipment.TryEquipSpirit(EquipmentSlot.Weapon);
            equipment.TryEquipSpirit(EquipmentSlot.Clothes);
            Assert.That(equipment.DamageBonus, Is.EqualTo(config.swordDamageBonus));
            Assert.That(equipment.ReachBonus, Is.EqualTo(config.swordReachBonus));
            Assert.That(equipment.WidthBonus, Is.EqualTo(config.swordWidthBonus));
            Assert.That(equipment.DamageReduction, Is.EqualTo(config.armorDamageReduction));
            equipment.TryUnequipSpirit(EquipmentSlot.Weapon);
            equipment.TryUnequipSpirit(EquipmentSlot.Clothes);
            Assert.That(equipment.DamageBonus, Is.Zero);
            Assert.That(equipment.ReachBonus, Is.Zero);
            Assert.That(equipment.WidthBonus, Is.Zero);
            Assert.That(equipment.DamageReduction, Is.Zero);
            Assert.That(equipment.TotalSpirits, Is.EqualTo(2));
            Assert.That(equipment.AvailableSpirits, Is.EqualTo(2));
        }
    }
}
