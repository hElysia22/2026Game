using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EnemyAlertTests
{
    private GameObject enemyObject, target, obstacle;
    private EnemyAI enemy;
    private EnemyData data;
    private Rigidbody2D body;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private void Set(string name, object value) => typeof(EnemyAI).GetField(name, Private).SetValue(enemy, value);
    private object Call(string name, params object[] args) => typeof(EnemyAI).GetMethod(name, Private).Invoke(enemy, args);

    [SetUp]
    public void Setup()
    {
        enemyObject = new GameObject("AlertTestEnemy");
        enemyObject.SetActive(false);
        enemyObject.transform.position = new Vector3(1000, 1000, 0);
        enemy = enemyObject.AddComponent<EnemyAI>();
        data = ScriptableObject.CreateInstance<EnemyData>();
        enemy.data = data;
        Call("Awake");
        body = enemyObject.GetComponent<Rigidbody2D>();
        target = new GameObject("AlertTestTarget");
        target.transform.position = enemyObject.transform.position + Vector3.right * 3;
        Set("player", target.transform);
        enemyObject.SetActive(true);
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(obstacle);
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(data);
        Physics2D.SyncTransforms();
    }

    [Test]
    public void DetectionEntersAlertBeforeAttack()
    {
        Call("UpdatePatrol");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Alert));
        Call("FixedAlert");
        Assert.That(body.velocity.x, Is.EqualTo(data.alertMoveSpeed));
    }

    [TestCase(1f, -1)]
    [TestCase(1.8f, 0)]
    [TestCase(3f, 1)]
    public void CoolingEnemyRetreatsStopsOrApproaches(float distance, int movement)
    {
        target.transform.position = enemyObject.transform.position + Vector3.right * distance;
        Call("EnterAlert");
        Call("FixedAlert");
        Assert.That(body.velocity.x, Is.EqualTo(data.alertMoveSpeed * movement).Within(0.001));
        Assert.That(enemyObject.transform.localScale.x, Is.EqualTo(1), "Retreat still faces the player.");
    }

    [Test]
    public void ReadyEnemyClosesGapThenAttacksAndReturnsToAlert()
    {
        Call("EnterAlert");
        Set("attackCooldownTimer", 0f);
        Call("UpdateAlert");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Alert));
        Call("FixedAlert");
        Assert.That(body.velocity.x, Is.GreaterThan(0));
        target.transform.position = enemyObject.transform.position + Vector3.right;
        Call("UpdateAlert");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Attack));
        Set("attackStateTimer", 0f);
        Call("UpdateAttack");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Alert));
        Call("FixedAlert");
        Assert.That(body.velocity.x, Is.LessThan(0));
    }

    [Test]
    public void LostTargetWaitsThenReturnsToPatrol()
    {
        Call("EnterAlert");
        target.transform.position += Vector3.right * 30;
        data.loseTargetDelay = 10;
        Call("UpdateAlert");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Alert));
        Call("FixedAlert");
        Assert.That(body.velocity.x, Is.Zero);
        Set("lostTargetTimer", 10f);
        Call("UpdateAlert");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Patrol));
    }

    [Test]
    public void PlayerCrossingBehindIsStillTrackedAndFaced()
    {
        Call("EnterAlert");
        target.transform.position = enemyObject.transform.position + Vector3.left * 3;
        Call("UpdateAlert");
        Call("FixedAlert");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Alert));
        Assert.That(body.velocity.x, Is.LessThan(0));
        Assert.That(enemyObject.transform.localScale.x, Is.EqualTo(-1));
    }

    [Test]
    public void RetreatWallProbeChecksBehindRatherThanFacingSide()
    {
        enemy.wallCheck = new GameObject("WallProbe").transform;
        enemy.wallCheck.SetParent(enemyObject.transform, false);
        enemy.wallCheck.localPosition = Vector3.right * 0.5f;
        enemy.wallLayer = 1 << 30;
        obstacle = new GameObject("RearWall");
        obstacle.layer = 30;
        obstacle.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 2);
        obstacle.transform.position = enemyObject.transform.position + Vector3.left * 0.5f;
        Physics2D.SyncTransforms();
        Assert.That(Call("MovementBlocked", -1), Is.EqualTo(true));
        Assert.That(Call("MovementBlocked", 1), Is.EqualTo(false));
        target.transform.position = enemyObject.transform.position + Vector3.right;
        Call("EnterAlert");
        Call("FixedAlert");
        Assert.That(body.velocity.x, Is.Zero);
    }

    [Test]
    public void RetreatStopsAtCliff()
    {
        enemy.groundCheck = new GameObject("GroundProbe").transform;
        enemy.groundCheck.SetParent(enemyObject.transform, false);
        enemy.groundCheck.localPosition = Vector3.down * 0.5f;
        enemy.groundLayer = 1 << 30;
        obstacle = new GameObject("GroundOnRightOnly");
        obstacle.layer = 30;
        obstacle.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.2f);
        obstacle.transform.position = enemyObject.transform.position + new Vector3(0.5f, -0.5f, 0);
        Physics2D.SyncTransforms();
        Assert.That(Call("MovementBlocked", -1), Is.EqualTo(true));
        Assert.That(Call("MovementBlocked", 1), Is.EqualTo(false));
    }

    [Test]
    public void HurtRecoversIntoAlertAndExistingAnimationStateIdsStayStable()
    {
        Call("EnterHurt");
        Set("hurtTimer", 0f);
        Call("UpdateHurt");
        Assert.That(enemy.CurrentState, Is.EqualTo(EnemyAI.EnemyState.Alert));
        Assert.That((int)EnemyAI.EnemyState.Attack, Is.EqualTo(1));
        Assert.That((int)EnemyAI.EnemyState.Hurt, Is.EqualTo(2));
        Assert.That((int)EnemyAI.EnemyState.Dead, Is.EqualTo(3));
    }
}
