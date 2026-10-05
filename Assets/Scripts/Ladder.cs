using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Ladder : MonoBehaviour
{
    public Transform topPoint;      // 可选，顶端位置
    public Transform bottomPoint;   // 可选，底端位置

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }
}