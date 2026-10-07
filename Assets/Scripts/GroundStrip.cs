using UnityEngine;

/// <summary>整张地面素材叠层显示；草片间隔摆放，泥土横向连续映射；不改变已有地形碰撞。</summary>
[ExecuteAlways, DisallowMultipleComponent]
public class GroundStrip : MonoBehaviour
{
    public MeshRenderer topLayer;
    public MeshRenderer midLayer;
    public MeshRenderer bottomLayer;
    public BoxCollider2D groundCollider;
    [Min(0.1f)] public float width = 10;
    [Tooltip("平台可站立表面的世界 Y 高度。")]
    public float surfaceY;
    [Min(1)] public float depth = 4;
    [Min(0.1f), Tooltip("草地图片的世界高度，横向周期按图片宽高比计算。")]
    public float topHeight = 2.2f;
    [Min(0), Tooltip("相邻草地图片之间的空隙，世界单位；0 恢复连续衔接。") ]
    public float topGap = 3;
    [Tooltip("草地有效区域，排除原图的大块透明留白；不修改原始图片。")]
    public Rect topSourceRect = new Rect(780f / 4663, 273f / 1580, 2816f / 4663, 444f / 1580);
    [Tooltip("草地相对站立表面的中心偏移。")]
    public float topCenterOffset = -0.4f;
    [Min(0.1f)] public float midHeight = 1.2f;
    public float midCenterOffset = -0.9f;
    [Tooltip("泥土顶部相对站立表面的偏移。")]
    public float soilTopOffset = -0.8f;
    [Tooltip("视觉图层的世界 Z；放在角色后方。")]
    public float worldZ = 0.1f;
    [Tooltip("地表草丛绘制在角色前方，遮住脚部。角色默认排序为 0。") ]
    public int topSortingOrder = 20;
    [Tooltip("统一横向纹理起点；相邻地面相同数值时不会重置纹理。")]
    public float textureOriginX;
    private MaterialPropertyBlock block;

    void OnEnable() => Refresh();
    void LateUpdate() => Refresh();

    [ContextMenu("地面/刷新外观")]
    public void Refresh()
    {
        if (groundCollider != null)
        {
            var size = new Vector2(Mathf.Max(0.1f, width), Mathf.Max(1, depth));
            var center = new Vector3(transform.position.x, surfaceY - size.y * 0.5f, 0);
            if (groundCollider.size != size) groundCollider.size = size;
            if (groundCollider.transform.position != center) groundCollider.transform.position = center;
        }
        float h = Mathf.Max(0.1f, topHeight);
        Rect crop = topSourceRect;
        crop.x = Mathf.Clamp(crop.x, 0, 0.999f); crop.y = Mathf.Clamp(crop.y, 0, 0.999f);
        crop.width = Mathf.Clamp(crop.width, 0.001f, 1 - crop.x);
        crop.height = Mathf.Clamp(crop.height, 0.001f, 1 - crop.y);
        float period = h;
        if (topLayer != null && topLayer.sharedMaterial != null && topLayer.sharedMaterial.mainTexture != null)
        {
            var t = topLayer.sharedMaterial.mainTexture;
            period = h * t.width * crop.width / (t.height * crop.height);
        }
        float topPeriod = period + Mathf.Max(0, topGap);
        Apply(topLayer, surfaceY + topCenterOffset, h, topPeriod, crop, topSortingOrder, period / topPeriod);
        Apply(midLayer, surfaceY + midCenterOffset, Mathf.Max(0.1f, midHeight), period, new Rect(0, 0, 1, 1), -10);
        float soilHeight = Mathf.Max(0.1f, depth + soilTopOffset);
        Apply(bottomLayer, surfaceY + soilTopOffset - soilHeight * 0.5f, soilHeight, period, new Rect(0, 0, 1, 1), -20);
    }

    private void Apply(MeshRenderer surface, float y, float height, float period, Rect crop, int order, float horizontalFill = 1)
    {
        if (surface == null) return;
        float w = Mathf.Max(0.1f, width);
        surface.transform.position = new Vector3(transform.position.x, y, worldZ);
        surface.transform.rotation = Quaternion.identity;
        surface.transform.localScale = new Vector3(w, height, 1);
        surface.sortingOrder = order;
        // 在每条地面边缘使用相同的世界 UV，既可镜像平铺也可跨多个视觉条连续。
        double leftU = (transform.position.x - w * 0.5 - textureOriginX) / Mathf.Max(0.1f, period);
        if (block == null) block = new MaterialPropertyBlock();
        block.Clear();
        block.SetVector("_UVTransform", new Vector4(w / period, 1, (float)(leftU % 2), 0));
        block.SetVector("_SourceRect", new Vector4(crop.x, crop.y, crop.width, crop.height));
        block.SetFloat("_WorldBottomY", -100000);
        block.SetFloat("_HorizontalFill", horizontalFill);
        block.SetColor("_Tint", Color.white);
        surface.SetPropertyBlock(block);
    }
}
