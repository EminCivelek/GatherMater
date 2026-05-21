#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DuelArenaSetup
{
    [MenuItem("GatherMater/Setup Duel Arena UI")]
    public static void Run()
    {
        // ── helpers ───────────────────────────────────────────────────────
        RectTransform MkUI(string n, Transform p)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(p, false);
            return go.GetComponent<RectTransform>();
        }
        void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        TMP_Text Txt(string n, Transform p, int size, Color col, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var rt = MkUI(n, p);
            var t  = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.color = col; t.alignment = align;
            return t;
        }
        Image Img(string n, Transform p, Color col)
        {
            var rt = MkUI(n, p);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = col; return img;
        }

        // ── 0. Find Canvas ─────────────────────────────────────────────────
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[DuelSetup] No Canvas found."); return; }
        var canvasRT = canvas.GetComponent<RectTransform>();

        // ── 1. DuelOpponentRowUI prefab ────────────────────────────────────
        const string prefabFolder = "Assets/Prefabs";
        const string prefabPath   = prefabFolder + "/DuelOpponentRowUI.prefab";
        if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);

        var rowRoot    = new GameObject("DuelOpponentRowUI");
        rowRoot.layer  = 5;
        var rowRT      = rowRoot.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 70);
        rowRoot.AddComponent<Image>().color = new Color(0.16f, 0.18f, 0.24f, 1f);
        var rowHLG = rowRoot.AddComponent<HorizontalLayoutGroup>();
        rowHLG.childControlWidth = true; rowHLG.childControlHeight = true;
        rowHLG.childForceExpandWidth = false; rowHLG.childForceExpandHeight = true;
        rowHLG.spacing = 0; rowHLG.padding = new RectOffset(8, 8, 0, 0);

        // Rank
        var rankRT = MkUI("RankText", rowRoot.transform);
        var rankLE = rankRT.gameObject.AddComponent<LayoutElement>(); rankLE.preferredWidth = 72;
        var rankT  = rankRT.gameObject.AddComponent<TextMeshProUGUI>();
        rankT.fontSize = 26; rankT.color = new Color(0.7f, 0.7f, 0.7f); rankT.alignment = TextAlignmentOptions.MidlineLeft;

        // Name
        var nameRT = MkUI("NameText", rowRoot.transform);
        var nameLE = nameRT.gameObject.AddComponent<LayoutElement>(); nameLE.flexibleWidth = 1;
        var nameT  = nameRT.gameObject.AddComponent<TextMeshProUGUI>();
        nameT.fontSize = 26; nameT.color = Color.white; nameT.alignment = TextAlignmentOptions.MidlineLeft;

        // Power
        var pwRT = MkUI("PowerText", rowRoot.transform);
        var pwLE = pwRT.gameObject.AddComponent<LayoutElement>(); pwLE.preferredWidth = 100;
        var pwT  = pwRT.gameObject.AddComponent<TextMeshProUGUI>();
        pwT.fontSize = 24; pwT.color = new Color(0.6f, 0.9f, 1f); pwT.alignment = TextAlignmentOptions.Midline;

        // Honor
        var hnRT = MkUI("HonorText", rowRoot.transform);
        var hnLE = hnRT.gameObject.AddComponent<LayoutElement>(); hnLE.preferredWidth = 100;
        var hnT  = hnRT.gameObject.AddComponent<TextMeshProUGUI>();
        hnT.fontSize = 24; hnT.color = new Color(1f, 0.85f, 0.2f); hnT.alignment = TextAlignmentOptions.Midline;

        // Duel button
        var btnRT  = MkUI("DuelButton", rowRoot.transform);
        var btnLE  = btnRT.gameObject.AddComponent<LayoutElement>(); btnLE.preferredWidth = 120; btnLE.minWidth = 120;
        var btnImg = btnRT.gameObject.AddComponent<Image>(); btnImg.color = new Color(0.2f, 0.55f, 0.2f);
        var btn    = btnRT.gameObject.AddComponent<Button>(); btn.targetGraphic = btnImg;
        var colors = btn.colors; colors.disabledColor = new Color(0.4f, 0.4f, 0.4f); btn.colors = colors;
        var btnLblRT = MkUI("Text", btnRT);
        Fill(btnLblRT);
        var btnLbl = btnLblRT.gameObject.AddComponent<TextMeshProUGUI>();
        btnLbl.fontSize = 24; btnLbl.color = Color.white; btnLbl.text = "Duel"; btnLbl.alignment = TextAlignmentOptions.Midline;

        var rowScript = rowRoot.AddComponent<DuelOpponentRowUI>();
        var rowSO     = new SerializedObject(rowScript);
        rowSO.FindProperty("rankText").objectReferenceValue    = rankT;
        rowSO.FindProperty("nameText").objectReferenceValue    = nameT;
        rowSO.FindProperty("powerText").objectReferenceValue   = pwT;
        rowSO.FindProperty("honorText").objectReferenceValue   = hnT;
        rowSO.FindProperty("duelButton").objectReferenceValue  = btn;
        rowSO.FindProperty("duelButtonText").objectReferenceValue = btnLbl;
        rowSO.ApplyModifiedPropertiesWithoutUndo();

        var rowPrefab = PrefabUtility.SaveAsPrefabAsset(rowRoot, prefabPath);
        Object.DestroyImmediate(rowRoot);
        Debug.Log($"[DuelSetup] Row prefab saved: {prefabPath}");

        // ── 2. DuelUI panel ────────────────────────────────────────────────
        // Remove existing
        var existing = GameObject.Find("DuelUI");
        if (existing != null) Object.DestroyImmediate(existing);

        // Root (full-screen, invisible)
        var duelRoot   = new GameObject("DuelUI"); duelRoot.layer = 5;
        var duelRootRT = duelRoot.AddComponent<RectTransform>();
        duelRoot.transform.SetParent(canvas.transform, false);
        Fill(duelRootRT);
        var duelScript = duelRoot.AddComponent<DuelUI>();

        // Dim overlay
        var panelGO  = new GameObject("Panel"); panelGO.layer = 5;
        panelGO.transform.SetParent(duelRoot.transform, false);
        var panelRT  = panelGO.AddComponent<RectTransform>(); Fill(panelRT);
        var panelImg = panelGO.AddComponent<Image>(); panelImg.color = new Color(0f, 0f, 0f, 0.65f);
        panelGO.SetActive(false);

        // Card
        var cardRT  = MkUI("Card", panelGO.transform);
        cardRT.anchorMin = new Vector2(0.5f, 0.5f); cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(700, 640);
        var cardImg = cardRT.gameObject.AddComponent<Image>(); cardImg.color = new Color(0.11f, 0.13f, 0.17f);
        var cardVLG = cardRT.gameObject.AddComponent<VerticalLayoutGroup>();
        cardVLG.childControlWidth = true; cardVLG.childControlHeight = false;
        cardVLG.childForceExpandWidth = true; cardVLG.childForceExpandHeight = false;
        cardVLG.spacing = 8; cardVLG.padding = new RectOffset(20, 20, 16, 16);

        // Header row
        var headerRT  = MkUI("Header", cardRT);
        var headerLE  = headerRT.gameObject.AddComponent<LayoutElement>(); headerLE.preferredHeight = 52;
        var headerHLG = headerRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerHLG.childControlWidth = true; headerHLG.childControlHeight = true;
        headerHLG.childForceExpandWidth = false; headerHLG.childForceExpandHeight = true; headerHLG.spacing = 8;

        var titleLE = MkUI("Title", headerRT).gameObject.AddComponent<LayoutElement>(); titleLE.flexibleWidth = 1;
        var titleT  = titleLE.gameObject.AddComponent<TextMeshProUGUI>();
        titleT.text = "Duel Arena"; titleT.fontSize = 34; titleT.fontStyle = FontStyles.Bold;
        titleT.color = Color.white; titleT.alignment = TextAlignmentOptions.MidlineLeft;

        var closeBtnRT  = MkUI("CloseButton", headerRT);
        var closeLE     = closeBtnRT.gameObject.AddComponent<LayoutElement>(); closeLE.preferredWidth = 52; closeLE.preferredHeight = 52;
        var closeBtnImg = closeBtnRT.gameObject.AddComponent<Image>(); closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f);
        var closeBtn    = closeBtnRT.gameObject.AddComponent<Button>(); closeBtn.targetGraphic = closeBtnImg;
        var closeLblRT  = MkUI("X", closeBtnRT); Fill(closeLblRT);
        var closeLbl    = closeLblRT.gameObject.AddComponent<TextMeshProUGUI>();
        closeLbl.text = "✕"; closeLbl.fontSize = 28; closeLbl.color = Color.white; closeLbl.alignment = TextAlignmentOptions.Midline;

        // Honor label
        var honorLblRT = MkUI("HonorLabel", cardRT);
        var honorLE    = honorLblRT.gameObject.AddComponent<LayoutElement>(); honorLE.preferredHeight = 36;
        var honorT     = honorLblRT.gameObject.AddComponent<TextMeshProUGUI>();
        honorT.text = "Your Honor: 0"; honorT.fontSize = 28; honorT.color = new Color(1f, 0.85f, 0.2f);
        honorT.alignment = TextAlignmentOptions.MidlineLeft;

        // Column headers
        var colHdrRT  = MkUI("ColumnHeaders", cardRT);
        var colHdrLE  = colHdrRT.gameObject.AddComponent<LayoutElement>(); colHdrLE.preferredHeight = 36;
        var colHdrHLG = colHdrRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        colHdrHLG.childControlWidth = true; colHdrHLG.childControlHeight = true;
        colHdrHLG.childForceExpandWidth = false; colHdrHLG.childForceExpandHeight = true;
        colHdrHLG.spacing = 0; colHdrHLG.padding = new RectOffset(8, 8, 0, 0);

        Color hdrCol = new Color(0.5f, 0.5f, 0.5f);
        void ColHdr(string label, Transform parent, float width, float flex)
        {
            var rt = MkUI(label, parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            if (flex > 0) le.flexibleWidth = flex; else le.preferredWidth = width;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = 22; t.color = hdrCol;
            t.alignment = flex > 0 ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
        }
        ColHdr("Rank",  colHdrRT, 72,   0);
        ColHdr("Name",  colHdrRT, 0,    1);
        ColHdr("Power", colHdrRT, 100,  0);
        ColHdr("Honor", colHdrRT, 100,  0);
        var sepHdrRT = MkUI("Spacer", colHdrRT);
        sepHdrRT.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;

        // Separator line
        var sepRT  = MkUI("Separator", cardRT);
        var sepLE  = sepRT.gameObject.AddComponent<LayoutElement>(); sepLE.preferredHeight = 1;
        sepRT.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);

        // Rows container (VLG)
        var rowsRT  = MkUI("Rows", cardRT);
        var rowsLE  = rowsRT.gameObject.AddComponent<LayoutElement>(); rowsLE.flexibleHeight = 1;
        var rowsVLG = rowsRT.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsVLG.childControlWidth = true; rowsVLG.childControlHeight = true;
        rowsVLG.childForceExpandWidth = true; rowsVLG.childForceExpandHeight = false;
        rowsVLG.spacing = 4;

        var rowInstances = new DuelOpponentRowUI[5];
        for (int i = 0; i < 5; i++)
        {
            var rowGO = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, rowsRT);
            rowInstances[i] = rowGO.GetComponent<DuelOpponentRowUI>();
            rowGO.SetActive(false);
        }

        // Status text
        var statusRT = MkUI("StatusText", cardRT);
        var statusLE = statusRT.gameObject.AddComponent<LayoutElement>(); statusLE.preferredHeight = 40;
        var statusT  = statusRT.gameObject.AddComponent<TextMeshProUGUI>();
        statusT.text = "Loading..."; statusT.fontSize = 26; statusT.color = new Color(0.7f, 0.7f, 0.7f);
        statusT.alignment = TextAlignmentOptions.Midline; statusT.gameObject.SetActive(false);

        // ── 4. Wire DuelUI fields ─────────────────────────────────────────
        var duelSO = new SerializedObject(duelScript);
        duelSO.FindProperty("panel").objectReferenceValue       = panelGO;
        duelSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
        duelSO.FindProperty("honorLabel").objectReferenceValue  = honorT;
        duelSO.FindProperty("statusText").objectReferenceValue  = statusT;

        var rowsProp = duelSO.FindProperty("opponentRows");
        rowsProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rowInstances[i];

        duelSO.ApplyModifiedPropertiesWithoutUndo();

        // ── 5. HonorManager GO ────────────────────────────────────────────
        var honorGO = GameObject.Find("HonorManager");
        if (honorGO == null)
        {
            honorGO = new GameObject("HonorManager");
            honorGO.AddComponent<HonorManager>();
        }

        // ── 6. DuelArena world building GO ────────────────────────────────
        var arenaGO = GameObject.Find("DuelArena");
        if (arenaGO == null)
        {
            arenaGO = new GameObject("DuelArena");
            arenaGO.transform.position = new Vector3(-70f, 13.78f, 0f);
            var sr = arenaGO.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default"; sr.sortingOrder = 0;
            var arena = arenaGO.AddComponent<DuelArena>();
            var bc    = arenaGO.AddComponent<BoxCollider2D>();
            bc.size   = new Vector2(2f, 2f);
            Debug.Log("[DuelSetup] DuelArena GO placed at (-70, 13.78, 0). Assign a sprite in Inspector.");
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[DuelSetup] Done! Save the scene.");
    }
}
#endif
