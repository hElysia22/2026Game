using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class TriggerZone : MonoBehaviour
{
    public string flagId;
    public bool triggerOnce = true;
    public UnityEvent onTrigger;

    void Start()
    {
        if (triggerOnce && GameFlags.Instance != null && GameFlags.Instance.HasFlag(flagId))
            gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && GameFlags.Instance != null && GameFlags.Instance.HasFlag(flagId)) return;

        if (!string.IsNullOrEmpty(flagId) && GameFlags.Instance != null)
            GameFlags.Instance.SetFlag(flagId);

        onTrigger?.Invoke();

        if (triggerOnce) GetComponent<Collider2D>().enabled = false;
    }
}