using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }
    public bool IsActive { get; private set; }
    public float ResumeTimeScale { get; private set; } = 1;
    private float endTime;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Play(float duration)
    {
        if (duration <= 0) return;
        if (IsActive)
        {
            endTime = Mathf.Max(endTime, Time.realtimeSinceStartup + duration);
            return;
        }
        if (Time.timeScale == 0) return;
        ResumeTimeScale = Time.timeScale;
        endTime = Time.realtimeSinceStartup + duration;
        IsActive = true;
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        Time.timeScale = 0;
        while (Time.realtimeSinceStartup < endTime) yield return null;
        Finish();
    }

    private void Finish()
    {
        if (!IsActive) return;
        IsActive = false;
        var menu = FindObjectOfType<EquipmentMenuController>();
        // 玩家死亡时不在这里冻结时间：留给死亡动画播完，随后由 GameManager 弹失败界面并暂停。
        bool paused = (menu != null && menu.IsOpen)
            || (GameManager.Instance != null && GameManager.Instance.IsPaused);
        Time.timeScale = paused ? 0 : ResumeTimeScale;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        Finish();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
