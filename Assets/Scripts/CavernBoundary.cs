using UnityEngine;

/// <summary>使用现有泥土材质生成起伏洞壁；轮廓同时驱动外观与二维碰撞，不新增贴图。</summary>
[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class CavernBoundary : MonoBehaviour
{
    [Tooltip("从上到下填写洞壁边缘坐标，使用物体局部坐标。")]
    public Vector2[] contour;
    [Tooltip("泥土另一侧的 X；左洞壁小于轮廓 X，右洞壁大于轮廓 X。")]
    public float outerX;
    public Material soilMaterial;
    public PhysicsMaterial2D contactMaterial;
    Mesh mesh;
    bool dirty;

    void OnEnable() { dirty = true; }
    void OnValidate() { dirty = true; }
    void LateUpdate() { if (dirty) Refresh(); }

    public void Refresh()
    {
        dirty = false;
        if (contour == null || contour.Length < 2) return;
        if (mesh == null) { mesh = new Mesh { name = "Cavern wall", hideFlags = HideFlags.HideAndDontSave }; }
        mesh.Clear();
        var vertices = new Vector3[contour.Length * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(contour.Length - 1) * 6];
        for (int i = 0; i < contour.Length; i++)
        {
            vertices[i * 2] = contour[i];
            vertices[i * 2 + 1] = new Vector3(outerX, contour[i].y, 0);
            uv[i * 2] = new Vector2(0, (float)i / (contour.Length - 1));
            uv[i * 2 + 1] = new Vector2(1, uv[i * 2].y);
            if (i == contour.Length - 1) continue;
            int a = i * 2, t = i * 6;
            triangles[t] = a; triangles[t+1] = a+1; triangles[t+2] = a+2;
            triangles[t+3] = a+1; triangles[t+4] = a+3; triangles[t+5] = a+2;
        }
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = GetComponent<MeshRenderer>(); renderer.sharedMaterial = soilMaterial; renderer.sortingOrder = -20;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        var path = new Vector2[contour.Length + 2];
        System.Array.Copy(contour, path, contour.Length);
        path[contour.Length] = new Vector2(outerX, contour[contour.Length-1].y);
        path[contour.Length+1] = new Vector2(outerX, contour[0].y);
        var collider = GetComponent<PolygonCollider2D>(); collider.points = path; collider.sharedMaterial = contactMaterial;
    }

    void OnDisable()
    {
        if (mesh == null) return;
        GetComponent<MeshFilter>().sharedMesh = null;
        if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        mesh = null;
    }
}
