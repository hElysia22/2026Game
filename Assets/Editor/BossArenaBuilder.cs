using UnityEngine;
using UnityEditor;

public static class BossArenaBuilder
{
    [MenuItem("Tools/Game/Configure Boss Arena")]
    public static void Configure()
    {
        if (Application.isPlaying) return;
        var parent = GameObject.Find("CavernLevel");
        if (parent == null || parent.transform.Find("BossArena") != null) return;
        // Boss 已改为 BossAI（只保留待机 / 攻击两个状态），这里只取现成的 Boss。
        var bossGo = GameObject.Find("Boss");
        if (bossGo == null)
        {
            Debug.LogWarning("BossArenaBuilder: 场景里找不到名为 Boss 的对象，先创建 Boss 再执行。");
            return;
        }
        var bossAi = bossGo.GetComponent<BossAI>();
        if (bossAi == null)
        {
            Debug.LogWarning("BossArenaBuilder: Boss 上没有 BossAI 组件。");
            return;
        }
        var root = new GameObject("BossArena"); root.transform.SetParent(parent.transform, false);
        var arena = root.AddComponent<BossArena>(); arena.player = Object.FindObjectOfType<PlayerController>(); arena.boss = bossAi;
        arena.manager = Object.FindObjectOfType<GameManager>(); arena.combatBounds = new Rect(74.35f,0,23.3f,11.5f);
        var camera = Object.FindObjectOfType<Unity.Cinemachine.CinemachineCamera>();
        arena.arenaCamera = camera.GetComponent<BossArenaCamera>();
        if (arena.arenaCamera == null) arena.arenaCamera = camera.gameObject.AddComponent<BossArenaCamera>();
        arena.arenaCamera.outputCamera = Camera.main; arena.arenaCamera.viewBounds = new Rect(74,-1,24,13);
        var trigger = new GameObject("EntryZone", typeof(BoxCollider2D)); trigger.transform.SetParent(root.transform, false);
        trigger.transform.position = new Vector3(86.1f,5,0); arena.entryZone = trigger.GetComponent<BoxCollider2D>();
        arena.entryZone.isTrigger = true; arena.entryZone.size = new Vector2(21.8f,10);
        var gates = new GameObject("BattleBarriers"); gates.transform.SetParent(root.transform, false); arena.barriers = gates;
        var soil = Object.FindObjectOfType<GroundStrip>().bottomLayer.sharedMaterial;
        foreach (float x in new[]{74f,98f})
        {
            var gate = new GameObject(x==74?"LeftGate":"RightGate"); gate.transform.SetParent(gates.transform,false); gate.transform.position=new Vector3(x,0,0);
            var g = gate.AddComponent<GroundStrip>(); g.width=.65f; g.surfaceY=12; g.depth=12.5f; g.soilTopOffset=0;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name="Mud"; quad.transform.SetParent(gate.transform,false); Object.DestroyImmediate(quad.GetComponent<Collider>());
            g.bottomLayer=quad.GetComponent<MeshRenderer>(); g.bottomLayer.sharedMaterial=soil;
            var col = new GameObject("GateCollider",typeof(BoxCollider2D)); col.transform.SetParent(gate.transform,false); col.layer=LayerMask.NameToLayer("Ground");g.groundCollider=col.GetComponent<BoxCollider2D>();g.Refresh();
        }
        var ceiling = new GameObject("ArenaCeiling",typeof(BoxCollider2D)); ceiling.transform.SetParent(gates.transform,false);
        ceiling.layer=LayerMask.NameToLayer("Ground");ceiling.transform.position=new Vector3(86,12,0);ceiling.GetComponent<BoxCollider2D>().size=new Vector2(24,.3f);
        gates.SetActive(false); bossGo.SetActive(false);
        var view = Object.FindObjectOfType<PauseMenuView>();
        if (view.resultLabel == null)
        {
            var label = new GameObject("ResultLabel",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI)); label.transform.SetParent(view.panel.transform,false);
            var rt=label.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(0,175);rt.sizeDelta=new Vector2(600,80);
            var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Equipment/Fonts/Equipment Chinese SDF.asset");
            text.fontSize=48;text.color=new Color(.32f,.25f,.06f);text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;text.text="关卡完成";
            var fill=new GameObject("ResultHeadingBackground",typeof(RectTransform),typeof(UnityEngine.UI.Image));fill.transform.SetParent(view.panel.transform,false);fill.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
            var fillRT=fill.GetComponent<RectTransform>();fillRT.anchorMin=fillRT.anchorMax=fillRT.pivot=new Vector2(.5f,.5f);fillRT.anchoredPosition=new Vector2(0,175);fillRT.sizeDelta=new Vector2(370,105);
            fill.GetComponent<UnityEngine.UI.Image>().color=new Color(212f/255,214f/255,95f/255,1);fill.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;fill.SetActive(false);view.resultHeadingBackground=fill;
            view.resultLabel=text;label.SetActive(false);EditorUtility.SetDirty(view);
        }
        EditorUtility.SetDirty(arena); EditorUtility.SetDirty(arena.arenaCamera);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
