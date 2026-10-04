using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public string checkpointId = "cp_01";
    public Transform respawnPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance?.SetCheckpoint(checkpointId, respawnPoint != null ? respawnPoint.position : transform.position);
    }
}