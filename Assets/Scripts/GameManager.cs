using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public PlayerInputReader input;
    public GameObject player;
    public GameObject deathPanel;
    public GameObject pausePanel;
    [Min(0), Tooltip("玩家死亡后延迟多久弹出失败界面，留出死亡动画时间（Char_Death 约 1.4s）。")]
    public float deathUiDelay = 1.6f;
    public string startScene = "StartScene";
    public bool IsPaused { get; private set; }
    public bool IsCompleted { get; private set; }
    public event Action<bool> OnPauseChanged;
    float pauseResumeTimeScale = 1;
    bool previousInputBlock, previousCursorVisible;
    CursorLockMode previousCursorLock;
    Vector3 currentRespawnPos;
    Health health;
    bool loading;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1;
    }

    void Start()
    {
        if (player == null && input != null) player = input.gameObject;
        health = player != null ? player.GetComponent<Health>() : null;
        if (health != null) health.OnDeath += OnPlayerDeath;
        if (player != null) currentRespawnPos = player.transform.position;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (input == null || !input.ConsumePause() || loading) return;
        var menu = input.GetComponent<EquipmentMenuController>();
        if (menu != null && menu.IsOpen) menu.Close();
        else if (!IsCompleted && (health == null || !health.IsDead)) TogglePause();
    }

    public void SetCheckpoint(string id, Vector3 pos) => currentRespawnPos = pos;

    public void OnPlayerDeath()
    {
        if (loading) return;
        // 先让死亡动画播完，再弹失败界面（界面复用暂停面板 + “挑战失败” 结果字样）。
        if (deathUiDelay <= 0f) { ShowDeathUi(); return; }
        StartCoroutine(DeathUiRoutine());
    }

    IEnumerator DeathUiRoutine()
    {
        float t = 0f;
        while (t < deathUiDelay) { t += Time.unscaledDeltaTime; yield return null; }
        ShowDeathUi();
    }

    void ShowDeathUi()
    {
        if (deathPanel != null) deathPanel.SetActive(true);
        SetPaused(true);
    }

    public void CompleteLevel()
    {
        if (IsCompleted || (health != null && health.IsDead)) return;
        IsCompleted = true;
        SetPaused(true);
    }

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused || (!paused && (IsCompleted || (health != null && health.IsDead)))) return;
        var menu = input != null ? input.GetComponent<EquipmentMenuController>() : null;
        if (paused && menu != null && menu.IsOpen) menu.Close();
        if (paused)
        {
            pauseResumeTimeScale = HitStop.Instance != null && HitStop.Instance.IsActive ? HitStop.Instance.ResumeTimeScale : Time.timeScale;
            if (pauseResumeTimeScale <= 0) pauseResumeTimeScale = 1;
            previousInputBlock = input != null && input.IsGameplayInputBlocked;
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            if (input != null) input.SetGameplayInputBlocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (input != null) input.SetGameplayInputBlocked(previousInputBlock);
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLock;
        }
        IsPaused = paused;
        Time.timeScale = paused ? 0 : pauseResumeTimeScale;
        if (pausePanel != null) pausePanel.SetActive(paused);
        OnPauseChanged?.Invoke(paused);
    }

    void TogglePause() => SetPaused(!IsPaused);

    public void Respawn()
    {
        if (player == null) return;
        var actor = player.GetComponent<PlayerController>();
        var hp = player.GetComponent<Health>();
        if (hp != null) hp.ResetHealth(actor != null ? actor.data.maxHP : hp.maxHP);
        if (actor != null) actor.ResetAfterFall(currentRespawnPos);
        if (deathPanel != null) deathPanel.SetActive(false);
        SetPaused(false);
    }

    void PrepareSceneChange()
    {
        loading = true;
        var menu = input != null ? input.GetComponent<EquipmentMenuController>() : null;
        if (menu != null) menu.Close();
        IsPaused = false;
        Time.timeScale = 1;
    }

    public void Restart()
    {
        if (loading) return;
        PrepareSceneChange();
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    public void ReturnToStartMenu()
    {
        if (loading) return;
        PrepareSceneChange();
        SceneManager.LoadSceneAsync(startScene);
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= OnPlayerDeath;
        if (Instance == this) Instance = null;
    }
}
