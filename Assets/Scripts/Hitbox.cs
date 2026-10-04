using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public int damage = 1;
    public Vector2 size = Vector2.one;
    public float knockback = 3f;
    public float invincibleTime = 0.8f;
    public LayerMask targetLayer;
    public GameObject hitVfx;
    public AudioClip hitSfx;
    public GameObject owner;

    private readonly HashSet<Collider2D> hitTargets = new();

    void OnEnable() => hitTargets.Clear();

    void Update()
    {
        var hits = Physics2D.OverlapBoxAll(transform.position, size, 0f, targetLayer);
        foreach (var h in hits)
        {
            if (h.gameObject == owner) continue;
            if (hitTargets.Contains(h)) continue;
            hitTargets.Add(h);

            var health = h.GetComponent<Health>();
            if (health == null) continue;

            int dir = owner != null
                ? (int)Mathf.Sign(h.transform.position.x - owner.transform.position.x)
                : 1;
            if (dir == 0) dir = 1;

            Vector2 kb = new Vector2(dir * knockback, knockback * 0.5f);
            if (health.TryTakeDamage(damage, invincibleTime, kb))
            {
                if (hitSfx != null) AudioSource.PlayClipAtPoint(hitSfx, transform.position);
                if (hitVfx != null) Instantiate(hitVfx, h.transform.position, Quaternion.identity);
                HitStop.Instance?.Play(0.05f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, size);
    }
}