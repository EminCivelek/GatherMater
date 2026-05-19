using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen item info panel opened when the player taps an inventory item.
/// Shows stats relevant to the item category and provides action buttons.
/// </summary>
public class ItemInfoPanelUI : MonoBehaviour
{
    public static ItemInfoPanelUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button     closeButton;

    [Header("Identity")]
    [SerializeField] private Image           itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI slotText;

    [Header("Weapon Stats")]
    [SerializeField] private GameObject      weaponStatsGroup;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;

    [Header("Armor Stats")]
    [SerializeField] private GameObject      armorStatsGroup;
    [SerializeField] private TextMeshProUGUI armorText;

    [Header("On-Hit Effects")]
    [SerializeField] private GameObject      onHitGroup;
    [SerializeField] private TextMeshProUGUI onHitBonusDamageText;
    [SerializeField] private TextMeshProUGUI onHitHPRecoveryText;

    [Header("Tags")]
    [SerializeField] private GameObject twoHandedTag;
    [SerializeField] private GameObject offHandTag;

    [Header("Action Buttons")]
    [SerializeField] private GameObject actionButtonsGroup;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Button disenchantButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button destroyButton;

    [Header("Crafting Action Buttons")]
    [SerializeField] private GameObject craftingActionButtonsGroup;
    [SerializeField] private Button     craftingCraftButton;

    private ItemInstance _item;
    private Action       _onCraft;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
        equipButton.onClick.AddListener(OnEquip);
        useButton?.onClick.AddListener(OnUseConsumable);
        disenchantButton.onClick.AddListener(OnDisenchant);
        sellButton.onClick.AddListener(OnSell);
        destroyButton.onClick.AddListener(OnDestroyItem);
        craftingCraftButton?.onClick.AddListener(OnCraft);
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open(ItemInstance item)
    {
        _item    = item;
        _onCraft = null;
        if (actionButtonsGroup         != null) actionButtonsGroup.SetActive(true);
        if (craftingActionButtonsGroup != null) craftingActionButtonsGroup.SetActive(false);
        Populate(item);
        transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    public void OpenForCraft(ItemInstance item, Action onCraft)
    {
        _item    = item;
        _onCraft = onCraft;
        if (actionButtonsGroup         != null) actionButtonsGroup.SetActive(false);
        if (craftingActionButtonsGroup != null) craftingActionButtonsGroup.SetActive(true);
        Populate(item);
        transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        _item    = null;
        _onCraft = null;
    }

    // ── Populate ──────────────────────────────────────────────────────────────────
    private void Populate(ItemInstance item)
    {
        var data = item.data;

        // Identity
        if (itemIcon     != null) itemIcon.sprite   = data?.icon;
        if (itemNameText != null) itemNameText.text  = data?.itemName ?? "Unknown";
        if (levelText    != null) levelText.text     = item.IsStackable ? $"x{item.stackCount}" : $"Level  {item.level}";
        if (categoryText != null) categoryText.text  = data?.category.ToString() ?? "";
        if (slotText     != null) slotText.text      = data?.primarySlot.ToString() ?? "";

        bool isPotion         = data?.category == ItemCategory.Potion;
        bool isScroll         = data?.category == ItemCategory.Scroll;
        bool isSpeedUp        = data?.category == ItemCategory.SpeedUp;
        bool isWeaponOrShield = item.IsWeapon || item.IsShield;
        bool isArmor          = data?.category == ItemCategory.Armor;

        if (equipButton      != null) equipButton.gameObject.SetActive(!isPotion && !isScroll && !isSpeedUp);
        if (useButton        != null) useButton.gameObject.SetActive(isPotion || isSpeedUp);
        if (disenchantButton != null) disenchantButton.gameObject.SetActive(isPotion);
        if (sellButton       != null) sellButton.gameObject.SetActive(!isPotion && !isSpeedUp);

        // Weapon stats
        if (weaponStatsGroup != null) weaponStatsGroup.SetActive(isWeaponOrShield);
        if (isWeaponOrShield)
        {
            if (attackText      != null) attackText.text      = $"Attack:  {item.GetAttack():F1}";
            if (attackSpeedText != null) attackSpeedText.text = $"Atk Speed:  {item.GetAttackSpeed():F2}";
        }

        // Armor stats
        if (armorStatsGroup != null) armorStatsGroup.SetActive(isArmor || item.IsShield);
        if (isArmor || item.IsShield)
        {
            if (armorText != null) armorText.text = $"Armor:  {item.GetArmor():F1}";
        }

        // On-hit effects — only show group if any value is non-zero
        bool hasOnHit = data != null && (data.onHitBonusDamage > 0f || data.onHitHPRecovery > 0f);
        if (onHitGroup != null) onHitGroup.SetActive(hasOnHit);
        if (hasOnHit)
        {
            if (onHitBonusDamageText != null) onHitBonusDamageText.text = $"On-Hit Damage:  +{data.onHitBonusDamage:F1}";
            if (onHitHPRecoveryText  != null) onHitHPRecoveryText.text  = $"On-Hit HP:  +{data.onHitHPRecovery:F1}";
        }

        // Tags
        if (twoHandedTag != null) twoHandedTag.SetActive(item.IsTwoHanded);
        if (offHandTag   != null) offHandTag.SetActive(item.CanEquipOffHand);
    }

    // ── Actions ───────────────────────────────────────────────────────────────────
    private void OnEquip()
    {
        if (_item == null) return;
        EquipmentUI.Instance?.EquipItem(_item);
        Close();
    }

    private void OnUseConsumable()
    {
        if (_item?.data == null) return;
        if (_item.data.category == ItemCategory.SpeedUp) { OnUseSpeedUp(); return; }
        OnUsePotion();
    }

    private void OnUsePotion()
    {
        if (_item?.data == null) return;
        PlayerStats.Instance?.ApplyPotion(_item.data);
        if (_item.stackCount > 1)
        {
            ItemInventory.Instance?.RemoveOne(_item);
            Populate(_item);   // refresh count display, keep panel open
        }
        else
        {
            ItemInventory.Instance?.Remove(_item);
            Close();
        }
    }

    private void OnUseSpeedUp()
    {
        if (_item?.data == null) return;
        int minutes = _item.data.speedUpMinutes > 0 ? _item.data.speedUpMinutes : 1;

        bool applied = false;

        // Try crafting first, then upgrade
        var craftingUI = CraftingUI.Instance;
        if (craftingUI != null)
        {
            var stationField = typeof(CraftingUI).GetField("_station", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var station = stationField?.GetValue(craftingUI) as CraftingStation;
            if (station != null && station.ActiveJobs.Count > 0)
            {
                station.ApplySpeedUp(minutes);
                applied = true;
            }
        }

        if (!applied && UpgradeJobTracker.IsActive)
        {
            UpgradeJobTracker.ApplySpeedUp(minutes);
            applied = true;
        }

        if (!applied)
        {
            Debug.Log("[SpeedUp] No active crafting or upgrade job to speed up.");
            return;
        }

        if (_item.stackCount > 1)
        {
            ItemInventory.Instance?.RemoveOne(_item);
            Populate(_item);
        }
        else
        {
            ItemInventory.Instance?.Remove(_item);
            Close();
        }
    }

    private void OnDisenchant()
    {
        if (_item?.data == null) return;

        var inv = Inventory.Instance;
        if (inv == null) { Close(); return; }

        if (_item.data.category == ItemCategory.Potion)
        {
            PotionRecipe potionRecipe = FindPotionRecipe(_item.data);
            if (potionRecipe == null)
            {
                Debug.LogWarning($"[Disenchant] No potion recipe found for {_item.data.itemName}.");
                Close();
                return;
            }
            int herbs = Mathf.FloorToInt(potionRecipe.herbCost * 0.5f);
            if (herbs > 0) inv.Add(ResourceType.Herbs, herbs);
            Debug.Log($"[Disenchant] {_item.data.itemName} → {herbs}x Herbs");
            if (_item.stackCount > 1) { ItemInventory.Instance?.RemoveOne(_item); Populate(_item); }
            else { ItemInventory.Instance?.Remove(_item); Close(); }
            return;
        }

        CraftingRecipe recipe = FindRecipe(_item.data);
        if (recipe == null)
        {
            Debug.LogWarning($"[Disenchant] No recipe found for {_item.data.itemName}.");
            Close();
            return;
        }

        var returned = new System.Text.StringBuilder();
        foreach (var req in recipe.requirements)
        {
            int half = Mathf.FloorToInt(req.amount * 0.5f);
            if (half <= 0) continue;
            inv.Add(req.resource, half);
            returned.Append($"{half}x {req.resource}  ");
        }

        ItemInventory.Instance?.Remove(_item);
        Debug.Log($"[Disenchant] {_item.data.itemName} → {returned.ToString().TrimEnd()}");
        Close();
    }

    private static PotionRecipe FindPotionRecipe(ItemData item)
    {
        if (AlchemyUI.Instance == null) return null;
        foreach (var r in AlchemyUI.Instance.Recipes)
            if (r != null && r.output == item) return r;
        return null;
    }

    private static CraftingRecipe FindRecipe(ItemData item)
    {
        var all = Resources.LoadAll<CraftingRecipe>("Recipes");
        foreach (var recipe in all)
            if (recipe.outputItem == item) return recipe;
        return null;
    }

    private void OnSell()
    {
        if (_item?.data == null) return;

        if (_item.data.category == ItemCategory.Scroll)
        {
            int price = _item.data.sellPrice > 0 ? _item.data.sellPrice : 100;
            int total = price * _item.stackCount;
            Inventory.Instance?.Add(ResourceType.Gold, total);
            ItemInventory.Instance?.Remove(_item);
            Close();
            return;
        }

        // Weapons, shields, armor — return half of crafting resources
        var inv = Inventory.Instance;
        if (inv != null)
        {
            CraftingRecipe recipe = FindRecipe(_item.data);
            if (recipe != null)
            {
                foreach (var req in recipe.requirements)
                {
                    int half = Mathf.FloorToInt(req.amount * 0.5f);
                    if (half > 0) inv.Add(req.resource, half);
                }
            }
        }

        ItemInventory.Instance?.Remove(_item);
        Close();
    }

    private void OnDestroyItem()
    {
        if (_item == null) return;
        if (_item.IsStackable && _item.stackCount > 1)
        {
            ItemInventory.Instance?.RemoveOne(_item);
            Populate(_item);   // refresh count, stay open
        }
        else
        {
            ItemInventory.Instance?.Remove(_item);
            Close();
        }
    }

    private void OnCraft()
    {
        _onCraft?.Invoke();
        Close();
    }
}
