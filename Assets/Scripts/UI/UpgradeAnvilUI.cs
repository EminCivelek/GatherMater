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

    [Header("Mode Tabs")]
    [SerializeField] private Button upgradeTabButton;
    [SerializeField] private Button enchantTabButton;
    [SerializeField] private Image  upgradeTabBg;
    [SerializeField] private Image  enchantTabBg;
    [SerializeField] private TMP_Text itemColumnLabel;
    [SerializeField] private TMP_Text actionButtonText;

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
    private bool                             _skipUnequipWarning;

    // ── Upgrade state ─────────────────────────────────────────────────────────
    private Coroutine    _upgradeCoroutine;
    private bool         _isUpgrading;
    private ItemInstance _upgradingItem;

    // ── Enchant state ─────────────────────────────────────────────────────────
    private Coroutine    _enchantCoroutine;
    private bool         _isEnchanting;
    private ItemInstance _enchantingItem;
    private bool         _enchantMode;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
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
        upgradeButton.onClick.AddListener(OnAction);
        if (upgradeSpeedUpButton != null) upgradeSpeedUpButton.onClick.AddListener(OnSpeedUpClicked);
        if (upgradeTabButton     != null) upgradeTabButton.onClick.AddListener(() => SetMode(false));
        if (enchantTabButton     != null) enchantTabButton.onClick.AddListener(() => SetMode(true));
        _player = FindAnyObjectByType<IsometricPlayerController>();
        ResumeSavedUpgrade();
        ResumeSavedEnchant();
    }

    private void Update()
    {
        if (_anvil != null && _player != null && _player.IsMoving) Close();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void Open(UpgradeAnvil anvil)
    {
        EquipmentUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        DailyMissionBoardUI.Instance?.Close();
        DuelUI.Instance?.Close();

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

        bool anyJobActive = _isUpgrading || _isEnchanting;
        if (upgradeFooter != null) upgradeFooter.SetActive(anyJobActive);

        RefreshTabVisuals();
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

    // ── Mode ──────────────────────────────────────────────────────────────────
    private void SetMode(bool enchant)
    {
        _enchantMode            = enchant;
        _selectedItem           = null;
        _selectedItemIsEquipped = false;
        _selectedScroll         = null;
        _requiredScrollName     = null;

        if (selectedItemText   != null) selectedItemText.text   = "Item: —";
        if (selectedScrollText != null) selectedScrollText.text = "Scroll: —";
        if (scrollColumn       != null) scrollColumn.SetActive(false);

        RefreshTabVisuals();
        BuildItemList();
        RefreshUpgradeButton();
    }

    private void RefreshTabVisuals()
    {
        var activeColor   = new Color(0.18f, 0.55f, 0.18f, 1f);
        var inactiveColor = new Color(0.12f, 0.12f, 0.20f, 1f);

        if (upgradeTabBg != null) upgradeTabBg.color = _enchantMode ? inactiveColor : activeColor;
        if (enchantTabBg != null) enchantTabBg.color  = _enchantMode ? activeColor : inactiveColor;

        if (itemColumnLabel != null)
            itemColumnLabel.text = _enchantMode ? "Select Weapon to Enchant" : "Select Item to Upgrade";

        if (actionButtonText != null)
            actionButtonText.text = _enchantMode ? "Enchant" : "Upgrade";
    }

    // ── List builders ─────────────────────────────────────────────────────────
    private void BuildItemList()
    {
        foreach (var s in _itemSlots) Destroy(s.gameObject);
        _itemSlots.Clear();

        var entries = new List<(ItemInstance item, bool isEquipped, bool isMax)>();

        if (ItemInventory.Instance != null)
        {
            foreach (var item in ItemInventory.Instance.Items)
            {
                if (_enchantMode ? !IsWeapon(item) : !IsEquipmentCategory(item)) continue;
                entries.Add((item, false, !_enchantMode && IsAtMaxLevel(item)));
            }
        }

        if (Equipment.Instance != null)
        {
            foreach (EquipSlot equipSlot in Enum.GetValues(typeof(EquipSlot)))
            {
                var item = Equipment.Instance.GetEquipped(equipSlot);
                if (item == null) continue;
                if (_enchantMode ? !IsWeapon(item) : !IsEquipmentCategory(item)) continue;
                entries.Add((item, true, !_enchantMode && IsAtMaxLevel(item)));
            }
        }

        entries.Sort((a, b) => a.isMax.CompareTo(b.isMax));

        foreach (var (item, isEquipped, isMax) in entries)
        {
            var slot = Instantiate(itemSlotPrefab, itemListParent);
            ItemInstance captured   = item;
            bool         capturedEq = isEquipped;
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
                if (_enchantMode)
                {
                    if (item.data?.category != ItemCategory.EnchantmentScroll) continue;
                }
                else
                {
                    if (item.data?.category != ItemCategory.Scroll) continue;
                    if (item.data.itemName  != _requiredScrollName)  continue;
                }

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
                noScrollText.text = _enchantMode
                    ? "No enchantment scrolls\nin inventory"
                    : (_requiredScrollName != null ? $"You don't have\n{_requiredScrollName}" : "No matching upgrade scroll");
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────────
    private void SelectItem(ItemInstance item, bool isEquipped)
    {
        _selectedItem           = item;
        _selectedItemIsEquipped = isEquipped;
        _selectedScroll         = null;
        _requiredScrollName     = _enchantMode ? null : GetRequiredScrollName(item);

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

        if (_enchantMode)
        {
            if (selectedItemText != null)
                selectedItemText.text = $"Weapon: {_selectedItem.data?.itemName}  Lv{_selectedItem.level}";

            if (selectedScrollText != null)
            {
                if (_selectedScroll != null)
                {
                    float amount  = ComputeEnchantAmount(_selectedItem, _selectedScroll);
                    float rate    = ItemData.GetUpgradeSuccessRate(_selectedItem.level);
                    string typeLbl = _selectedScroll.data?.enchantmentScrollType == EnchantmentType.OnHitBonusDamage
                        ? "On-Hit Dmg" : "On-Hit HP";
                    selectedScrollText.text = $"Effect: {typeLbl} +{amount:F1}  ·  {Mathf.RoundToInt(rate * 100)}% success  ·  Fail = keep old";
                }
                else
                {
                    selectedScrollText.text = "Scroll: —";
                }
            }
            return;
        }

        // Upgrade mode
        int   goldNeeded  = 50 * _selectedItem.level;
        int   targetLevel = _selectedItem.level + 1;
        float upgradeRate = ItemData.GetUpgradeSuccessRate(targetLevel);

        if (selectedItemText != null)
            selectedItemText.text = $"Item: {_selectedItem.data?.itemName}  Lv{_selectedItem.level} → Lv{targetLevel}  (Cost: {goldNeeded} Gold)";

        if (selectedScrollText != null)
        {
            if (_selectedScroll != null)
            {
                string risk = targetLevel <= 3 ? "Safe fail" : targetLevel <= 15 ? "Downgrade on fail" : "Destroy on fail";
                selectedScrollText.text = $"Scroll: {_selectedScroll.data?.itemName}  ·  {Mathf.RoundToInt(upgradeRate * 100)}% success  ·  {risk}";
            }
            else
            {
                selectedScrollText.text = "Scroll: —";
            }
        }
    }

    // ── Action dispatch ───────────────────────────────────────────────────────
    private void OnAction()
    {
        if (_enchantMode) BeginEnchant();
        else              OnUpgrade();
    }

    // ── Upgrade ───────────────────────────────────────────────────────────────
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
        if (!CanAffordGold(_selectedItem))
        {
            ConfirmDialogUI.Instance?.ShowMessage("Not Enough Gold", $"You need {goldCost} Gold to upgrade this item.", "OK");
            return;
        }

        if (!_anvil.StartUpgrade(_selectedItem, _selectedScroll, _selectedItemIsEquipped)) return;

        Inventory.Instance.Spend(ResourceType.Gold, goldCost);

        _upgradingItem    = _selectedItem;
        _upgradeCoroutine = StartCoroutine(UpgradeCoroutine(_upgradingItem));

        ClearSelection();
        BuildItemList();
        RefreshUpgradeButton();
    }

    private IEnumerator UpgradeCoroutine(ItemInstance item)
    {
        _isUpgrading = true;

        int   tgtLvl = item.level + 1;
        float rate   = ItemData.GetUpgradeSuccessRate(tgtLvl);
        if (upgradeFooter         != null) upgradeFooter.SetActive(true);
        if (upgradeFooterIcon     != null) upgradeFooterIcon.sprite   = item.data?.icon;
        if (upgradeFooterNameText != null) upgradeFooterNameText.text = $"{item.data?.itemName}  Lv{item.level} → Lv{tgtLvl}  ({Mathf.RoundToInt(rate * 100)}% success)";
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

        if (upgradeFooter != null) upgradeFooter.SetActive(_isEnchanting);

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

    // ── Enchant ───────────────────────────────────────────────────────────────
    private void BeginEnchant()
    {
        if (_anvil == null || _selectedItem == null || _selectedScroll == null) return;
        if (_isEnchanting || _isUpgrading) return;

        int goldCost = 50 * _selectedItem.level;
        if (!CanAffordGold(_selectedItem))
        {
            ConfirmDialogUI.Instance?.ShowMessage("Not Enough Gold", $"You need {goldCost} Gold to enchant this item.", "OK");
            return;
        }

        if (_selectedItemIsEquipped && ItemInventory.Instance != null && ItemInventory.Instance.IsFull)
        {
            ConfirmDialogUI.Instance?.ShowMessage("Inventory Full", "Free up a slot so the item can be unequipped.", "OK");
            return;
        }

        float enchantAmount = ComputeEnchantAmount(_selectedItem, _selectedScroll);

        if (!_anvil.StartEnchant(_selectedItem, _selectedScroll, enchantAmount, _selectedItemIsEquipped)) return;

        Inventory.Instance.Spend(ResourceType.Gold, goldCost);

        _enchantingItem   = _selectedItem;
        _enchantCoroutine = StartCoroutine(EnchantCoroutine(_enchantingItem));

        ClearSelection();
        BuildItemList();
        RefreshUpgradeButton();
    }

    private IEnumerator EnchantCoroutine(ItemInstance item)
    {
        _isEnchanting = true;

        float rate = ItemData.GetUpgradeSuccessRate(EnchantJobTracker.WeaponLevel);
        if (upgradeFooter         != null) upgradeFooter.SetActive(true);
        if (upgradeFooterIcon     != null) upgradeFooterIcon.sprite   = item.data?.icon;
        if (upgradeFooterNameText != null) upgradeFooterNameText.text = $"Enchanting {item.data?.itemName}  ({Mathf.RoundToInt(rate * 100)}% success)";
        if (upgradeProgressBar    != null) { upgradeProgressBar.minValue = 0f; upgradeProgressBar.maxValue = 1f; }

        while (!EnchantJobTracker.IsComplete)
        {
            if (upgradeProgressBar != null) upgradeProgressBar.value = EnchantJobTracker.Progress;
            if (upgradeTimeText    != null) upgradeTimeText.text     = FormatTime(EnchantJobTracker.Remaining);
            yield return null;
        }

        CompleteEnchant(item);
    }

    private void CompleteEnchant(ItemInstance item)
    {
        bool success;
        if (_anvil != null)
        {
            success = _anvil.FinishEnchant(item);
        }
        else
        {
            int   weaponLevel   = EnchantJobTracker.WeaponLevel;
            var   pendingType   = (EnchantmentType)EnchantJobTracker.PendingType;
            float pendingAmount = EnchantJobTracker.PendingAmount;
            float rate          = ItemData.GetUpgradeSuccessRate(weaponLevel);
            success = UnityEngine.Random.value < rate;
            EnchantJobTracker.Clear();
            if (success)
            {
                item.enchantmentType   = pendingType;
                item.enchantmentAmount = pendingAmount;
                ItemInventory.Instance?.NotifyItemUpgraded();
            }
        }

        _isEnchanting    = false;
        _enchantCoroutine = null;
        _enchantingItem  = null;

        if (upgradeFooter != null) upgradeFooter.SetActive(_isUpgrading);

        if (success)
        {
            string typeLbl = item.enchantmentType == EnchantmentType.OnHitBonusDamage ? "On-Hit Bonus Damage" : "On-Hit HP Recovery";
            ConfirmDialogUI.Instance?.ShowMessage("Enchant Successful!",
                $"{item?.data?.itemName ?? "Weapon"} now has\n{typeLbl} (+{item.enchantmentAmount:F1})!", "OK");
        }
        else
        {
            ConfirmDialogUI.Instance?.ShowMessage("Enchant Failed",
                $"The enchantment failed.\n{item?.data?.itemName ?? "Weapon"} keeps its previous enchantment.", "OK");
        }

        if (panel.activeSelf) BuildItemList();
        RefreshUpgradeButton();
    }

    // ── Persistence resume ────────────────────────────────────────────────────
    private void ResumeSavedUpgrade()
    {
        if (!UpgradeJobTracker.IsActive) return;

        var item = FindUpgradingItem();
        if (item == null) { UpgradeJobTracker.Clear(); return; }

        if (UpgradeJobTracker.IsComplete)
            CompleteUpgrade(item);
        else
        {
            _upgradingItem    = item;
            _isUpgrading      = true;
            _upgradeCoroutine = StartCoroutine(UpgradeCoroutine(item));
        }
    }

    private void ResumeSavedEnchant()
    {
        if (!EnchantJobTracker.IsActive) return;

        string instanceId = EnchantJobTracker.InstanceId;
        var item = ItemInventory.Instance?.Items
            .FirstOrDefault(i => !string.IsNullOrEmpty(instanceId) && i.instanceId == instanceId);

        if (item == null) { EnchantJobTracker.Clear(); return; }

        if (EnchantJobTracker.IsComplete)
            CompleteEnchant(item);
        else
        {
            _enchantingItem   = item;
            _isEnchanting     = true;
            _enchantCoroutine = StartCoroutine(EnchantCoroutine(item));
        }
    }

    private ItemInstance FindUpgradingItem()
    {
        string instanceId    = UpgradeJobTracker.UpgradingInstance;
        string dataId        = UpgradeJobTracker.ItemDataId;
        int    expectedLevel = UpgradeJobTracker.TargetLevel - 1;

        return ItemInventory.Instance?.Items
            .FirstOrDefault(i => !string.IsNullOrEmpty(instanceId) && i.instanceId == instanceId)
            ?? ItemInventory.Instance?.Items
            .FirstOrDefault(i => i.itemDataId == dataId && i.level == expectedLevel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void ClearSelection()
    {
        _selectedItem   = null;
        _selectedScroll = null;
        if (selectedItemText   != null) selectedItemText.text   = "Item: —";
        if (selectedScrollText != null) selectedScrollText.text = "Scroll: —";
        if (scrollColumn       != null) scrollColumn.SetActive(false);
    }

    private void RefreshUpgradeButton()
    {
        if (upgradeButton == null) return;
        bool itemBusy = _selectedItem != null && (
            UpgradeJobTracker.IsBeingUpgraded(_selectedItem.instanceId) ||
            EnchantJobTracker.IsBeingEnchanted(_selectedItem.instanceId));
        upgradeButton.interactable = !_isUpgrading && !_isEnchanting
            && _selectedItem != null && _selectedScroll != null && !itemBusy;
    }

    private static float ComputeEnchantAmount(ItemInstance weapon, ItemInstance scroll)
    {
        if (weapon?.data == null || scroll?.data == null) return 0f;
        float baseAmount = weapon.level * scroll.data.enchantAmountPerLevel;
        return weapon.data.isTwoHanded ? baseAmount : baseAmount * 0.5f;
    }

    private static bool CanAffordGold(ItemInstance item)
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

    private static bool IsWeapon(ItemInstance item) =>
        item?.data?.category == ItemCategory.Weapon;

    private static bool IsAtMaxLevel(ItemInstance item) =>
        item?.data != null && item.level >= item.data.MaxUpgradeLevel;

    private void OnSpeedUpClicked()
    {
        if (_isEnchanting)
        {
            SpeedUpPanelUI.Instance?.Open(item =>
            {
                int minutes = item.data?.speedUpMinutes > 0 ? item.data.speedUpMinutes : 1;
                EnchantJobTracker.ApplySpeedUp(minutes);
            });
        }
        else
        {
            SpeedUpPanelUI.Instance?.Open(item =>
            {
                int minutes = item.data?.speedUpMinutes > 0 ? item.data.speedUpMinutes : 1;
                UpgradeJobTracker.ApplySpeedUp(minutes);
            });
        }
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
