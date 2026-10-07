using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 受击闪红：订阅同物体（或父物体）上的 Health.OnDamaged，把子物体的 SpriteRenderer 短暂染红。
/// 只改 SpriteRenderer.color，不替换材质；用真实时间计时，顿帧（timeScale = 0）期间也能照常结束。
/// </summary>
[DisallowMultipleComponent]
public class SpriteFlash : MonoBehaviour
{
    [Tooltip("闪红颜色：与精灵原色相乘，越接近白色越淡。")]
    public Color flashColor = new Color(1f, 0.5f, 0.5f, 1f);

    [Min(0.01f), Tooltip("一次闪红的持续秒数（真实时间，不受顿帧影响）。")]
    public float duration = 0.18f;

    [Min(1), Tooltip("红白交替次数：1 = 闪一下；2 = 闪两下。")]
    public int flashes = 2;

    private Health health;
    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private readonly List<Color> baseColors = new List<Color>();
    private Coroutine routine;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (r == null) continue;
            renderers.Add(r);
            baseColors.Add(r.color);
        }
    }

    void OnEnable()
    {
        if (health != null) health.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (health != null) health.OnDamaged -= HandleDamaged;
        if (routine != null) { StopCoroutine(routine); routine = null; }
        Restore();
    }

    private void HandleDamaged()
    {
        if (!isActiveAndEnabled || renderers.Count == 0 || duration <= 0f) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            // 0→1 往返 flashes 次：0 是原色，1 是最红。
            float phase = Mathf.PingPong(t / duration * flashes, 1f);
            ApplyTint(phase);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Restore();
        routine = null;
    }

    private void ApplyTint(float phase)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var baseColor = baseColors[i];
            var tint = new Color(baseColor.r * flashColor.r, baseColor.g * flashColor.g,
                                 baseColor.b * flashColor.b, baseColor.a);
            r.color = Color.Lerp(baseColor, tint, phase);
        }
    }

    private void Restore()
    {
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].color = baseColors[i];
    }
}
