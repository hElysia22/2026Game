using UnityEngine;

[System.Serializable]
public class AttackStep
{
    public string animName = "Attack1";
    public float damage = 1f;
    public float duration = 0.3f;
    public float comboWindowStart = 0.15f;
    public float comboWindowEnd = 0.3f;
    public Vector2 hitboxOffset = new Vector2(1f, 0f);
    public Vector2 hitboxSize = new Vector2(1f, 1f);
    public float knockback = 3f;
    public AudioClip sfx;
    public GameObject vfxPrefab;
}