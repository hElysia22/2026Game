using UnityEngine;

public class DebugConfigPanel : MonoBehaviour
{
    public PlayerController player;
    public EnemyAI enemy;
    public CharacterData characterData;
    public EnemyData enemyData;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.BackQuote;

    private bool show;

    // 折叠状态
    private bool foldPlayer = true;
    private bool foldCombo = false;
    private bool foldEnemy = false;
    private bool foldCamera = false;
    private bool foldCombat = false;

    private Vector2 scrollPos;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) show = !show;
    }

    void OnGUI()
    {
        if (!show) return;

        float w = 340f;
        float h = Mathf.Min(Screen.height - 20f, 640f);

        GUILayout.BeginArea(new Rect(10, 10, w, h), GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("=== Debug Config ===");
        if (GUILayout.Button("X", GUILayout.Width(24))) show = false;
        GUILayout.EndHorizontal();

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        DrawPlayerSection();
        DrawComboSection();
        DrawEnemySection();
        DrawCameraSection();
        DrawCombatSection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ---------- 折叠头 ----------
    bool Foldout(bool expanded, string title)
    {
        return GUILayout.Toggle(expanded, (expanded ? "▼ " : "▶ ") + title, "Button");
    }

    // ---------- 分类 1：玩家 ----------
    void DrawPlayerSection()
    {
        foldPlayer = Foldout(foldPlayer, "Player");
        if (!foldPlayer) return;

        if (characterData == null)
        {
            GUILayout.Label("  characterData 未赋值");
            return;
        }

        GUILayout.BeginVertical(GUI.skin.box);
        characterData.moveSpeed = Slider("Move Speed", characterData.moveSpeed, 1f, 20f);
        characterData.acceleration = Slider("Accel", characterData.acceleration, 10f, 200f);
        characterData.deceleration = Slider("Decel", characterData.deceleration, 10f, 200f);
        characterData.jumpForce = Slider("Jump Force", characterData.jumpForce, 5f, 25f);
        characterData.gravityScale = Slider("Gravity", characterData.gravityScale, 1f, 8f);
        characterData.fallGravityMultiplier = Slider("Fall Gravity", characterData.fallGravityMultiplier, 1f, 4f);
        characterData.coyoteTime = Slider("Coyote", characterData.coyoteTime, 0f, 0.3f);
        characterData.jumpBufferTime = Slider("Jump Buffer", characterData.jumpBufferTime, 0f, 0.3f);
        characterData.jumpCutMultiplier = Slider("Jump Cut", characterData.jumpCutMultiplier, 0f, 1f);
        GUILayout.EndVertical();
    }

    // ---------- 分类 2：连击 ----------
    void DrawComboSection()
    {
        foldCombo = Foldout(foldCombo, "Combo Hitbox");
        if (!foldCombo) return;

        if (characterData == null || characterData.combo == null || characterData.combo.Length == 0)
        {
            GUILayout.Label("  combo 未配置");
            return;
        }

        for (int i = 0; i < characterData.combo.Length; i++)
        {
            var step = characterData.combo[i];
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Step {i + 1}");

            step.damage = Slider("  Damage", step.damage, 0f, 10f);
            step.knockback = Slider("  Knockback", step.knockback, 0f, 15f);
            step.duration = Slider("  Duration", step.duration, 0.05f, 1f);
            step.comboWindowStart = Slider("  Window Start", step.comboWindowStart, 0f, 1f);
            step.comboWindowEnd = Slider("  Window End", step.comboWindowEnd, 0f, 1f);
            step.hitboxOffset.x = Slider("  Offset X", step.hitboxOffset.x, -2f, 3f);
            step.hitboxOffset.y = Slider("  Offset Y", step.hitboxOffset.y, -1f, 2f);
            step.hitboxSize.x = Slider("  Size X", step.hitboxSize.x, 0.2f, 3f);
            step.hitboxSize.y = Slider("  Size Y", step.hitboxSize.y, 0.2f, 3f);

            GUILayout.EndVertical();
        }
    }

    // ---------- 分类 3：敌人 ----------
    void DrawEnemySection()
    {
        foldEnemy = Foldout(foldEnemy, "Enemy");
        if (!foldEnemy) return;

        if (enemyData == null)
        {
            GUILayout.Label("  enemyData 未赋值");
            return;
        }

        GUILayout.BeginVertical(GUI.skin.box);
        enemyData.moveSpeed = Slider("Move Speed", enemyData.moveSpeed, 0f, 10f);
        enemyData.patrolDistance = Slider("Patrol", enemyData.patrolDistance, 0.5f, 10f);
        enemyData.detectRange = Slider("Detect", enemyData.detectRange, 1f, 15f);
        enemyData.attackRange = Slider("Atk Range", enemyData.attackRange, 0.5f, 3f);
        enemyData.attackCooldown = Slider("Atk CD", enemyData.attackCooldown, 0.2f, 3f);
        enemyData.attackWindup = Slider("Windup", enemyData.attackWindup, 0f, 1f);
        enemyData.attackRecovery = Slider("Recovery", enemyData.attackRecovery, 0f, 1f);
        GUILayout.EndVertical();
    }

    // ---------- 分类 4：相机 ----------
    void DrawCameraSection()
    {
        foldCamera = Foldout(foldCamera, "Camera");
        if (!foldCamera) return;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("  相机参数在 Cinemachine 面板调");
        GUILayout.EndVertical();
    }

    // ---------- 分类 5：战斗 ----------
    void DrawCombatSection()
    {
        foldCombat = Foldout(foldCombat, "Combat");
        if (!foldCombat) return;

        GUILayout.BeginVertical(GUI.skin.box);

        if (player != null)
        {
            GUILayout.Label($"State: {player.CurrentState}");
            GUILayout.Label($"Grounded: {player.IsGrounded}");
            GUILayout.Label($"Facing: {player.FacingDirection}");
        }
        else
        {
            GUILayout.Label("  player 未赋值");
        }

        if (characterData != null)
        {
            characterData.invincibleTime = Slider("Invincible", characterData.invincibleTime, 0f, 2f);
            characterData.hitStopTime = Slider("Hit Stop", characterData.hitStopTime, 0f, 0.3f);
        }

        GUILayout.EndVertical();
    }

    // ---------- 滑块 ----------
    float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120));
        value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
        GUILayout.Label(value.ToString("F2"), GUILayout.Width(48));
        GUILayout.EndHorizontal();
        return value;
    }
}