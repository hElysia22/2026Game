using UnityEngine;

public class LevelEnd : MonoBehaviour
{
    public GameObject winPanel;
    public AudioClip winSfx;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (winPanel != null) winPanel.SetActive(true);
        if (winSfx != null) AudioSource.PlayClipAtPoint(winSfx, transform.position);
        Time.timeScale = 0f;
    }
}