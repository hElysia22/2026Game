using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class RoundedMudPlatform : MonoBehaviour
{
    [Min(1)] public float width = 6;
    [Min(.5f)] public float depth = 1.2f;
    [Min(.05f)] public float radius = .4f;
    public Material edgeMaterial, soilMaterial;
    public PhysicsMaterial2D contactMaterial;
    Mesh outerMesh, innerMesh;
    MeshRenderer innerRenderer;
    bool dirty;
    void OnEnable() { dirty = true; }
    void OnValidate() { dirty = true; }
    void LateUpdate() { if (dirty) Refresh(); }

    Vector2[] Outline(float inset)
    {
        float w = Mathf.Max(.2f,width-2*inset), h = Mathf.Max(.2f,depth-2*inset);
        float r = Mathf.Clamp(radius-inset,.03f,Mathf.Min(w,h)*.5f);
        Vector2[] centers={new Vector2(w*.5f-r,-inset-r),new Vector2(w*.5f-r,-inset-h+r),new Vector2(-w*.5f+r,-inset-h+r),new Vector2(-w*.5f+r,-inset-r)};
        var points=new Vector2[24];
        for(int corner=0;corner<4;corner++)for(int i=0;i<6;i++)
        {
            float angle=(90-corner*90-i*18)*Mathf.Deg2Rad;
            points[corner*6+i]=centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r;
        }
        return points;
    }
    void Fill(Mesh mesh,Vector2[] points)
    {
        var v=new Vector3[points.Length+1];var uv=new Vector2[v.Length];var t=new int[points.Length*3];
        v[0]=new Vector3(0,-depth*.5f,0);uv[0]=new Vector2(.5f,.5f);
        for(int i=0;i<points.Length;i++)
        {
            v[i+1]=points[i];uv[i+1]=new Vector2(points[i].x/width+.5f,1+points[i].y/depth);
            t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=(i+1)%points.Length+1;
        }
        mesh.Clear();mesh.vertices=v;mesh.uv=uv;mesh.triangles=t;mesh.RecalculateBounds();
    }
    public void Refresh()
    {
        dirty=false;
        if(outerMesh==null)outerMesh=new Mesh{name="Round mud edge",hideFlags=HideFlags.HideAndDontSave};
        if(innerMesh==null)innerMesh=new Mesh{name="Round mud soil",hideFlags=HideFlags.HideAndDontSave};
        if(innerRenderer==null)
        {
            var child=transform.Find("SoilInset");
            if(child==null){var go=new GameObject("SoilInset",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);child=go.transform;}
            innerRenderer=child.GetComponent<MeshRenderer>();
        }
        var points=Outline(0);Fill(outerMesh,points);Fill(innerMesh,Outline(.08f));
        GetComponent<MeshFilter>().sharedMesh=outerMesh;innerRenderer.GetComponent<MeshFilter>().sharedMesh=innerMesh;
        var r=GetComponent<MeshRenderer>();r.sharedMaterial=edgeMaterial;r.sortingOrder=-18;
        innerRenderer.sharedMaterial=soilMaterial;innerRenderer.sortingOrder=-17;
        r.shadowCastingMode=innerRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows=innerRenderer.receiveShadows=false;
        var col=GetComponent<PolygonCollider2D>();col.points=points;col.sharedMaterial=contactMaterial;
    }
    void OnDisable()
    {
        GetComponent<MeshFilter>().sharedMesh=null;
        if(innerRenderer!=null)innerRenderer.GetComponent<MeshFilter>().sharedMesh=null;
        if(Application.isPlaying){Destroy(outerMesh);Destroy(innerMesh);}else{DestroyImmediate(outerMesh);DestroyImmediate(innerMesh);}
        outerMesh=innerMesh=null;
    }
}
