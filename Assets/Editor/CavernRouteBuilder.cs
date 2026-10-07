using UnityEngine;
using UnityEditor;

public static class CavernRouteBuilder
{
    public static PhysicsMaterial2D ContactMaterial()
    {
        const string path="Assets/UI/Underground/TerrainContact.physicsMaterial2D";
        var material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if(material==null){material=new PhysicsMaterial2D("Terrain Contact") {friction=0,bounciness=0};AssetDatabase.CreateAsset(material,path);}
        return material;
    }

    public static void ConfigureCavern(Transform parent)
    {
        var contact=ContactMaterial();
        foreach(var c in parent.GetComponentsInChildren<Collider2D>(true)) if(!c.isTrigger)c.sharedMaterial=contact;
        var right=GameObject.Find("RightMud").GetComponent<GroundStrip>();right.bottomLayer.enabled=false;right.groundCollider.enabled=false;
        AddWall("RightCaveWall",parent,98,new Vector2[]{new Vector2(62,0),new Vector2(62,-2),new Vector2(63.4f,-5),new Vector2(62.8f,-8),new Vector2(62,-10),new Vector2(63.2f,-13),new Vector2(62.3f,-16),new Vector2(62,-18),new Vector2(62,-42)},right.bottomLayer.sharedMaterial,contact);
        AddWall("LeftCaveEdge",parent,17,new Vector2[]{new Vector2(18,0),new Vector2(18.2f,-2),new Vector2(18.6f,-5),new Vector2(18.2f,-8),new Vector2(18.7f,-12),new Vector2(18.3f,-15),new Vector2(18,-18),new Vector2(18,-38)},right.bottomLayer.sharedMaterial,contact);
        var background=GameObject.Find("UndergroundBackground").GetComponent<ParallaxBackground>();background.worldMaxX=64;background.layers[0].tint=new Color(.5f,.62f,.7f,1);background.UpdateLayers();
        foreach(var enemy in parent.GetComponentsInChildren<EnemyAI>(true))
        {
            if(enemy.name=="Enemy3"||enemy.name=="Enemy4")Object.DestroyImmediate(enemy.gameObject);
            else if(enemy.name=="Enemy2")enemy.transform.position=new Vector3(41,-5,0);
        }
    }

    static void AddWall(string name, Transform parent,float outerX,Vector2[] points,Material material,PhysicsMaterial2D contact)
    {
        var child=parent.Find(name);if(child!=null)return;
        var root=new GameObject(name,typeof(CavernBoundary));root.transform.SetParent(parent,false);root.layer=LayerMask.NameToLayer("Ground");root.transform.position=new Vector3(0,0,.1f);
        var wall=root.GetComponent<CavernBoundary>();wall.outerX=outerX;wall.contour=points;wall.soilMaterial=material;wall.contactMaterial=contact;wall.Refresh();
    }

    public static void AddMudSteps(Transform parent)
    {
        var reference = GameObject.Find("RightMud").GetComponent<GroundStrip>();
        float[] ys={-15.5f,-13,-8.8f,-6.4f};
        for(int i=0;i<4;i++)
        {
            if(parent.Find("CaveMudStep"+(i+1))!=null)continue;
            float left=i==0||i==2?55.9f:57.1f;
            var go=new GameObject("CaveMudStep"+(i+1),typeof(RoundedMudPlatform));go.transform.SetParent(parent,false);go.layer=LayerMask.NameToLayer("Ground");go.transform.position=new Vector3((left+64)*.5f,ys[i],.1f);
            var p=go.GetComponent<RoundedMudPlatform>();p.width=64-left;p.depth=1.2f;p.radius=.4f;p.edgeMaterial=reference.midLayer.sharedMaterial;p.soilMaterial=reference.bottomLayer.sharedMaterial;p.contactMaterial=ContactMaterial();p.Refresh();
        }
    }
}
