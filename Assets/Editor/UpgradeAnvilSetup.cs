
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class UpgradeAnvilSetup
{
    [MenuItem("GatherMater/Setup Upgrade Anvil UI")]
    public static void Run()
    {
        // ── helpers ────────────────────────────────────────────────────────────
        RectTransform MkUI(string n, Transform p)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(p, false);
            return go.GetComponent<RectTransform>();
        }
        void Fill(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchorMax = Vector2.one;
        }

        // ── 1. Slot prefab ─────────────────────────────────────────────────────
        var slotRoot = new GameObject("UpgradeItemSlotUI");
        slotRoot.layer = 5;
        var slotRT   = slotRoot.AddComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(0, 64);
        slotRoot.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.25f, 1f);
        var slotBtn = slotRoot.AddComponent<Button>();
        var slotHLG = slotRoot.AddComponent<HorizontalLayoutGroup>();
        slotHLG.childControlWidth = true; slotHLG.childControlHeight = true;
        slotHLG.childForceExpandWidth = false; slotHLG.childForceExpandHeight = true;
        slotHLG.spacing = 8; slotHLG.padding = new RectOffset(8, 8, 4, 4);
        var slotCSF = slotRoot.AddComponent<ContentSizeFitter>();
        slotCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // highlight overlay
        var hlGO = new GameObject("Highlight"); hlGO.layer = 5;
        hlGO.transform.SetParent(slotRoot.transform, false);
        var hlRT = hlGO.AddComponent<RectTransform>();
        hlRT.anchorMin = Vector2.zero; hlRT.anchorMax = Vector2.one;
        hlRT.offsetMin = hlRT.offsetMax = Vector2.zero;
        hlGO.AddComponent<Image>().color = new Color(1f, 0.85f, 0.2f, 0.3f);
        hlGO.SetActive(false);

        // icon
        var iconRT = MkUI("Icon", slotRoot.transform);
        var iconLE = iconRT.gameObject.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 52; iconLE.preferredHeight = 52;
        iconLE.minWidth = 52; iconLE.minHeight = 52;
        var iconImg = iconRT.gameObject.AddComponent<Image>();
        iconImg.color = Color.white; iconImg.preserveAspect = true;

        // names column
        var namesRT  = MkUI("NamesColumn", slotRoot.transform);
        namesRT.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var namesVLG = namesRT.gameObject.AddComponent<VerticalLayoutGroup>();
        namesVLG.childControlWidth = true; namesVLG.childControlHeight = true;
        namesVLG.childForceExpandWidth = true; namesVLG.childForceExpandHeight = false;
        namesVLG.spacing = 2;

        var nameTxtRT = MkUI("NameText", namesRT);
        nameTxtRT.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        var nameTxtTMP = nameTxtRT.gameObject.AddComponent<TextMeshProUGUI>();
        nameTxtTMP.text = "Item Name"; nameTxtTMP.fontSize = 20; nameTxtTMP.color = Color.white;
        nameTxtTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var lvlTxtRT = MkUI("LevelText", namesRT);
        lvlTxtRT.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
        var lvlTxtTMP = lvlTxtRT.gameObject.AddComponent<TextMeshProUGUI>();
        lvlTxtTMP.text = "Lv 1"; lvlTxtTMP.fontSize = 16;
        lvlTxtTMP.color = new Color(0.65f, 0.85f, 1f, 1f);
        lvlTxtTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // equipped tag
        var eqRT = MkUI("EquippedTag", slotRoot.transform);
        var eqLE = eqRT.gameObject.AddComponent<LayoutElement>();
        eqLE.preferredWidth = 82; eqLE.minWidth = 82;
        var eqTMP = eqRT.gameObject.AddComponent<TextMeshProUGUI>();
        eqTMP.text = "(Equipped)"; eqTMP.fontSize = 13;
        eqTMP.color = new Color(0.35f, 1f, 0.35f, 1f);
        eqTMP.alignment = TextAlignmentOptions.Midline;
        eqRT.gameObject.SetActive(false);

        // UpgradeItemSlotUI component
        var slotComp = slotRoot.AddComponent<UpgradeItemSlotUI>();
        var slotSO   = new SerializedObject(slotComp);
        slotSO.FindProperty("icon").objectReferenceValue         = iconImg;
        slotSO.FindProperty("nameText").objectReferenceValue     = nameTxtTMP;
        slotSO.FindProperty("levelText").objectReferenceValue    = lvlTxtTMP;
        slotSO.FindProperty("equippedTag").objectReferenceValue  = eqTMP;
        slotSO.FindProperty("selectButton").objectReferenceValue = slotBtn;
        slotSO.FindProperty("highlight").objectReferenceValue    = hlGO;
        slotSO.ApplyModifiedProperties();

        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        var slotPrefab     = PrefabUtility.SaveAsPrefabAsset(slotRoot, "Assets/Prefabs/UpgradeItemSlotUI.prefab");
        var slotPrefabComp = slotPrefab.GetComponent<UpgradeItemSlotUI>();
        Object.DestroyImmediate(slotRoot);

        // ── 2. Remove stale UI root ────────────────────────────────────────────
        var stale = GameObject.Find("UpgradeAnvilUI");
        if (stale != null) Object.DestroyImmediate(stale);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Setup] Canvas not found."); return; }

        // ── 3. Root ───────────────────────────────────────────────────────────
        var rootRT = MkUI("UpgradeAnvilUI", canvas.transform);
        Fill(rootRT);

        // ── 4. Panel (centred card) ───────────────────────────────────────────
        var panelRT = MkUI("Panel", rootRT);
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(960, 700);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.15f, 0.97f);
        var panelVLG = panelRT.gameObject.AddComponent<VerticalLayoutGroup>();
        panelVLG.childControlWidth = true; panelVLG.childControlHeight = true;
        panelVLG.childForceExpandWidth = true; panelVLG.childForceExpandHeight = false;
        panelVLG.spacing = 8; panelVLG.padding = new RectOffset(14, 14, 12, 12);

        // ── 5. Header ─────────────────────────────────────────────────────────
        var hdrRT = MkUI("Header", panelRT);
        var hdrLE = hdrRT.gameObject.AddComponent<LayoutElement>();
        hdrLE.preferredHeight = 56; hdrLE.minHeight = 56;
        var hdrHLG = hdrRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        hdrHLG.childControlWidth = true; hdrHLG.childControlHeight = true;
        hdrHLG.childForceExpandWidth = false; hdrHLG.childForceExpandHeight = true;
        hdrHLG.spacing = 8;

        var titleRT = MkUI("TitleText", hdrRT);
        titleRT.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var titleTMP = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Upgrade Anvil"; titleTMP.fontSize = 30; titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.85f, 0.3f, 1f);
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var closeBtnRT = MkUI("CloseButton", hdrRT);
        var closeBtnLE = closeBtnRT.gameObject.AddComponent<LayoutElement>();
        closeBtnLE.preferredWidth = 56; closeBtnLE.minWidth = 56;
        closeBtnRT.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.15f, 0.15f, 1f);
        var closeBtnBtn = closeBtnRT.gameObject.AddComponent<Button>();
        var closeTxtRT  = MkUI("Text", closeBtnRT); Fill(closeTxtRT);
        var closeTMP    = closeTxtRT.gameObject.AddComponent<TextMeshProUGUI>();
        closeTMP.text = "X"; closeTMP.fontSize = 24; closeTMP.fontStyle = FontStyles.Bold;
        closeTMP.color = Color.white; closeTMP.alignment = TextAlignmentOptions.Center;

        // ── 6. Two-column list area ───────────────────────────────────────────
        var listsRT = MkUI("ListsRow", panelRT);
        var listsLE = listsRT.gameObject.AddComponent<LayoutElement>();
        listsLE.preferredHeight = 470; listsLE.flexibleHeight = 1;
        var listsHLG = listsRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        listsHLG.childControlWidth = true; listsHLG.childControlHeight = true;
        listsHLG.childForceExpandWidth = true; listsHLG.childForceExpandHeight = true;
        listsHLG.spacing = 10;

        RectTransform MakeScrollColumn(string colName, string labelText)
        {
            var colRT  = MkUI(colName, listsRT);
            var colVLG = colRT.gameObject.AddComponent<VerticalLayoutGroup>();
            colVLG.childControlWidth = true; colVLG.childControlHeight = true;
            colVLG.childForceExpandWidth = true; colVLG.childForceExpandHeight = false;
            colVLG.spacing = 4;
            colRT.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.12f, 0.9f);

            var lblRT = MkUI("Label", colRT);
            lblRT.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            var lblTMP = lblRT.gameObject.AddComponent<TextMeshProUGUI>();
            lblTMP.text = labelText; lblTMP.fontSize = 18; lblTMP.fontStyle = FontStyles.Bold;
            lblTMP.color = new Color(0.9f, 0.75f, 0.25f, 1f);
            lblTMP.alignment = TextAlignmentOptions.Center;

            var svRT = MkUI("ScrollView", colRT);
            svRT.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            svRT.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.05f);
            var sv = svRT.gameObject.AddComponent<ScrollRect>();

            var vpRT = MkUI("Viewport", svRT); Fill(vpRT);
            vpRT.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            var mask = vpRT.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;

            var cntRT = MkUI("Content", vpRT);
            cntRT.anchorMin = new Vector2(0, 1); cntRT.anchorMax = new Vector2(1, 1);
            cntRT.pivot = new Vector2(0.5f, 1f); cntRT.offsetMin = cntRT.offsetMax = Vector2.zero;
            var cntVLG = cntRT.gameObject.AddComponent<VerticalLayoutGroup>();
            cntVLG.childControlWidth = true; cntVLG.childControlHeight = true;
            cntVLG.childForceExpandWidth = true; cntVLG.childForceExpandHeight = false;
            cntVLG.spacing = 4; cntVLG.padding = new RectOffset(4, 4, 4, 4);
            var csf = cntRT.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.content = cntRT; sv.viewport = vpRT;
            sv.horizontal = false; sv.vertical = true;
            sv.movementType = ScrollRect.MovementType.Clamped;
            return cntRT;
        }

        var itemContentRT   = MakeScrollColumn("ItemColumn",   "Select Item to Upgrade");
        var scrollContentRT = MakeScrollColumn("ScrollColumn", "Select Upgrade Scroll");

        // ── 7. Selection info bar ─────────────────────────────────────────────
        var selRT = MkUI("SelectionInfo", panelRT);
        var selLE = selRT.gameObject.AddComponent<LayoutElement>();
        selLE.preferredHeight = 50; selLE.minHeight = 50;
        selRT.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.22f, 0.9f);
        var selHLG = selRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        selHLG.childControlWidth = true; selHLG.childControlHeight = true;
        selHLG.childForceExpandWidth = true; selHLG.childForceExpandHeight = true;
        selHLG.spacing = 16; selHLG.padding = new RectOffset(10, 10, 4, 4);

        var selItemRT  = MkUI("SelectedItemText", selRT);
        var selItemTMP = selItemRT.gameObject.AddComponent<TextMeshProUGUI>();
        selItemTMP.text = "Item: —"; selItemTMP.fontSize = 20; selItemTMP.color = Color.white;
        selItemTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var selScrollRT  = MkUI("SelectedScrollText", selRT);
        var selScrollTMP = selScrollRT.gameObject.AddComponent<TextMeshProUGUI>();
        selScrollTMP.text = "Scroll: —"; selScrollTMP.fontSize = 20; selScrollTMP.color = Color.white;
        selScrollTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── 8. Upgrade button ─────────────────────────────────────────────────
        var upgRT = MkUI("UpgradeButton", panelRT);
        var upgLE = upgRT.gameObject.AddComponent<LayoutElement>();
        upgLE.preferredHeight = 60; upgLE.minHeight = 60;
        upgRT.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.55f, 0.18f, 1f);
        var upgBtn = upgRT.gameObject.AddComponent<Button>(); upgBtn.interactable = false;
        var upgTxtRT = MkUI("Text", upgRT); Fill(upgTxtRT);
        var upgTMP   = upgTxtRT.gameObject.AddComponent<TextMeshProUGUI>();
        upgTMP.text = "Upgrade"; upgTMP.fontSize = 26; upgTMP.fontStyle = FontStyles.Bold;
        upgTMP.color = Color.white; upgTMP.alignment = TextAlignmentOptions.Center;

        // ── 9. Wire UpgradeAnvilUI component ──────────────────────────────────
        var uiComp = rootRT.gameObject.AddComponent<UpgradeAnvilUI>();
        var so     = new SerializedObject(uiComp);
        so.FindProperty("panel").objectReferenceValue              = panelRT.gameObject;
        so.FindProperty("closeButton").objectReferenceValue        = closeBtnBtn;
        so.FindProperty("itemListParent").objectReferenceValue     = itemContentRT;
        so.FindProperty("itemSlotPrefab").objectReferenceValue     = slotPrefabComp;
        so.FindProperty("scrollListParent").objectReferenceValue   = scrollContentRT;
        so.FindProperty("scrollSlotPrefab").objectReferenceValue   = slotPrefabComp;
        so.FindProperty("selectedItemText").objectReferenceValue   = selItemTMP;
        so.FindProperty("selectedScrollText").objectReferenceValue = selScrollTMP;
        so.FindProperty("upgradeButton").objectReferenceValue      = upgBtn;

        var hud    = GameObject.Find("Canvas/HUD");
        if (hud != null) so.FindProperty("inventoryHUD").objectReferenceValue = hud;
        var intBtn = GameObject.FindObjectOfType<InteractionButton>(true)?.gameObject;
        if (intBtn != null) so.FindProperty("interactionButton").objectReferenceValue = intBtn;
        so.ApplyModifiedProperties();

        // ── 10. Deactivate panel ──────────────────────────────────────────────
        panelRT.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Setup] UpgradeAnvilUI created and all fields wired.");
    }
}
#endif
