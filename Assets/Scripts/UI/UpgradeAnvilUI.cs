using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeAnvilUI : MonoBehaviour
{
    public static UpgradeAnvilUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button     closeButton;
    [SerializeField] private GameObject inventoryHUD;
    [SerializeField] private GameObject interactionButton;

    [Header("Item List (weapons / armor)")]
    [SerializeField] private Transform         itemListParent;
    [SerializeField] private UpgradeItemSlotUI itemSlotPrefab;

    [Header("Scroll List")]
    [SerializeField] private GameObject        scrollColumn;
    [SerializeField] private GameObject        scrollViewGO;
    [SerializeField] private TMP_Text          noScrollText;
    [SerializeField] private Transform         scrollListParent;
    [SerializeField] private UpgradeItemSlotUI scrollSlotPrefab;

    [Header("Selection Info")]
    [SerializeField] private TMP_Text selectedItemText;
    [SerializeField] private TMP_Text selectedScrollText;
    [SerializeField] private Button   upgradeButton;

    [Header("Upgrade Footer")]
    [SerializeField] private GameObject upgradeFooter;
    [SerializeField] private Image      upgradeFooterIcon;
    [SerializeField] private TMP_Text   upgradeFooterNameText;
    [SerializeField] private Slider     upgradeProgressBar;
    [SerializeField] private TMP_Text   upgradeTimeText;
    [SerializeField] private Button     upgradeSpeedUpButton;

    private UpgradeAnvil                     _anvil;
    private GameObject                       _overlay;
    private ItemInstance                     _selectedItem;
    private bool                             _selectedItemIsEquipped;
    private ItemInstance                     _selectedScroll;
    private string                           _requiredScrollName;
    private readonly List<UpgradeItemSlotUI> _itemSlots   = new();
    private readonly List<UpgradeItemSlotUI> _scrollSlots = new();
    private IsometricPlayerController        _player;
    private Coroutine                        _upgradeCoroutine;
    private bool                             _isUpgrading;
    private ItemInstance                     _upgradingItem;
    private bool                             _skipUnequipWarning;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
        if (upgradeFooter != null) upgradeFooter.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
        upgradeButton.onClick.AddListener(OnUpgrade);
        if (upgradeSpeedUpButton != null) upgradeSpeedUpButton.onClick.AddListener(OnSpeedUpClicked);
        _player = FindAnyObjectByType<IsometricPlayerController>();
        ResumeSavedUpgrade();
    }

    private void Update()
    {
        if (_anvil != null && _player != null && _player.IsMoving) Close();
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open(UpgradeAnvil anvil)
    {
        EquipmentUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        DailyMissionBoardUI.Instance?.Close();

        _anvil                  = anvil;
        _selectedItem           = null;
        _selectedItemIsEquipped = false;
        _selectedScroll         = null;
        _requiredScrollName     = null;

        EnsureOverlay();
        _overlay.SetActive(true);
        _overlay.transform.SetAsLastSibling();
        transform.SetAsLastSibling();
        panel.SetActive(true);
        if (inventoryHUD      != null) inventoryHUD.SetActive(false);
        if (interactionButton != null) interactionButton.SetActive(false);

        if (selectedItemText   != null) selectedItemText.text   = "Item: —";
        if (selectedScrollText != null) selectedScrollText.text = "Scroll: —";
        if (scrollColumn       != null) scrollColumn.SetActive(false);

        if (upgradeFooter != null) upgradeFooter.SetActive(_isUpgrading);

        BuildItemList();
        RefreshUpgradeButton();
    }

    public void Close()
    {
        if (_overlay != null) _overlay.SetActive(false);
        _anvil = null;
        panel.SetActive(false);
        if (inventoryHUD      != null) inventoryHUD.SetActive(true);
        if (interactionButton != null) interactionButton.SetActive(true);
    }

    private void EnsureOverlay()
    {
        if (_overlay != null) return;
        _overlay = new GameObject("CloseOverlay");
        _overlay.transform.SetParent(transform.parent, false);
        var rt = _overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var img = _overlay.AddComponent<UnityEngine.UI.Image>(); img.color = Color.clear;
        var btn = _overlay.AddComponent<UnityEngine.UI.Button>(); btn.targetGraphic = img;
        btn.onClick.AddListener(Close);
        _overlay.SetActive(false);
    }

    // ── List builders ─────────────────────────────────────────────────────────────
    private void BuildItemList()
    {
        foreach (var s in _itemSlots) Destroy(s.gameObject);
        _itemSlots.Clear();

        var entries = new System.Collections.Generic.List<(ItemInstance item, bool isEquipped, bool isMax)>();

        if (ItemInventory.Instance != null)
        {
            foreach (var item in ItemInventory.Instance.Items)
            {
                if (!IsEquipmentCategory(item)) continue;
                entries.Add((item, false, IsAtMaxLevel(item)));
            }
        }

        if (Equipment.Instance != null)
        {
            foreach (EquipSlot equipSlot in Enum.GetValues(typeof(EquipSlot)))
            {
                var item = Equipment.Instance.GetEquipped(equipSlot);
                if (item == null || !IsEquipmentCategory(item)) continue;
                entries.Add((item, true, IsAtMaxLevel(item)));
            }
        }

        // Non-max items first, max-level items at the bottom
        entries.Sort((a, b) => a.isMax.CompareTo(b.isMax));

        foreach (var (item, isEquipped, isMax) in entries)
        {
            var slot = Instantiate(itemSlotPrefab, itemListParent);
            ItemInstance captured    = item;
            bool         capturedEq  = isEquipped;
            slot.Init(item, isEquipped, isMax ? null : () => SelectItem(captured, capturedEq), isMax);
            _itemSlots.Add(slot);
        }
    }

    private void BuildScrollList()
    {
        foreach (var s in _scrollSlots) Destroy(s.gameObject);
        _scrollSlots.Clear();

        if (ItemInventory.Instance != null)
        {
            foreach (var item in ItemInventory.Instance.Items)
            {
                if (item.data?.category != ItemCategory.Scroll) continue;
                if (item.data.itemName  != _requiredScrollName)  continue;
                var slot = Instantiate(scrollSlotPrefab, scrollListParent);
                ItemInstance captured = item;
                slot.Init(item, false, () => SelectScroll(captured));
                _scrollSlots.Add(slot);
            }
        }

        bool hasScrolls = _scrollSlots.Count > 0;
        if (scrollViewGO != null) scrollViewGO.SetActive(hasScrolls);
        if (noScrollText != null)
        {
            noScrollText.gameObject.SetActive(!hasScrolls);
            if (!hasScrolls)
                noScrollText.text = _requiredScrollName != null
                    ? $"You don't have\n{_requiredScrollName}"
                    : "No matching upgrade scroll";
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────────────
    private void SelectItem(ItemInstance item, bool isEquipped)
    {
        _selectedItem           = item;
        _selectedItemIsEquipped = isEquipped;
        _selectedScroll         = null;
        _requiredScrollName     = GetRequiredScrollName(item);

        foreach (var s in _itemSlots)
            s.SetHighlight(s.BoundItem == item);
        if (selectedScrollText != null) selectedScrollText.text = "Scroll: —";

        if (scrollColumn != null) scrollColumn.SetActive(true);
        BuildScrollList();
        RefreshSelectionInfo();
        RefreshUpgradeButton();
    }

    private void SelectScroll(ItemInstance scroll)
    {
        _selectedScroll = scroll;

        foreach (var s in _scrollSlots)
            s.SetHighlight(s.BoundItem == scroll);

        RefreshSelectionInfo();
        RefreshUpgradeButton();
    }

    private void RefreshSelectionInfo()
    {
        if (_selectedItem == null)
        {
            if (selectedItemText   != null) selectedItemText.text   = "Item: —";
            if (selectedScrollText != null) selectedScrollText.text = "Scroll: —";
            return;
        }

        int   goldNeeded  = 50 * _selectedItem.level;
        int   targetLevel = _selectedItem.level + 1;
        float rate        = ItemData.GetUpgradeSuccessRate(targetLevel);

        if (selectedItemText != null)
            selectedItemText.text = $"Item: {_selectedItem.data?.itemName}  Lv{_selectedItem.level} → Lv{targetLevel}  (Cost: {goldNeeded} Gold)";

        if (selectedScrollText != null)
        {
            if (_selectedScroll != null)
            {
                string risk = targetLevel <= 3 ? "Safe fail" : targetLevel <= 15 ? "Downgrade on fail" : "Destroy on fail";
                selectedScrollText.text = $"Scroll: {_selectedScroll.data?.itemName}  ·  {Mathf.RoundToInt(rate * 100)}% success  ·  {risk}";
            }
            else
            {
                selectedScrollText.text = "Scroll: —";
            }
        }
    }

    // ── Upgrade ───────────────────────────────────────────────────────────────────
    private void OnUpgrade()
    {
        if (_anvil == null || _selectedItem == null || _selectedScroll == null) return;
        if (_isUpgrading) return;

        if (_selectedItemIsEquipped)
        {
            if (ItemInventory.Instance != null && ItemInventory.Instance.IsFull)
            {
                ConfirmDialogUI.Instance?.ShowMessage(
                    "Inventory Full",
                    "Your inventory is full.\nFree up a slot so the item can be unequipped before upgrading.",
                    "OK");
                return;
            }

            if (_skipUnequipWarning) { BeginUpgrade(); return; }

            ConfirmDialogUI.Instance?.ShowConfirm(
                "Item Will Be Unequipped",
                "This item will be removed from your equipment slot for the duration of the upgrade.\n\nRemember to re-equip it once the upgrade is complete.",
                OnUnequipWarningConfirmed,
                "Continue", "Cancel",
                showDontAskAgain: true);
            return;
        }

        BeginUpgrade();
    }

    private void OnUnequipWarningConfirmed()
    {
        if (ConfirmDialogUI.Instance != null && ConfirmDialogUI.Instance.DontShowAgainChecked)
            _skipUnequipWarning = true;
        BeginUpgrade();
    }

    private void BeginUpgrade()
    {
        if (_anvil == null || _selectedItem == null || _selectedScroll == null) return;
        if (_isUpgrading) return;

        int goldCost = 50 * _selectedItem.level;
        if (!CanAffordUpgradeGold(_selectedItem))
        {
            ConfirmDialogUI.Instance?.ShowMessage("Not Enough Gold", $"You need {goldCost} Gold to upgrade this item.", "OK");
            return;
        }

        if (!_anvil.StartUpgrade(_selectedItem, _selectedScroll, _selectedItemIsEquipped)) return;

        Inventory.Instance.Spend(ResourceType.Gold, goldCost);

        _upgradingItem = _selectedItem;
        _upgradeCoroutine = StartCoroutine(UpgradeCoroutine(_upgradingItem));

        _selectedItem   = null;
        _selectedScroll = null;
        if (selectedItemText   != null) selectedItemText.text   = "Item: —";
        if (selectedScrollText != null) selectedScrollText.text = "Scroll: —";
        if (scrollColumn       != null) scrollColumn.SetActive(false);

        BuildItemList();
        RefreshUpgradeButton();
    }

    private IEnumerator UpgradeCoroutine(ItemInstance item)
    {
        _isUpgrading = true;

        int   _tgtLvl = item.level + 1;
        float _rate   = ItemData.GetUpgradeSuccessRate(_tgtLvl);
        if (upgradeFooter         != null) upgradeFooter.SetActive(true);
        if (upgradeFooterIcon     != null) upgradeFooterIcon.sprite     = item.data?.icon;
        if (upgradeFooterNameText != null) upgradeFooterNameText.text   = $"{item.data?.itemName}  Lv{item.level} → Lv{_tgtLvl}  ({Mathf.RoundToInt(_rate * 100)}% success)";
        if (upgradeProgressBar    != null) { upgradeProgressBar.minValue = 0f; upgradeProgressBar.maxValue = 1f; }

        while (!UpgradeJobTracker.IsComplete)
        {
            if (upgradeProgressBar != null) upgradeProgressBar.value = UpgradeJobTracker.Progress;
            if (upgradeTimeText    != null) upgradeTimeText.text     = FormatTime(UpgradeJobTracker.Remaining);
            yield return null;
        }

        CompleteUpgrade(item);
    }

    private void CompleteUpgrade(ItemInstance item)
    {
        UpgradeResult result;
        if (_anvil != null)
        {
            result = _anvil.FinishUpgrade(item);
        }
        else
        {
            int   targetLevel = UpgradeJobTracker.TargetLevel;
            float rate        = ItemData.GetUpgradeSuccessRate(targetLevel);
            bool  success     = UnityEngine.Random.value < rate;
            UpgradeJobTracker.Clear();
            result = UpgradeAnvil.ApplyOutcome(item, targetLevel, success);
        }

        _isUpgrading      = false;
        _upgradeCoroutine = null;
        _upgradingItem    = null;

        if (upgradeFooter != null) upgradeFooter.SetActive(false);

        ShowUpgradeResult(result, item);

        if (panel.activeSelf) BuildItemList();
        RefreshUpgradeButton();
    }

    private static void ShowUpgradeResult(UpgradeResult result, ItemInstance item)
    {
        string name = item?.data?.itemName ?? "Item";
        switch (result)
        {
            case UpgradeResult.Success:
                ConfirmDialogUI.Instance?.ShowMessage("Upgrade Successful!", $"{name} has been enhanced to +{item?.level}!", "OK");
                break;
            case UpgradeResult.SafeFail:
                ConfirmDialogUI.Instance?.ShowMessage("Upgrade Failed", $"The upgrade failed, but {name} was unharmed.", "OK");
                break;
            case UpgradeResult.Downgrade:
                ConfirmDialogUI.Instance?.ShowMessage("Upgrade Failed!", $"The upgrade failed!\n{name} dropped to +{item?.level}.", "OK");
                break;
            case UpgradeResult.Destroy:
                ConfirmDialogUI.Instance?.ShowMessage("Item Destroyed!", $"The upgrade failed!\n{name} has been destroyed.", "OK");
                break;
        }
    }

    // ── Persistence resume ────────────────────────────────────────────────────────
    private void ResumeSavedUpgrade()
    {
        if (!UpgradeJobTracker.IsActive) return;

        var item = FindUpgradingItem();
        if (item == null) { UpgradeJobTracker.Clear(); return; }

        if (UpgradeJobTracker.IsComplete)
        {
            CompleteUpgrade(item);
        }
        else
        {
            _upgradingItem    = item;
            _isUpgrading      = true;
            _upgradeCoroutine = StartCoroutine(UpgradeCoroutine(item));
        }
    }

    private ItemInstance FindUpgradingItem()
    {
        string instanceId   = UpgradeJobTracker.UpgradingInstance;
        string dataId       = UpgradeJobTracker.ItemDataId;
        int    expectedLevel = UpgradeJobTracker.TargetLevel - 1;

        // Prefer exact instanceId match; fall back to itemDataId+level for legacy saves
        return ItemInventory.Instance?.Items
            .FirstOrDefault(i => !string.IsNullOrEmpty(instanceId) && i.instanceId == instanceId)
            ?? ItemInventory.Instance?.Items
            .FirstOrDefault(i => i.itemDataId == dataId && i.level == expectedLevel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────
    private void RefreshUpgradeButton()
    {
        if (upgradeButton == null) return;
        upgradeButton.interactable = !_isUpgrading && _selectedItem != null && _selectedScroll != null;
    }

    private static bool CanAffordUpgradeGold(ItemInstance item)
    {
        int cost = 50 * item.level;
        return Inventory.Instance != null && Inventory.Instance.Has(ResourceType.Gold, cost);
    }

    private static string GetRequiredScrollName(ItemInstance item)
    {
        if (item?.data == null) return null;
        return item.data.itemTier switch
        {
            ItemTier.Wooden      => "Wooden Upgrade Scroll",
            ItemTier.Iron        => "Iron Upgrade Scroll",
            ItemTier.Steel       => "Steel Upgrade Scroll",
            ItemTier.RizeanSteel => "Rizean Steel Upgrade Scroll",
            _                    => null
        };
    }

    private static bool IsEquipmentCategory(ItemInstance item)
    {
        if (item?.data == null) return false;
        return item.data.category == ItemCategory.Weapon ||
               item.data.category == ItemCategory.Shield ||
               item.data.category == ItemCategory.Armor;
    }

    private static bool IsAtMaxLevel(ItemInstance item) =>
        item?.data != null && item.level >= item.data.MaxUpgradeLevel;

    private void OnSpeedUpClicked()
    {
        SpeedUpPanelUI.Instance?.Open(item =>
        {
            int minutes = item.data?.speedUpMinutes > 0 ? item.data.speedUpMinutes : 1;
            UpgradeJobTracker.ApplySpeedUp(minutes);
        });
    }

    private static string FormatTime(float seconds)
    {
        if (seconds >= 60f)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.CeilToInt(seconds % 60f);
            return s > 0 ? $"{m}m {s}s" : $"{m}m";
        }
        return $"{Mathf.CeilToInt(seconds)}s";
    }
}
