using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    public string gameScene = "GameScene";
    public AudioClip clickSound;
    [Range(0, 1)] public float clickVolume = 0.65f;
    public bool IsLoading { get; private set; }

    void Awake()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        if (IsLoading) return;
        IsLoading = true;
        MenuAudio.Play(clickSound, clickVolume);
        SceneManager.LoadSceneAsync(gameScene);
    }

    public void QuitGame()
    {
        MenuAudio.Play(clickSound, clickVolume);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
