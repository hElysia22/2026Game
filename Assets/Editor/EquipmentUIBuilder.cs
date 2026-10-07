using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>在编辑器中生成可编辑的 Canvas/Prefab；不在运行时重建 UI。</summary>
public static class EquipmentUIBuilder
{
    const string Folder = "Assets/UI/Equipment";
    static EquipmentUIStyle style;
    static readonly Color PanelColor = new Color(0.075f, 0.155f, 0.135f, 0.98f);

    [MenuItem("Tools/Game/Create Equipment UI")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new Exception("Please stop Play Mode.");
        if (GameObject.Find("EquipmentCanvas") != null) throw new Exception("EquipmentCanvas already exists. Edit the existing hierarchy.");
        ImportSprites();
        style = CreateStyle();
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (player == null || player.GetComponent<EquipmentMenuController>() == null) throw new Exception("Player equipment menu is missing.");
        var root = new GameObject("EquipmentCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(EquipmentPanelView));
        Undo.RegisterCreatedObjectUndo(root, "Create spirit equipment UI");
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var view = root.GetComponent<EquipmentPanelView>(); view.style = style;
        BuildHUD(root.transform);
        BuildPanel(root.transform, view);
        view.panelRoot.SetActive(false);

        if (UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true).Length == 0)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create UI event system");
            es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, Folder + "/EquipmentUI.prefab", InteractionMode.AutomatedAction);
        view.menu = player.GetComponent<EquipmentMenuController>();
        root.GetComponentInChildren<EquipmentHUDView>(true).menu = view.menu;
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        PrefabUtility.RecordPrefabInstancePropertyModifications(root.GetComponentInChildren<EquipmentHUDView>(true));
        Undo.RecordObject(player.data, "Set player life to five points"); player.data.maxHP = 5; EditorUtility.SetDirty(player.data);
        foreach (var health in player.GetComponents<Health>()) { Undo.RecordObject(health, "Set player life to five points"); health.maxHP = 5; EditorUtility.SetDirty(health); }
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);
        Selection.activeGameObject = root;
    }

    static void ImportSprites()
    {
        foreach (var path in Directory.GetFiles(Folder + "/Sprites", "*.png"))
        {
            var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Sprites/" + name + ".png");

    static EquipmentUIStyle CreateStyle()
    {
        var result = AssetDatabase.LoadAssetAtPath<EquipmentUIStyle>(Folder + "/Equipment Style.asset");
        if (result != null) return result;
        result = ScriptableObject.CreateInstance<EquipmentUIStyle>();
        result.font = CreateFont();
        result.bag = Sprite("Bag"); result.spiritOwned = Sprite("SpiritOwned"); result.spiritMissing = Sprite("SpiritMissing");
        result.healthFull = Sprite("HealthFull"); result.healthDamaged = Sprite("HealthDamaged"); result.healthOrnament = Sprite("HealthOrnament");
        result.healthPerEye = 1;
        result.entries = new[] {
            new EquipmentPresentation {slot=EquipmentSlot.Weapon,baseName="匕首",enhancedName="长剑",description="随身的短刃，适合近距离出手。灵能让刀锋延伸，也让每次攻击更有力量。",baseIcon=Sprite("Dagger"),enhancedIcon=Sprite("Sword")},
            new EquipmentPresentation {slot=EquipmentSlot.Scarf,baseName="围巾",enhancedName="斗篷",description="一路相伴的围巾。注入灵后化为斗篷，借风在空中再跃一步。",baseIcon=Sprite("Bag"),enhancedIcon=Sprite("Cloak")},
            new EquipmentPresentation {slot=EquipmentSlot.Clothes,baseName="衣服",enhancedName="护甲",description="轻便的衣物。灵为它赋予保护，让旅途中受到的伤害减轻。",baseIcon=Sprite("Clothes"),enhancedIcon=Sprite("Armor")}
        };
        AssetDatabase.CreateAsset(result, Folder + "/Equipment Style.asset");
        return result;
    }

    static TMPro.TMP_FontAsset CreateFont()
    {
        const string path = Folder + "/Fonts/Equipment Chinese SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
        if (existing != null && existing.fallbackFontAssetTable.Count > 0) return existing;
        var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/Fonts/NotoSansCJKsc-Regular.otf");
        var font = TMPro.TMP_FontAsset.CreateFontAsset(source, 40, 4, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, TMPro.AtlasPopulationMode.Dynamic);
        font.name = "Equipment Chinese SDF";
        string strings = string.Concat(Enumerable.Range(32, 95).Select(i => (char)i));
        foreach (var file in new[] {"EquipmentPanelView.cs", "EquipmentHUDView.cs", "EquipmentUIStyle.cs"}) strings += File.ReadAllText("Assets/Scripts/" + file);
        strings += File.ReadAllText("Assets/Editor/EquipmentUIBuilder.cs");
        strings = new string(strings.Where(c => !char.IsControl(c)).Distinct().ToArray());
        string missing;
        font.TryAddCharacters(strings, out missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("UI font missing: " + missing);
        font.atlasPopulationMode = TMPro.AtlasPopulationMode.Static;
        if (existing != null) { UnityEngine.Object.DestroyImmediate(font); font = existing; } else SaveFont(font, path);
        var fallback = TMPro.TMP_FontAsset.CreateFontAsset(source, 40, 4, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, TMPro.AtlasPopulationMode.Dynamic);
        fallback.name = "Equipment Chinese Fallback";
        fallback.TryAddCharacters("中文备用", out missing);
        typeof(TMPro.TMP_FontAsset).GetField("m_ClearDynamicDataOnBuild", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(fallback, true);
        SaveFont(fallback, Folder + "/Fonts/Equipment Chinese Fallback.asset");
        font.fallbackFontAssetTable.Add(fallback); EditorUtility.SetDirty(font);
        return font;
    }

    static void SaveFont(TMPro.TMP_FontAsset font, string path)
    {
        AssetDatabase.CreateAsset(font, path);
        font.material.name = font.name + " Material";
        AssetDatabase.AddObjectToAsset(font.material, font);
        for (int i = 0; i < font.atlasTextures.Length; i++)
        {
            font.atlasTextures[i].name = font.name + " Atlas " + i;
            AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
        }
        EditorUtility.SetDirty(font);
    }

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static void Fill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color, bool raycast = false)
    {
        var rect = Rect(name, parent, position, size); var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite; image.color = color; image.raycastTarget = raycast; image.preserveAspect = sprite != null; return image;
    }
    static TMPro.TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size, string content, float fontSize, Color color, TMPro.TextAlignmentOptions alignment = TMPro.TextAlignmentOptions.Left)
    {
        var rect = Rect(name, parent, position, size); var text = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        text.font = style.font; text.text = content; text.fontSize = fontSize; text.color = color;
        text.alignment = alignment; text.enableAutoSizing = false; text.enableWordWrapping = true;
        text.raycastTarget = false; text.lineSpacing = 6; text.overflowMode = TMPro.TextOverflowModes.Overflow; return text;
    }
    static UnityEngine.UI.Button Button(UnityEngine.UI.Image image)
    {
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.08f);
        colors.pressedColor = new Color(0.85f, 0.82f, 0.7f); colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f); button.colors = colors;
        return button;
    }

    static void BuildHUD(Transform parent)
    {
        var root = Rect("HUD", parent, new Vector2(32, -24), new Vector2(450, 180));
        root.anchorMin = root.anchorMax = new Vector2(0, 1); root.pivot = new Vector2(0, 1);
        var hud = root.gameObject.AddComponent<EquipmentHUDView>(); hud.style = style;
        var ornament = Image("HealthOrnament", root, new Vector2(0, 0), new Vector2(450, 65), style.healthOrnament, Color.white);
        ornament.rectTransform.anchorMin = ornament.rectTransform.anchorMax = new Vector2(0, 1); ornament.rectTransform.pivot = new Vector2(0, 1); ornament.rectTransform.anchoredPosition = Vector2.zero;
        hud.eyes = new UnityEngine.UI.Image[5];
        for (int i = 0; i < 5; i++)
        {
            var eye = Image("Eye" + (i + 1), root, Vector2.zero, new Vector2(36, 40), style.healthFull, Color.white);
            eye.rectTransform.anchorMin = eye.rectTransform.anchorMax = new Vector2(0, 1); eye.rectTransform.anchoredPosition = new Vector2(80 + i * 62, -new float[] {22, 27, 42, 52, 52}[i]);
            hud.eyes[i] = eye;
        }
        var bag = Image("BagButton", root, Vector2.zero, new Vector2(70, 60), style.bag, Color.white, true);
        bag.rectTransform.anchorMin = bag.rectTransform.anchorMax = new Vector2(0, 1); bag.rectTransform.anchoredPosition = new Vector2(53, -110);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(Button(bag).onClick, hud.OpenMenu);
        hud.spiritIcons = new UnityEngine.UI.Image[2];
        for (int i = 0; i < 2; i++)
        {
            var spirit = Image("Spirit" + (i + 1), root, Vector2.zero, new Vector2(30, 43), style.spiritMissing, Color.white);
            spirit.rectTransform.anchorMin = spirit.rectTransform.anchorMax = new Vector2(0, 1); spirit.rectTransform.anchoredPosition = new Vector2(130 + i * 49, -108); hud.spiritIcons[i] = spirit;
        }
        var hint = Text("OpenHint", root, Vector2.zero, new Vector2(270, 26), "I  打开背包", 16, style.mutedColor);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0, 1); hint.rectTransform.pivot = new Vector2(0, 0.5f); hint.rectTransform.anchoredPosition = new Vector2(22, -155);
    }

    static void BuildPanel(Transform parent, EquipmentPanelView view)
    {
        var modal = Rect("EquipmentModal", parent, Vector2.zero, Vector2.zero); Fill(modal); view.panelRoot = modal.gameObject;
        var backdrop = Image("Backdrop", modal, Vector2.zero, Vector2.zero, null, new Color(0.018f, 0.047f, 0.04f, 0.76f), true); Fill(backdrop.rectTransform);
        var border = Image("Border", modal, Vector2.zero, new Vector2(1124, 704), null, style.accentColor);
        var panel = Image("Panel", modal, Vector2.zero, new Vector2(1120, 700), null, PanelColor, true);
        Image("HeaderBag", panel.transform, new Vector2(-481, 279), new Vector2(70, 61), style.bag, Color.white);
        Text("Heading", panel.transform, new Vector2(-297, 285), new Vector2(250, 60), "灵的装卸", 36, style.textColor);
        Text("Subtitle", panel.transform, new Vector2(-298, 238), new Vector2(250, 28), "以灵寄物 · 选择一件装备", 17, style.mutedColor);
        view.summary = Text("Summary", panel.transform, new Vector2(210, 278), new Vector2(420, 40), "已获得 0 / 2    可用 0", 21, style.textColor, TMPro.TextAlignmentOptions.Right);
        var closeImage = Image("CloseButton", panel.transform, new Vector2(501, 280), new Vector2(44, 44), null, style.slotColor, true);
        Text("CloseLabel", closeImage.transform, Vector2.zero, new Vector2(44, 44), "×", 30, style.textColor, TMPro.TextAlignmentOptions.Center);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(Button(closeImage).onClick, view.CloseMenu);
        Image("HeaderRule", panel.transform, new Vector2(0, 215), new Vector2(1040, 1.5f), null, new Color(0.61f, 0.55f, 0.34f, 0.65f));
        Image("ColumnRule", panel.transform, new Vector2(-233, -35), new Vector2(1.5f, 438), null, new Color(0.61f, 0.55f, 0.34f, 0.4f));
        Text("ListLabel", panel.transform, new Vector2(-390, 183), new Vector2(264, 30), "随身装备", 18, style.mutedColor);
        var list = Rect("EquipmentList", panel.transform, new Vector2(-389, -56), new Vector2(264, 426));
        var layout = list.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 14; layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        view.slots = new EquipmentSlotWidgets[3];
        for (int i = 0; i < 3; i++)
        {
            var entry = style.entries[i];
            var bg = Image("Slot" + entry.slot, list, Vector2.zero, new Vector2(264, 128), null, style.slotColor, true);
            var element = bg.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); element.preferredHeight = element.minHeight = 128;
            var button = Button(bg);
            if (i == 0) UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, view.SelectWeapon);
            else if (i == 1) UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, view.SelectScarf);
            else UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, view.SelectClothes);
            var icon = Image("Icon", bg.transform, new Vector2(-80, 0), new Vector2(67, 83), entry.baseIcon, Color.white);
            var name = Text("Name", bg.transform, new Vector2(16, 23), new Vector2(100, 36), entry.baseName, 26, style.textColor);
            var status = Text("Status", bg.transform, new Vector2(28, -18), new Vector2(125, 28), "未装入灵  0 / 1", 15, style.mutedColor);
            var spirit = Image("Spirit", bg.transform, new Vector2(100, 18), new Vector2(22, 31), style.spiritMissing, Color.white);
            var mark = Image("SelectedMark", bg.transform, new Vector2(-129, 0), new Vector2(5, 104), null, style.accentColor); mark.gameObject.SetActive(false);
            view.slots[i] = new EquipmentSlotWidgets {slot=entry.slot,background=bg,icon=icon,spirit=spirit,name=name,status=status,selectedMark=mark.gameObject};
        }
        var right = Rect("Details", panel.transform, new Vector2(158, -29), new Vector2(700, 486));
        var empty = Rect("EmptySelection", right, Vector2.zero, new Vector2(700, 486)); view.emptyDetail = empty.gameObject;
        view.panelSpirits = new UnityEngine.UI.Image[2];
        for (int i = 0; i < 2; i++) view.panelSpirits[i] = Image("CollectedSpirit" + i, empty, new Vector2(-42 + i * 84, 72), new Vector2(52, 74), style.spiritMissing, Color.white);
        Text("Prompt", empty, new Vector2(0, -9), new Vector2(650, 68), "选择加强", 37, style.textColor, TMPro.TextAlignmentOptions.Center);
        Text("Instructions", empty, new Vector2(0, -87), new Vector2(550, 100), "点击左侧装备，查看它的强化效果。\n每件装备只需 1 个灵，最多同时强化 2 件。", 21, style.mutedColor, TMPro.TextAlignmentOptions.Center);
        var detail = Rect("SelectedEquipment", right, Vector2.zero, new Vector2(700, 486)); view.selectedDetail = detail.gameObject; detail.gameObject.SetActive(false);
        view.preview = Image("Preview", detail, new Vector2(-229, 146), new Vector2(139, 146), style.entries[0].baseIcon, Color.white);
        view.title = Text("Title", detail, new Vector2(83, 175), new Vector2(422, 63), "匕首", 43, style.textColor);
        view.upgradeLabel = Text("Upgrade", detail, new Vector2(85, 116), new Vector2(426, 40), "装入灵后化为 长剑", 20, style.accentColor);
        view.description = Text("Description", detail, new Vector2(0, -19), new Vector2(620, 194), "", 22, style.textColor, TMPro.TextAlignmentOptions.TopLeft);
        Image("EffectRule", detail, new Vector2(0, -140), new Vector2(620, 1), null, new Color(0.61f, 0.55f, 0.34f, 0.4f));
        view.requirement = Text("Requirement", detail, new Vector2(0, -167), new Vector2(620, 40), "", 20, style.textColor, TMPro.TextAlignmentOptions.Center);
        var action = Image("ActionButton", detail, new Vector2(0, -223), new Vector2(280, 58), null, style.accentColor, true);
        view.actionButton = Button(action);
        view.actionLabel = Text("ActionLabel", action.transform, Vector2.zero, new Vector2(260, 54), "装入灵", 25, new Color(0.1f, 0.17f, 0.14f), TMPro.TextAlignmentOptions.Center);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(view.actionButton.onClick, view.ApplySpirit);
        view.statusMessage = Text("Feedback", detail, new Vector2(0, -268), new Vector2(650, 32), "", 17, style.mutedColor, TMPro.TextAlignmentOptions.Center);
        Text("Footer", panel.transform, new Vector2(0, -323), new Vector2(1020, 28), "I / Esc  关闭背包         灵可自由装卸与重新分配", 16, style.mutedColor, TMPro.TextAlignmentOptions.Center);
    }
}





