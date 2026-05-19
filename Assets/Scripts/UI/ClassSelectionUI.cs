using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClassSelectionUI : MonoBehaviour
{
    public static ClassSelectionUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button     closeButton;

    [Header("Class Buttons")]
    [SerializeField] private Button warriorButton;
    [SerializeField] private Button rogueButton;
    [SerializeField] private Button wizardButton;
    [SerializeField] private Button priestButton;

    [Header("Detail Panel")]
    [SerializeField] private Image           selectedClassIcon;
    [SerializeField] private TextMeshProUGUI selectedClassNameText;
    [SerializeField] private TextMeshProUGUI passivesText;
    [SerializeField] private TextMeshProUGUI pepTalkText;
    [SerializeField] private Button          confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private TextMeshProUGUI costNoticeText;  // repurposed from lockedNotice

    [SerializeField] private GameObject lockedNotice; // kept for wiring; text accessed via costNoticeText

    private PlayerClass _hoveredClass = PlayerClass.None;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        closeButton?.onClick.AddListener(Close);
        warriorButton?.onClick.AddListener(() => PreviewClass(PlayerClass.Warrior));
        rogueButton?.onClick.AddListener(()   => PreviewClass(PlayerClass.Rogue));
        wizardButton?.onClick.AddListener(()  => PreviewClass(PlayerClass.Wizard));
        priestButton?.onClick.AddListener(()  => PreviewClass(PlayerClass.Priest));
        confirmButton?.onClick.AddListener(OnConfirm);
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        var ps = PlayerStats.Instance;
        _hoveredClass = (ps != null && ps.selectedClass != PlayerClass.None)
            ? ps.selectedClass : PlayerClass.None;
        if (_hoveredClass != PlayerClass.None) ShowClassDetail(_hoveredClass);
        else ClearDetail();
        RefreshConfirmState();
        transform.SetAsLastSibling();
    }

    public void Close() { if (panel != null) panel.SetActive(false); }

    // ── Private helpers ───────────────────────────────────────────────────────────
    private void PreviewClass(PlayerClass cls)
    {
        _hoveredClass = cls;
        ShowClassDetail(cls);
        RefreshConfirmState();
    }

    private void ShowClassDetail(PlayerClass cls)
    {
        if (selectedClassNameText != null) selectedClassNameText.text = ClassData.DisplayName(cls);

        if (passivesText != null)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in ClassData.Passives(cls))
                sb.AppendLine($"• {p}");
            passivesText.text = sb.ToString().TrimEnd();
        }

        if (pepTalkText != null) pepTalkText.text = ClassData.PepTalk(cls);
    }

    private void ClearDetail()
    {
        if (selectedClassNameText != null) selectedClassNameText.text = "Choose Your Path";
        if (passivesText          != null) passivesText.text = "";
        if (pepTalkText           != null) pepTalkText.text  = "Select a class to begin your journey.";
    }

    private void RefreshConfirmState()
    {
        var ps  = PlayerStats.Instance;
        var inv = Inventory.Instance;

        bool hasClass  = ps != null && ps.selectedClass != PlayerClass.None;
        bool noneHovered = _hoveredClass == PlayerClass.None;
        bool sameClass = hasClass && _hoveredClass == ps.selectedClass;

        if (noneHovered)
        {
            SetConfirm(false, "Choose This Path");
            SetCostNotice(false, "");
            return;
        }

        if (!hasClass)
        {
            // First time — free
            SetConfirm(true, "Choose This Path");
            SetCostNotice(false, "");
            return;
        }

        if (sameClass)
        {
            SetConfirm(false, "Current Class");
            SetCostNotice(false, "");
            return;
        }

        // Changing class — costs Capacity of every resource
        int cost     = inv != null ? inv.Capacity : 1000;
        bool canAfford = CanAffordChange(cost);
        string btnLabel = canAfford ? "Change Class" : "Cannot Afford";
        SetConfirm(canAfford, btnLabel);
        SetCostNotice(true, BuildCostString(cost, canAfford));
    }

    private void SetConfirm(bool interactable, string label)
    {
        if (confirmButton    != null) confirmButton.interactable = interactable;
        if (confirmButtonText != null) confirmButtonText.text    = label;
    }

    private void SetCostNotice(bool visible, string text)
    {
        if (lockedNotice != null) lockedNotice.SetActive(visible);
        // resolve text component on first use
        if (costNoticeText == null && lockedNotice != null)
            costNoticeText = lockedNotice.GetComponentInChildren<TextMeshProUGUI>();
        if (costNoticeText != null) costNoticeText.text = text;
    }

    private static readonly ResourceType[] ChangeCostResources =
        { ResourceType.Wood, ResourceType.Stone, ResourceType.Gold, ResourceType.Herbs };

    private bool CanAffordChange(int cost)
    {
        var inv = Inventory.Instance;
        if (inv == null) return false;
        foreach (var t in ChangeCostResources)
            if (!inv.Has(t, cost)) return false;
        return true;
    }

    private string BuildCostString(int cost, bool canAfford)
    {
        var inv = Inventory.Instance;
        var sb  = new System.Text.StringBuilder();
        sb.Append("Change cost: ");
        bool first = true;
        foreach (var t in ChangeCostResources)
        {
            if (!first) sb.Append("  ");
            bool ok = inv != null && inv.Has(t, cost);
            sb.Append(ok ? "<color=#88ff88>" : "<color=#ff6666>");
            sb.Append($"{cost} {t}</color>");
            first = false;
        }
        return sb.ToString();
    }

    // ── Actions ───────────────────────────────────────────────────────────────────
    private void OnConfirm()
    {
        if (_hoveredClass == PlayerClass.None) return;
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        bool isFirstTime = ps.selectedClass == PlayerClass.None;
        if (!isFirstTime)
        {
            if (_hoveredClass == ps.selectedClass) return;
            var inv  = Inventory.Instance;
            int cost = inv != null ? inv.Capacity : 0;
            if (!CanAffordChange(cost)) return;
            foreach (var t in ChangeCostResources)
                inv.Spend(t, cost);
        }

        ps.SelectClass(_hoveredClass);
        RefreshConfirmState();
    }
}
