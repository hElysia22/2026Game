using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class ParallaxBackgroundSetup
{
    [MenuItem("Tools/Game/Create Parallax Background")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("请先停止运行。");
        var existing = Object.FindObjectOfType<ParallaxBackground>();
        if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
        var camera = Camera.main;
        if (camera == null) throw new System.InvalidOperationException("当前场景缺少主相机。");
        var shader = Shader.Find("Game/Parallax Background");
        if (shader == null) throw new System.InvalidOperationException("背景 Shader 尚未导入。");
        const string folder = "Assets/UI/Background";
        if (!AssetDatabase.IsValidFolder(folder + "/Materials")) AssetDatabase.CreateFolder(folder, "Materials");
        string[] files = {"远景.png", "中景.png", "前景.png"};
        string[] names = {"远景", "中景", "前景"};
        float[] speeds = {0.12f, 0.4f, 0.8f};
        var root = new GameObject("ParallaxBackground");
        Undo.RegisterCreatedObjectUndo(root, "Create parallax background");
        var background = root.AddComponent<ParallaxBackground>();
        background.targetCamera = camera;
        background.layers = new ParallaxBackgroundLayer[3];
        for (int i = 0; i < files.Length; i++)
        {
            string texturePath = folder + "/" + files[i];
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            var material = new Material(shader);
            material.name = names[i]; material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            AssetDatabase.CreateAsset(material, folder + "/Materials/" + names[i] + ".mat");
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = names[i]; surface.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(surface.GetComponent<Collider>());
            var renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            background.layers[i] = new ParallaxBackgroundLayer
            {
                layerName = names[i], renderer = renderer, scrollStrength = speeds[i], cameraDistance = 17,
                imageHeight = 1.575f, imageWidth = 1, sortingOrder = -300 + i * 100,
                sourceRect = new Rect(0, 0, 1, 1)
            };
        }
        var player = Object.FindObjectOfType<PlayerController>();
        var vcam = Object.FindObjectOfType<Unity.Cinemachine.CinemachineCamera>();
        if (player != null && vcam != null && vcam.Target.TrackingTarget == null)
        {
            Undo.RecordObject(vcam, "Set player camera follow target");
            vcam.Target.TrackingTarget = player.transform;
            EditorUtility.SetDirty(vcam);
        }
        BuildABC(background, camera);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, folder + "/ParallaxBackground.prefab", InteractionMode.AutomatedAction);
        background.targetCamera = camera;
        PrefabUtility.RecordPrefabInstancePropertyModifications(background);
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
    }

    public static void BuildABC(ParallaxBackground background, Camera camera)
    {
        if (background.chunks != null && background.chunks.Length == 3) return;
        if (PrefabUtility.IsPartOfPrefabInstance(background.gameObject))
            PrefabUtility.UnpackPrefabInstance(background.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        background.targetCamera = camera;
        background.worldCenterY = camera.transform.position.y;
        background.chunks = new Transform[3];
        for (int tile = 0; tile < 3; tile++)
        {
            var group = new GameObject(new[] {"A", "B", "C"}[tile]);
            group.transform.SetParent(background.transform, false);
            background.chunks[tile] = group.transform;
        }
        foreach (var layer in background.layers)
        {
            layer.referenceViewHeight = camera.orthographic ? camera.orthographicSize * 2
                : 2 * layer.cameraDistance * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            layer.worldZ = camera.transform.position.z + layer.cameraDistance;
            layer.renderer.transform.SetParent(background.chunks[1], false);
            layer.tileRenderers = new MeshRenderer[3];
            layer.tileRenderers[1] = layer.renderer;
            foreach (int tile in new[] {0, 2})
            {
                var copy = Object.Instantiate(layer.renderer.gameObject, background.chunks[tile], false);
                copy.name = layer.layerName;
                layer.tileRenderers[tile] = copy.GetComponent<MeshRenderer>();
            }
        }
        // 三层素材使用同一投影平面、原图比例和起点，保持初始合成对齐。
        var first = background.layers[0];
        background.worldCenterY = camera.transform.position.y
            + first.referenceViewHeight * (first.imageHeight - 1) * 0.5f;
        background.ResetOrigin();
        EditorUtility.SetDirty(background);
    }

    public static void RemoveVerticalRows(ParallaxBackground background)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(background.gameObject))
            PrefabUtility.UnpackPrefabInstance(background.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var layer in background.layers)
            for (int tile = 0; tile < 3; tile++)
                layer.tileRenderers[tile].transform.SetParent(background.chunks[tile], false);
        foreach (var chunk in background.chunks)
            for (int i = chunk.childCount - 1; i >= 0; i--)
                if (chunk.GetChild(i).GetComponent<MeshRenderer>() == null)
                    Undo.DestroyObjectImmediate(chunk.GetChild(i).gameObject);
        background.UpdateLayers();
        EditorUtility.SetDirty(background);
    }
}
