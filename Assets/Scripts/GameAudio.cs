using UnityEngine;

/// <summary>2D 玩法音效：挥动与命中独立播放，移动音效只在地面实际移动时循环。</summary>
[DisallowMultipleComponent]
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("角色与播放源")]
    public PlayerController player;
    public AudioSource effectsSource;
    public AudioSource movementSource;

    [Header("音效资源")]
    public AudioClip daggerSwing;
    public AudioClip swordSwing;
    public AudioClip enemyAttack;
    public AudioClip hit;
    public AudioClip move;

    [Header("音量")]
    [Range(0, 1)] public float effectsVolume = 0.8f;
    [Range(0, 1)] public float movementVolume = 0.35f;
    [Min(0.01f), Tooltip("实际水平移动超过此速度才播放，贴墙保持方向键不会持续发声。")]
    public float minimumMoveSpeed = 0.1f;

    private float previousX;
    private float lastMovementTime = float.NegativeInfinity;
    private bool effectsPaused;
    private EquipmentMenuController equipmentMenu;

    void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (effectsSource == null) effectsSource = CreateSource("GameplayEffects");
        if (movementSource == null) movementSource = CreateSource("PlayerMovement");
        Configure(effectsSource, false);
        Configure(movementSource, true);
        movementSource.clip = move;
        if (player != null)
        {
            previousX = player.transform.position.x;
            equipmentMenu = player.GetComponent<EquipmentMenuController>();
        }
    }

    private AudioSource CreateSource(string sourceName)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        return child.AddComponent<AudioSource>();
    }

    private static void Configure(AudioSource source, bool loop)
    {
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.loop = loop;
        source.dopplerLevel = 0;
    }

    private bool IsMenuPaused => (equipmentMenu != null && equipmentMenu.IsOpen)
        || (GameManager.Instance != null && GameManager.Instance.IsPaused);

    public void PlayPlayerAttack(bool sword, AudioClip fallback = null)
    {
        var selected = sword ? swordSwing : daggerSwing;
        PlayEffect(selected != null ? selected : fallback);
    }

    public void PlayEnemyAttack() => PlayEffect(enemyAttack);
    public void PlayHit(AudioClip overrideClip = null) => PlayEffect(overrideClip != null ? overrideClip : hit);

    private void PlayEffect(AudioClip clip)
    {
        if (!isActiveAndEnabled || clip == null || effectsSource == null || IsMenuPaused) return;
        effectsSource.volume = effectsVolume;
        effectsSource.PlayOneShot(clip);
    }

    void LateUpdate()
    {
        if (effectsSource != null)
        {
            effectsSource.volume = effectsVolume;
            bool paused = IsMenuPaused;
            if (paused && !effectsPaused) effectsSource.Pause();
            else if (!paused && effectsPaused) effectsSource.UnPause();
            effectsPaused = paused;
        }
        if (movementSource == null) return;
        movementSource.volume = movementVolume;
        if (movementSource.clip != move)
        {
            movementSource.Stop();
            movementSource.clip = move;
        }
        bool moving = false;
        if (player != null)
        {
            float x = player.transform.position.x;
            if (Time.deltaTime > 0 && Mathf.Abs(x - previousX) > minimumMoveSpeed * Time.deltaTime)
                lastMovementTime = Time.time;
            previousX = x;
            // 短暂保留运动采样，避免渲染帧之间没有 FixedUpdate 时反复重播。
            moving = Time.timeScale > 0 && !IsMenuPaused && player.isActiveAndEnabled
                && player.CurrentState == PlayerState.Run && player.IsGrounded
                && (player.input == null || !player.input.IsGameplayInputBlocked)
                && Time.time - lastMovementTime < 0.08f;
        }
        if (moving && move != null)
        {
            if (!movementSource.isPlaying) movementSource.Play();
        }
        else if (movementSource.isPlaying) movementSource.Stop();
    }

    void OnDisable()
    {
        if (effectsSource != null) effectsSource.Stop();
        if (movementSource != null) movementSource.Stop();
        effectsPaused = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
