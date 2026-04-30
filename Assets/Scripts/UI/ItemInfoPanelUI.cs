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
    [SerializeField] private Button equipButton;
    [SerializeField] private Button disenchantButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button destroyButton;

    private ItemInstance _item;

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
        disenchantButton.onClick.AddListener(OnDisenchant);
        sellButton.onClick.AddListener(OnSell);
        destroyButton.onClick.AddListener(OnDestroyItem);
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open(ItemInstance item)
    {
        _item = item;
        Populate(item);
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        _item = null;
    }

    // ── Populate ──────────────────────────────────────────────────────────────────
    private void Populate(ItemInstance item)
    {
        var data = item.data;

        // Identity
        if (itemIcon     != null) itemIcon.sprite   = data?.icon;
        if (itemNameText != null) itemNameText.text  = data?.itemName ?? "Unknown";
        if (levelText    != null) levelText.text     = $"Level  {item.level}";
        if (categoryText != null) categoryText.text  = data?.category.ToString() ?? "";
        if (slotText     != null) slotText.text      = data?.primarySlot.ToString() ?? "";

        bool isWeaponOrShield = item.IsWeapon || item.IsShield;
        bool isArmor          = data?.category == ItemCategory.Armor;

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

    private void OnDisenchant()
    {
        if (_item?.data == null) return;

        CraftingRecipe recipe = FindRecipe(_item.data);
        if (recipe == null)
        {
            Debug.LogWarning($"[Disenchant] No recipe found for {_item.data.itemName}.");
            Close();
            return;
        }

        var inv = Inventory.Instance;
        if (inv == null) { Close(); return; }

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

    private static CraftingRecipe FindRecipe(ItemData item)
    {
        var all = Resources.LoadAll<CraftingRecipe>("Recipes");
        foreach (var recipe in all)
            if (recipe.outputItem == item) return recipe;
        return null;
    }

    private void OnSell()
    {
        // TODO: wire up to shop / gold system
        Debug.Log($"[ItemInfo] Sell: {_item?.data?.itemName}");
        Close();
    }

    private void OnDestroyItem()
    {
        if (_item == null) return;
        ItemInventory.Instance?.Remove(_item);
        Close();
    }
}
