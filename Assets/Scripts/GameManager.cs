using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerInputReader input;

    public GameObject player;
    public GameObject deathPanel;
    public GameObject pausePanel;

    private string currentCheckpointId;
    private Vector3 currentRespawnPos;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (input.ConsumePause())
            TogglePause();
    }

    public void SetCheckpoint(string id, Vector3 pos)
    {
        currentCheckpointId = id;
        currentRespawnPos = pos;
    }

    public void OnPlayerDeath()
    {
        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Respawn()
    {
        Time.timeScale = 1f;
        if (deathPanel != null) deathPanel.SetActive(false);
        if (player != null && currentRespawnPos != Vector3.zero)
        {
            player.transform.position = currentRespawnPos;
            var health = player.GetComponent<Health>();
            if (health != null)
            {
                // 需要 Health 提供 ResetHP 方法
            }
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void TogglePause()
    {
        bool paused = Time.timeScale == 0f;
        Time.timeScale = paused ? 1f : 0f;
        if (pausePanel != null) pausePanel.SetActive(!paused);
    }
}