using UnityEngine;

/// <summary>进入战斗区后关门、锁镜头并唤醒占位 Boss；死亡与重开仍由 GameManager 负责。</summary>
[DisallowMultipleComponent]
public class BossArena : MonoBehaviour
{
    public PlayerController player;
    public BossAI boss;
    public BoxCollider2D entryZone;
    public GameObject barriers;
    public BossArenaCamera arenaCamera;
    public GameManager manager;
    public Rect combatBounds = new Rect(52.35f, 0, 23.3f, 11.5f);
    public bool IsEngaged { get; private set; }
    public bool IsCompleted { get; private set; }
    Health bossHealth, playerHealth;
    Rigidbody2D playerBody;
    Collider2D playerCollider;

    void Awake()
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            playerBody = player.GetComponent<Rigidbody2D>();
            playerCollider = player.GetComponent<Collider2D>();
        }
        if (boss != null) bossHealth = boss.GetComponent<Health>();
        if (barriers != null) barriers.SetActive(false);
        if (boss != null) boss.gameObject.SetActive(false);
    }

    void OnEnable() { if (bossHealth != null) bossHealth.OnDeath += CompleteFight; }
    void OnDisable()
    {
        if (bossHealth != null) bossHealth.OnDeath -= CompleteFight;
        if (arenaCamera != null) arenaCamera.SetLocked(false);
    }

    void Update()
    {
        if (IsEngaged || IsCompleted || player == null || playerHealth.IsDead || Time.timeScale <= 0) return;
        if (entryZone != null && entryZone.bounds.Contains(player.transform.position)) BeginFight();
    }

    public void BeginFight()
    {
        if (IsEngaged || IsCompleted || boss == null || playerHealth == null || playerHealth.IsDead) return;
        IsEngaged = true;
        if (barriers != null) barriers.SetActive(true);
        if (arenaCamera != null) arenaCamera.SetLocked(true);
        boss.gameObject.SetActive(true);
    }

    void FixedUpdate()
    {
        if (!IsEngaged || playerBody == null || playerHealth.IsDead) return;
        // 除实体墙外再限制身体范围，防止大击退或高速移动越过门。
        float margin = playerCollider != null ? playerCollider.bounds.extents.x : 0.25f;
        Vector2 pos = playerBody.position;
        float x = Mathf.Clamp(pos.x, combatBounds.xMin + margin, combatBounds.xMax - margin);
        if (x != pos.x)
        {
            playerBody.position = new Vector2(x, pos.y);
            var velocity = playerBody.velocity; velocity.x = 0; playerBody.velocity = velocity;
        }
    }

    public void CompleteFight()
    {
        if (!IsEngaged || IsCompleted) return;
        IsEngaged = false;
        IsCompleted = true;
        if (barriers != null) barriers.SetActive(false);
        if (arenaCamera != null) arenaCamera.SetLocked(false);
        if (manager != null) manager.CompleteLevel();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.7f, 0, 1);
        Gizmos.DrawWireCube(new Vector3(combatBounds.center.x, combatBounds.center.y, 0), new Vector3(combatBounds.width, combatBounds.height, 0));
    }
}
