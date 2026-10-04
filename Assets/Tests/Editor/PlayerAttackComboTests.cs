using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlayerAttackComboTests
{
    private GameObject playerObject;
    private GameObject targetObject;
    private CharacterData data;
    private PlayerController player;
    private PlayerAttackCombo combo;
    private Transform origin;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("AttackComboTestPlayer");
        playerObject.SetActive(false);
        playerObject.transform.position = new Vector3(100f, 200f, 0f);

        player = playerObject.AddComponent<PlayerController>();
        data = ScriptableObject.CreateInstance<CharacterData>();
        data.combo = new[] { new AttackStep() };
        player.data = data;

        origin = new GameObject("HitboxOrigin").transform;
        origin.SetParent(playerObject.transform, false);

        combo = playerObject.AddComponent<PlayerAttackCombo>();
        combo.player = player;
        combo.hitboxOrigin = origin;
        combo.enemyLayer = 1 << 6;
    }

    [TearDown]
    public void TearDown()
    {
        if (targetObject != null) Object.DestroyImmediate(targetObject);
        if (playerObject != null) Object.DestroyImmediate(playerObject);
        if (data != null) Object.DestroyImmediate(data);
        Physics2D.SyncTransforms();
    }

    [TestCase(1, 1f, 0f)]
    [TestCase(-1, 1f, 0f)]
    [TestCase(1, 2f, 0.4f)]
    [TestCase(-1, 2f, 0.4f)]
    [TestCase(1, -0.75f, -0.2f)]
    [TestCase(-1, -0.75f, -0.2f)]
    [TestCase(1, 0f, 0.5f)]
    [TestCase(-1, 0f, 0.5f)]
    public void OpenHitbox_MirrorsHorizontalOffsetExactlyOnce(int facing, float offsetX, float offsetY)
    {
        SetFacing(facing);
        data.combo[0].hitboxOffset = new Vector2(offsetX, offsetY);

        combo.Begin();
        combo.AE_OpenHitbox();

        Transform hitbox = origin.Find("PlayerHitbox");
        Assert.That(hitbox, Is.Not.Null);
        Vector3 expected = origin.position + new Vector3(facing * offsetX, offsetY, 0f);
        Assert.That(hitbox.position.x, Is.EqualTo(expected.x).Within(0.0001f),
            "The facing parent must mirror the configured local X offset only once.");
        Assert.That(hitbox.position.y, Is.EqualTo(expected.y).Within(0.0001f));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void OpenHitbox_FollowsOffsetOriginAndParentMovement(int facing)
    {
        SetFacing(facing);
        origin.localPosition = new Vector3(0.5f, 0.25f, 0f);
        data.combo[0].hitboxOffset = new Vector2(1.5f, 0.4f);

        combo.Begin();
        combo.AE_OpenHitbox();
        playerObject.transform.position += new Vector3(3f, -2f, 0f);

        Vector3 expected = playerObject.transform.position + new Vector3(facing * 2f, 0.65f, 0f);
        Transform hitbox = origin.Find("PlayerHitbox");
        Assert.That(Vector3.Distance(hitbox.position, expected), Is.LessThan(0.0001f),
            "Both the origin and attack offset must follow the player hierarchy.");
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void OpenHitbox_DetectionContainsFrontTargetAndExcludesRearTarget(int facing)
    {
        SetFacing(facing);
        data.combo[0].hitboxOffset = new Vector2(1.5f, 0f);
        data.combo[0].hitboxSize = new Vector2(1f, 1f);
        combo.Begin();
        combo.AE_OpenHitbox();
        Hitbox hitbox = origin.Find("PlayerHitbox").GetComponent<Hitbox>();

        targetObject = new GameObject("AttackComboTestTarget");
        targetObject.layer = 6;
        BoxCollider2D targetCollider = targetObject.AddComponent<BoxCollider2D>();
        targetCollider.size = new Vector2(0.1f, 0.1f);

        targetObject.transform.position = playerObject.transform.position + new Vector3(facing * 1.5f, 0f, 0f);
        Physics2D.SyncTransforms();
        CollectionAssert.Contains(Physics2D.OverlapBoxAll(hitbox.transform.position, hitbox.size, 0f, hitbox.targetLayer),
            targetCollider, "A positive offset must hit the target in front of the player.");

        targetObject.transform.position = playerObject.transform.position - new Vector3(facing * 1.5f, 0f, 0f);
        Physics2D.SyncTransforms();
        CollectionAssert.DoesNotContain(Physics2D.OverlapBoxAll(hitbox.transform.position, hitbox.size, 0f, hitbox.targetLayer),
            targetCollider, "The same attack must not be mirrored behind the player.");
    }

    private void SetFacing(int facing)
    {
        MethodInfo setFacing = typeof(PlayerController).GetMethod("SetFacing", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(setFacing, Is.Not.Null);
        setFacing.Invoke(player, new object[] { facing });
    }
}
