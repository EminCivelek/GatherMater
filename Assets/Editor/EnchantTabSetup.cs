#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds "Upgrade | Enchant" tab buttons to the existing UpgradeAnvilUI panel and wires
/// the new serialized fields. Safe to re-run — removes old tabs before adding new ones.
/// Run via GatherMater → Add Enchant Tabs to Anvil UI.
/// </summary>
public static class EnchantTabSetup
{
    [MenuItem("GatherMater/Add Enchant Tabs to Anvil UI")]
    public static void Run()
    {
        var uiRoot = GameObject.Find("UpgradeAnvilUI");
        if (uiRoot == null) { Debug.LogError("[EnchantTabSetup] UpgradeAnvilUI not found in scene."); return; }

        var uiComp = uiRoot.GetComponent<UpgradeAnvilUI>();
        if (uiComp == null) { Debug.LogError("[EnchantTabSetup] UpgradeAnvilUI component not found."); return; }

        // ── Find panel hierarchy ──────────────────────────────────────────────
        var panel = uiRoot.transform.Find("Panel");
        if (panel == null) { Debug.LogError("[EnchantTabSetup] Panel not found."); return; }

        var header = panel.Find("Header");
        if (header == null) { Debug.LogError("[EnchantTabSetup] Header not found."); return; }

        // Wire ItemColumn label
        var itemColumn = panel.Find("ListsRow/ItemColumn");
        var itemLabel  = itemColumn?.Find("Label")?.GetComponent<TextMeshProUGUI>();

        // Wire action button text
        var actionBtnText = panel.Find("UpgradeButton/Text")?.GetComponent<TextMeshProUGUI>();

        // Wire scrollColumn and scrollViewGO
        var scrollColumnT = panel.Find("ListsRow/ScrollColumn");
        var scrollViewT   = scrollColumnT?.Find("ScrollView");
        var scrollLabelT  = scrollColumnT?.Find("Label")?.GetComponent<TextMeshProUGUI>();

        // ── Remove old tabs if they exist ─────────────────────────────────────
        var old = header.Find("TabRow");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // ── Create tab row ────────────────────────────────────────────────────
        var tabRowGO = new GameObject("TabRow", typeof(RectTransform));
        tabRowGO.layer = 5;
        tabRowGO.transform.SetParent(header, false);
        // Insert between TitleText and CloseButton — move to index 1
        tabRowGO.transform.SetSiblingIndex(1);

        var tabRowRT = tabRowGO.GetComponent<RectTransform>();
        tabRowRT.sizeDelta = new Vector2(224, 0);
        var tabRowLE = tabRowGO.AddComponent<LayoutElement>();
        tabRowLE.preferredWidth = 224; tabRowLE.minWidth = 224;
        var tabRowHLG = tabRowGO.AddComponent<HorizontalLayoutGroup>();
        tabRowHLG.childControlWidth = true; tabRowHLG.childControlHeight = true;
        tabRowHLG.childForceExpandWidth = true; tabRowHLG.childForceExpandHeight = true;
        tabRowHLG.spacing = 4;

        Button upgTab, enchTab;
        Image  upgTabBg, enchTabBg;

        (upgTab, upgTabBg)  = MakeTab("UpgradeTab",  "Upgrade", tabRowGO.transform, new Color(0.18f, 0.55f, 0.18f, 1f));
        (enchTab, enchTabBg) = MakeTab("EnchantTab", "Enchant", tabRowGO.transform, new Color(0.12f, 0.12f, 0.20f, 1f));

        // ── Wire all fields ───────────────────────────────────────────────────
        var so = new SerializedObject(uiComp);

        so.FindProperty("upgradeTabButton").objectReferenceValue = upgTab;
        so.FindProperty("enchantTabButton").objectReferenceValue  = enchTab;
        so.FindProperty("upgradeTabBg").objectReferenceValue      = upgTabBg;
        so.FindProperty("enchantTabBg").objectReferenceValue      = enchTabBg;

        if (itemLabel     != null) so.FindProperty("itemColumnLabel").objectReferenceValue  = itemLabel;
        if (actionBtnText != null) so.FindProperty("actionButtonText").objectReferenceValue = actionBtnText;
        if (scrollColumnT != null) so.FindProperty("scrollColumn").objectReferenceValue     = scrollColumnT.gameObject;
        if (scrollViewT   != null) so.FindProperty("scrollViewGO").objectReferenceValue     = scrollViewT.gameObject;

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnchantTabSetup] Done — Upgrade/Enchant tabs added and wired.");
    }

    static (Button btn, Image bg) MakeTab(string name, string label, Transform parent, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.layer = 5;
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 18; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;

        return (btn, img);
    }
}
#endif
