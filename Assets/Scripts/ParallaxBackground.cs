using System;
using UnityEngine;

[Serializable]
public class ParallaxBackgroundLayer
{
    public string layerName;
    [HideInInspector] public MeshRenderer renderer;
    public MeshRenderer[] tileRenderers;
    [Range(0, 1), Tooltip("横向滚动强度：远景小、近景大。跳跃不影响此参数。")]
    public float scrollStrength = 0.4f;
    [HideInInspector] public float cameraDistance = 16;
    [Min(1.02f), Tooltip("图片在固定世界坐标中的高度倍率。")]
    public float imageHeight = 1.05f;
    [Min(0.1f), Tooltip("数值越小，树干越细、同一画面显示的树林越多。")]
    public float imageWidth = 0.65f;
    [Tooltip("源图片有效区域，0～1 坐标，用于排除白边。")]
    public Rect sourceRect = new Rect(0, 0, 1, 1);
    public float horizontalOffset;
    public Color tint = Color.white;
    public int sortingOrder = -200;
    [Min(0.1f), Tooltip("搭建时的参考视野高度。运行中保持固定，不随跳跃缩放。")]
    public float referenceViewHeight = 16;
    [Tooltip("背景固定世界 Z 坐标，需在角色后方。")]
    public float worldZ = 4;
    [NonSerialized] internal MaterialPropertyBlock[] properties;
}

/// <summary>仅横向 A/B/C 循环回收；纹理固定在世界坐标，森林下方为地下背景预留。</summary>
[ExecuteAlways, DefaultExecutionOrder(10000), DisallowMultipleComponent]
public class ParallaxBackground : MonoBehaviour
{
    public Camera targetCamera;
    public ParallaxBackgroundLayer[] layers;
    public Transform[] chunks;
    [Tooltip("森林背景的世界 Y 下边界，地下背景可放在此边界下方。不会向下复制树林。")]
    public float bottomY = -2;
    [Tooltip("可见区域的世界 X 边界，用于地下区域与森林区域交接。默认不限。") ]
    public float worldMinX = -100000;
    public float worldMaxX = 100000;
    [Tooltip("背景的固定世界 Y 中心。增大可将整套背景上移。")]
    public float worldCenterY;
    [Range(1.01f, 1.2f), Tooltip("视口覆盖余量。屏幕比例改变时会自动扩大各组覆盖宽度。")]
    public float viewportPadding = 1.02f;
    [Range(0, 0.02f), Tooltip("拼接边缘的小幅覆盖，消除浮点误差导致的细线。")]
    public float seamOverlap = 0.002f;
    public float ChunkWidth { get; private set; }

    private double originX;
    private bool hasOrigin;
    private readonly long[] indices = new long[3];
    private static readonly int UVTransform = Shader.PropertyToID("_UVTransform");
    private static readonly int SourceRect = Shader.PropertyToID("_SourceRect");
    private static readonly int Tint = Shader.PropertyToID("_Tint");
    private static readonly int WorldBottomY = Shader.PropertyToID("_WorldBottomY");

    void OnEnable() { hasOrigin = false; }
    void LateUpdate() => UpdateLayers();

    public long GetChunkIndex(int index) => indices[index];

    private static long RecycledIndex(int identity, long center)
    {
        long first = center - 1;
        return first + ((identity - 1 - first) % 3 + 3) % 3;
    }

    [ContextMenu("背景/重置横向起点")]
    public void ResetOrigin()
    {
        if (targetCamera == null) return;
        originX = targetCamera.transform.position.x;
        hasOrigin = true;
        UpdateLayers();
    }

    private Rect EffectiveRect(Rect rect)
    {
        rect.x = Mathf.Clamp(rect.x, 0, 0.999f); rect.y = Mathf.Clamp(rect.y, 0, 0.999f);
        rect.width = Mathf.Clamp(rect.width, 0.001f, 1 - rect.x);
        rect.height = Mathf.Clamp(rect.height, 0.001f, 1 - rect.y);
        return rect;
    }

    private float ViewHeight(float worldZ)
    {
        if (targetCamera.orthographic) return 2 * targetCamera.orthographicSize;
        float distance = Mathf.Max(targetCamera.nearClipPlane + 0.1f, worldZ - targetCamera.transform.position.z);
        return 2 * distance * Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
    }

    private float ImageWidth(ParallaxBackgroundLayer layer, Texture texture)
    {
        var rect = EffectiveRect(layer.sourceRect);
        return Mathf.Max(0.1f, layer.referenceViewHeight) * Mathf.Max(1.02f, layer.imageHeight)
            * texture.width * rect.width / (texture.height * rect.height) * Mathf.Max(0.1f, layer.imageWidth);
    }

    public void UpdateLayers()
    {
        if (targetCamera == null && Application.isPlaying) targetCamera = Camera.main;
        if (targetCamera == null || layers == null || chunks == null || chunks.Length != 3) return;
        if (!hasOrigin) { originX = targetCamera.transform.position.x; hasOrigin = true; }
        var cameraPosition = targetCamera.transform.position;
        float padding = Mathf.Max(1.01f, viewportPadding);
        float width = 0.1f;
        foreach (var layer in layers)
        {
            if (layer == null || layer.renderer == null || layer.renderer.sharedMaterial == null) continue;
            var texture = layer.renderer.sharedMaterial.mainTexture;
            if (texture == null) continue;
            width = Mathf.Max(width, ImageWidth(layer, texture), ViewHeight(layer.worldZ) * targetCamera.aspect * padding);
        }
        ChunkWidth = width;
        double travel = cameraPosition.x - originX;
        long center = (long)Math.Floor(travel / width + 0.5);
        for (int tile = 0; tile < 3; tile++)
        {
            if (chunks[tile] == null) continue;
            // A/B/C 分别保留 -1/0/1 的余数身份。向右跨一组时，只有 A 从 -1 移到 2。
            indices[tile] = RecycledIndex(tile, center);
            chunks[tile].SetPositionAndRotation(new Vector3((float)(originX + indices[tile] * (double)width), 0, 0), Quaternion.identity);
            chunks[tile].localScale = Vector3.one;
        }
        foreach (var layer in layers)
        {
            if (layer == null || layer.renderer == null || layer.renderer.sharedMaterial == null
                || layer.tileRenderers == null || layer.tileRenderers.Length != 3) continue;
            var texture = layer.renderer.sharedMaterial.mainTexture;
            if (texture == null) continue;
            var rect = EffectiveRect(layer.sourceRect);
            float imageWorldHeight = Mathf.Max(0.1f, layer.referenceViewHeight) * Mathf.Max(1.02f, layer.imageHeight);
            float imageWorldWidth = ImageWidth(layer, texture);
            // 图片整张映射到固定世界高度。跳跃不扩大几何，不复制或延展上下边缘。
            float quadHeight = imageWorldHeight;
            float geometryCenterY = worldCenterY;
            float quadWidth = width + Mathf.Max(0, seamOverlap);
            if (layer.properties == null || layer.properties.Length != 3) layer.properties = new MaterialPropertyBlock[3];
            for (int tile = 0; tile < 3; tile++)
            {
                var surface = layer.tileRenderers[tile];
                if (surface == null) continue;
                surface.transform.localPosition = new Vector3(0, geometryCenterY, layer.worldZ);
                surface.transform.localRotation = Quaternion.identity;
                surface.transform.localScale = new Vector3(quadWidth, quadHeight, 1);
                surface.sortingOrder = layer.sortingOrder;
                double offset = (indices[tile] * (double)width - travel * (1 - Mathf.Clamp01(layer.scrollStrength))) / imageWorldWidth + layer.horizontalOffset;
                float uScale = quadWidth / imageWorldWidth, vScale = quadHeight / imageWorldHeight;
                if (layer.properties[tile] == null) layer.properties[tile] = new MaterialPropertyBlock();
                var properties = layer.properties[tile];
                properties.SetVector(UVTransform, new Vector4(uScale, vScale, 0.5f - uScale * 0.5f + (float)(offset % 2), 0.5f - vScale * 0.5f + (geometryCenterY - worldCenterY) / imageWorldHeight));
                properties.SetFloat(WorldBottomY, bottomY);
                properties.SetFloat("_WorldMinX", worldMinX);
                properties.SetFloat("_WorldMaxX", worldMaxX);
                properties.SetVector(SourceRect, new Vector4(rect.x, rect.y, rect.width, rect.height));
                properties.SetColor(Tint, layer.tint);
                surface.SetPropertyBlock(properties);
            }
        }
    }
}
