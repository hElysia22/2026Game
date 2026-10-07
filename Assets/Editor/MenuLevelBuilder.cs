using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>生成可编辑场景对象，运行时不重建关卡或界面。</summary>
public static class MenuLevelBuilder
{
    const string StartPath = "Assets/Scene/StartScene.unity";
    const string GamePath = "Assets/Scene/GameScene.unity";
    static Material midMaterial, soilMaterial;
    static AudioClip Click => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/menu click.wav");

    [MenuItem("Tools/Game/Create Start And Pause UI")]
    public static void BuildMenus()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        ImportMenuImages();
        var game = EditorSceneManager.OpenScene(GamePath);
        if (GameObject.Find("PauseCanvas") == null) BuildPause();
        EditorSceneManager.SaveScene(game);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StartPath) == null)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = Color.black;
            camera.GetComponent<Camera>().cullingMask = 0;
            var root = Canvas("StartCanvas", 100);
            var controller = root.AddComponent<StartMenuController>(); controller.clickSound = Click;
            var bg = Node("Background", root.transform);
            Stretch(bg);
            var raw = bg.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Start/background.png");
            raw.raycastTarget = false;
            var aspect = bg.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)raw.texture.width / raw.texture.height;
            var play = Button("PlayButton", bg, "Assets/UI/Start/Sprites/play.png", "Assets/UI/Start/Sprites/play-选择后.png", new Vector2(.30f, .415f), new Vector2(270, 133));
            var quit = Button("QuitButton", bg, "Assets/UI/Start/Sprites/quit.png", "Assets/UI/Start/Sprites/quit-选择后.png", new Vector2(.30f, .255f), new Vector2(270, 133));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(play.onClick, controller.StartGame);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);
            var nav = play.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.Explicit; nav.selectOnDown = quit; nav.selectOnUp = quit; play.navigation = nav;
            nav = quit.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.Explicit; nav.selectOnUp = play; nav.selectOnDown = play; quit.navigation = nav;
            var es = EventSystem(); es.firstSelectedGameObject = play.gameObject;
            EditorSceneManager.SaveScene(scene, StartPath);
        }
        var others = EditorBuildSettings.scenes.Where(s => s.path != StartPath && s.path != GamePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(StartPath, true), new EditorBuildSettingsScene(GamePath, true) }.Concat(others).ToArray();
        EditorSceneManager.OpenScene(GamePath);
        AssetDatabase.SaveAssets();
    }

    static void ImportMenuImages()
    {
        foreach (var folder in new[] {"Assets/UI/Start/Sprites", "Assets/UI/Pause/Sprites"})
        foreach (var path in System.IO.Directory.GetFiles(folder, "*.png"))
        {
            var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        ImportTexture("Assets/UI/Start/background.png", 4096);
    }

    static void BuildPause()
    {
        var manager = UnityEngine.Object.FindObjectOfType<GameManager>();
        var actor = UnityEngine.Object.FindObjectOfType<PlayerController>();
        manager.input = actor.input; manager.player = actor.gameObject;
        var flags = manager.GetComponent<GameFlags>();
        if (flags != null)
        {
            new GameObject("PersistentGameFlags", typeof(GameFlags));
            UnityEngine.Object.DestroyImmediate(flags);
        }
        var root = Canvas("PauseCanvas", 200);
        var view = root.AddComponent<PauseMenuView>(); view.manager = manager; view.clickSound = Click;
        var panel = Node("PausePanel", root.transform); Stretch(panel); view.panel = panel.gameObject;
        var dim = panel.gameObject.AddComponent<UnityEngine.UI.Image>(); dim.color = new Color(0, 0, 0, .48f); dim.raycastTarget = true;
        var art = Node("Artwork", panel); Center(art, Vector2.zero, new Vector2(1000, 612));
        var image = art.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = Sprite("Assets/UI/Pause/Sprites/暂停界面.png"); image.raycastTarget = false;
        var border = Node("Border", art); Stretch(border);
        image = border.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = Sprite("Assets/UI/Pause/Sprites/边框.png"); image.raycastTarget = false;
        var restart = Button("RestartButton", panel, "Assets/UI/Pause/Sprites/restart.png", null, new Vector2(.5f, .5f), new Vector2(370, 77));
        restart.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 52);
        var menu = Button("MenuButton", panel, "Assets/UI/Pause/Sprites/Menu.png", null, new Vector2(.5f, .5f), new Vector2(370, 77));
        menu.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -52);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(restart.onClick, view.RestartGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(menu.onClick, view.ReturnToMenu);
        manager.pausePanel = panel.gameObject;
        manager.deathPanel = null;
        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(manager);
        EventSystem();
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/UI/Pause/PauseUI.prefab");
    }

    static GameObject Canvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        return go;
    }

    static UnityEngine.EventSystems.EventSystem EventSystem()
    {
        var es = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es != null) return es;
        var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        return go.GetComponent<UnityEngine.EventSystems.EventSystem>();
    }

    static RectTransform Node(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>();
    }
    static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
    static void Center(RectTransform rt, Vector2 position, Vector2 size) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = position; rt.sizeDelta = size; }
    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static UnityEngine.UI.Button Button(string name, Transform parent, string normal, string selected, Vector2 anchor, Vector2 size)
    {
        var rt = Node(name, parent); Center(rt, Vector2.zero, size); rt.anchorMin = rt.anchorMax = anchor;
        var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = Sprite(normal); image.preserveAspect = true; image.raycastTarget = true;
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        if (selected != null)
        {
            button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            var state = button.spriteState; state.highlightedSprite = state.pressedSprite = state.selectedSprite = Sprite(selected); button.spriteState = state;
        }
        else
        {
            var colors = button.colors; colors.highlightedColor = new Color(1, 1, .75f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.8f, .8f, .6f); button.colors = colors;
        }
        return button;
    }

    static void ImportTexture(string path, int max)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        imp.textureType = TextureImporterType.Default; imp.maxTextureSize = max; imp.npotScale = TextureImporterNPOTScale.None;
        imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.filterMode = FilterMode.Bilinear; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport();
    }

    [MenuItem("Tools/Game/Create Cavern Level")]
    public static void BuildLevel()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        if (SceneManager.GetActiveScene().path != GamePath) EditorSceneManager.OpenScene(GamePath);
        if (GameObject.Find("CavernLevel") != null) throw new Exception("CavernLevel exists; edit its terrain objects directly.");
        ImportTexture("Assets/UI/Underground/Background.png", 8192);
        ImportTexture("Assets/UI/Underground/Island.png", 8192);
        var g = UnityEngine.Object.FindObjectOfType<GroundStrip>();
        midMaterial = g.midLayer.sharedMaterial; soilMaterial = g.bottomLayer.sharedMaterial;
        g.transform.position = new Vector3(6, 0, 0); g.width = 24; g.depth = 40; g.Refresh(); EditorUtility.SetDirty(g);
        var level = new GameObject("CavernLevel");
        Ledge("UpperForest", level.transform, 36, 62, 0, 1.2f, g);
        Ledge("RightMud", level.transform, 62, 98, 0, 42, g);
        Ledge("LowerLeft", level.transform, 18, 28, -18, 20, null);
        Ledge("LowerMiddle", level.transform, 29.2f, 39, -18, 20, null);
        Ledge("LowerRight", level.transform, 40.2f, 62, -18, 20, null);
        BuildIsland(level.transform);
        var forest = UnityEngine.Object.FindObjectsOfType<ParallaxBackground>().First(b => b.name == "ParallaxBackground");
        forest.worldMinX = -100000; forest.worldMaxX = 100000;
        forest.ResetOrigin(); EditorUtility.SetDirty(forest); PrefabUtility.RecordPrefabInstancePropertyModifications(forest);
        BuildUnderground(level.transform);
        var actor = UnityEngine.Object.FindObjectOfType<PlayerController>(); actor.transform.position = new Vector3(-1, .6f, 0);
        actor.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        if (actor.GetComponent<PlayerFallRecovery>() == null) actor.gameObject.AddComponent<PlayerFallRecovery>();
        actor.GetComponent<PlayerFallRecovery>().fallY = -26;
        var template = UnityEngine.Object.FindObjectOfType<EnemyAI>();
        var enemyData = UnityEngine.Object.Instantiate(template.data); enemyData.name = "Cavern Enemy Data";
        enemyData.patrolDistance = 1.4f; enemyData.ignoreY = false; enemyData.detectRange = 6; enemyData.loseTargetRange = 8;
        AssetDatabase.CreateAsset(enemyData, "Assets/UI/Underground/Cavern Enemy Data.asset");
        Vector2[] spawns = {new Vector2(8, .7f), new Vector2(29.5f, -2.1f), new Vector2(37, -5), new Vector2(47, -5), new Vector2(89, .7f)};
        for (int i = 0; i < spawns.Length; i++)
        {
            var enemy = i == 0 ? template : UnityEngine.Object.Instantiate(template);
            enemy.name = "Enemy" + (i + 1); enemy.transform.SetParent(level.transform, true);
            enemy.transform.position = new Vector3(spawns[i].x, spawns[i].y, 0); enemy.enabled = true; enemy.data = enemyData;
            enemy.GetComponent<Rigidbody2D>().velocity = Vector2.zero; EditorUtility.SetDirty(enemy);
            var oldRenderer = enemy.GetComponent<SpriteRenderer>();
            var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (oldRenderer != null && circle != null)
            {
                var visual = new GameObject("EnemyCircle", typeof(SpriteRenderer)); visual.transform.SetParent(enemy.transform, false);
                var renderer = visual.GetComponent<SpriteRenderer>(); renderer.sprite = circle; renderer.color = oldRenderer.color; renderer.sortingOrder = oldRenderer.sortingOrder;
                visual.transform.localScale = Vector3.one * ((i == 4 ? 1.3f : 1) / circle.bounds.size.x);
                oldRenderer.enabled = false;
            }
        }
        CavernRouteBuilder.AddMudSteps(level.transform);
        BossArenaBuilder.Configure();
        CavernRouteBuilder.ConfigureCavern(level.transform);
        // Old white ladder placeholders are unrelated to this jump route; retain them disabled.
        foreach (var ladder in UnityEngine.Object.FindObjectsOfType<Ladder>()) ladder.gameObject.SetActive(false);
        AssetDatabase.SaveAssets(); Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    static void Ledge(string name, Transform parent, float x1, float x2, float y, float depth, GroundStrip reference)
    {
        var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.position = new Vector3((x1 + x2) * .5f, 0, 0);
        var strip = root.AddComponent<GroundStrip>(); strip.width = x2 - x1; strip.surfaceY = y; strip.depth = depth;
        strip.worldZ = .1f; strip.midHeight = .4110949f; strip.midCenterOffset = -strip.midHeight * .5f; strip.soilTopOffset = -strip.midHeight;
        strip.midLayer = Quad("MidLayer", root.transform, midMaterial);
        strip.bottomLayer = Quad("BottomLayer", root.transform, soilMaterial);
        var collision = new GameObject("GroundCollider", typeof(BoxCollider2D)); collision.transform.SetParent(root.transform, false); collision.layer = LayerMask.NameToLayer("Ground"); strip.groundCollider = collision.GetComponent<BoxCollider2D>();
        if (reference != null)
        {
            strip.topLayer = Quad("TopLayer", root.transform, reference.topLayer.sharedMaterial);
            strip.topHeight = reference.topHeight; strip.topGap = reference.topGap; strip.topSourceRect = reference.topSourceRect; strip.topCenterOffset = reference.topCenterOffset;
        }
        strip.Refresh();
    }

    static MeshRenderer Quad(string name, Transform parent, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name; go.transform.SetParent(parent, false);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return r;
    }

    static void BuildIsland(Transform parent)
    {
        const float x0 = 3480, yBottom = 4680, sourceWidth = 6788, sourceHeight = 3237, worldWidth = 30;
        float scale = worldWidth / sourceWidth, height = sourceHeight * scale;
        var material = new Material(Shader.Find("Game/Parallax Background")); material.name = "Underground Island";
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Underground/Island.png");
        material.SetVector("_SourceRect", new Vector4(x0 / 12923, (5069 - yBottom) / 5069, sourceWidth / 12923, sourceHeight / 5069));
        AssetDatabase.CreateAsset(material, "Assets/UI/Underground/Island.mat");
        var r = Quad("FloatingIsland", parent, material); r.sortingOrder = -25;
        float centerY = -5.6f - ((yBottom - 2390) * scale - height * .5f);
        r.transform.position = new Vector3(39, centerY, .1f); r.transform.localScale = new Vector3(worldWidth, height, 1);
        var colGo = new GameObject("IslandCollider", typeof(PolygonCollider2D)); colGo.transform.SetParent(parent, false); colGo.layer = LayerMask.NameToLayer("Ground");
        colGo.transform.position = new Vector3(39, centerY, 0);
        Vector2[] px = {new Vector2(3520,2600),new Vector2(3745,1780),new Vector2(5280,1760),new Vector2(5740,2390),new Vector2(9800,2390),new Vector2(9910,2570),new Vector2(9310,3160),new Vector2(8770,3450),new Vector2(8770,3740),new Vector2(10200,3810),new Vector2(10250,3990),new Vector2(9250,4350),new Vector2(8220,4350),new Vector2(8140,3610),new Vector2(7300,3760),new Vector2(6200,4000),new Vector2(4500,3400),new Vector2(3870,3010)};
        colGo.GetComponent<PolygonCollider2D>().points = px.Select(p => new Vector2((p.x - x0 - sourceWidth * .5f) * scale, (yBottom - p.y - sourceHeight * .5f) * scale)).ToArray();
    }

    static void BuildUnderground(Transform parent)
    {
        var old = GameObject.Find("UndergroundBackground");
        if (old == null) old = UnityEngine.Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "UndergroundBackground")?.gameObject;
        var root = old ?? new GameObject("UndergroundBackground"); root.SetActive(true); root.transform.SetParent(parent, true);
        var b = root.AddComponent<ParallaxBackground>(); b.targetCamera = Camera.main;
        b.worldCenterY = -20; b.bottomY = -44; b.worldMinX = 17.96f; b.worldMaxX = 64;
        var material = new Material(Shader.Find("Game/Parallax Background")); material.name = "Underground Background";
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Underground/Background.png");
        AssetDatabase.CreateAsset(material, "Assets/UI/Underground/Background.mat");
        b.chunks = new Transform[3]; var renderers = new MeshRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            var chunk = new GameObject(new[] {"A","B","C"}[i]); chunk.transform.SetParent(root.transform, false); b.chunks[i] = chunk.transform;
            renderers[i] = Quad("Underground", chunk.transform, material);
        }
        b.layers = new[] {new ParallaxBackgroundLayer {layerName="Underground",renderer=renderers[1],tileRenderers=renderers,scrollStrength=.18f,worldZ=.1f,referenceViewHeight=42,imageHeight=1.2f,imageWidth=1,sortingOrder=-350}};
        b.ResetOrigin();
    }
}
