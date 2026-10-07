using System.Collections;
using UnityEngine;

/// <summary>菜单点击声独立于游戏暂停，并允许完整播放到下一场景。</summary>
public class MenuAudio : MonoBehaviour
{
    public static void Play(AudioClip clip, float volume)
    {
        if (clip == null || !Application.isPlaying) return;
        var go = new GameObject("MenuClickAudio");
        DontDestroyOnLoad(go);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.ignoreListenerPause = true;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
        var owner = go.AddComponent<MenuAudio>();
        owner.StartCoroutine(owner.RemoveAfterSound(clip.length));
    }

    IEnumerator RemoveAfterSound(float duration)
    {
        yield return new WaitForSecondsRealtime(duration + 0.1f);
        Destroy(gameObject);
    }
}
