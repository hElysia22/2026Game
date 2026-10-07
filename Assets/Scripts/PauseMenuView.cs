using UnityEngine;

public class PauseMenuView : MonoBehaviour
{
    public GameManager manager;
    public GameObject panel;
    public TMPro.TMP_Text resultLabel;
    public GameObject resultHeadingBackground;
    public AudioClip clickSound;
    [Range(0, 1)] public float clickVolume = 0.65f;

    void OnEnable()
    {
        if (manager == null) manager = FindObjectOfType<GameManager>();
        if (manager == null) return;
        manager.OnPauseChanged += Show;
        Show(manager.IsPaused);
    }

    void OnDisable()
    {
        if (manager != null) manager.OnPauseChanged -= Show;
    }

    void Show(bool open)
    {
        if (panel != null) panel.SetActive(open);
        if (resultLabel == null || manager == null) return;
        var hp = manager.player != null ? manager.player.GetComponent<Health>() : null;
        bool ended = manager.IsCompleted || (hp != null && hp.IsDead);
        resultLabel.gameObject.SetActive(open && ended);
        if (resultHeadingBackground != null) resultHeadingBackground.SetActive(open && ended);
        resultLabel.text = manager.IsCompleted ? "关卡完成" : "挑战失败";
    }
    public void RestartGame()
    {
        MenuAudio.Play(clickSound, clickVolume);
        manager?.Restart();
    }
    public void ReturnToMenu()
    {
        MenuAudio.Play(clickSound, clickVolume);
        manager?.ReturnToStartMenu();
    }
}
