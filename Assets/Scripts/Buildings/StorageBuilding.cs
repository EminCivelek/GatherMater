using UnityEngine;

/// <summary>
/// Pre-placed storage building. Sets the player's total inventory capacity.
/// Player walks up to upgrade it, which increases capacity.
/// </summary>
public class StorageBuilding : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [SerializeField] private StorageConfig config;

    [Header("Identity")]
    [Tooltip("Unique string ID for PlayerPrefs persistence.")]
    [SerializeField] private string buildingId;

    // ── IInteractable ─────────────────────────────────────────────────────────────
    public string InteractionLabel => config.buildingName;
    public Sprite InteractionIcon  => config.icon;
    public float  GatherDuration   => 0f;
    public bool   IsAvailable      => true;

    // ── Public state ──────────────────────────────────────────────────────────────
    public StorageConfig Config  => config;
    public int  UpgradeLevel     => _upgradeLevel;
    public bool IsMaxLevel       => _upgradeLevel >= config.tiers.Length - 1;

    public StorageConfig.StorageTier CurrentTier =>
        config.tiers[Mathf.Clamp(_upgradeLevel, 0, config.tiers.Length - 1)];

    public StorageConfig.UpgradeCost[] UpgradeCosts =>
        IsMaxLevel ? null : CurrentTier.upgradeCost;

    // ── Private ───────────────────────────────────────────────────────────────────
    private int _upgradeLevel;
    private string LevelKey => $"{buildingId}_storage_level";

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
    private void Awake() => _upgradeLevel = PlayerPrefs.GetInt(LevelKey, 0);

    private void Start() => ApplyCapacity();

    // ── IInteractable ─────────────────────────────────────────────────────────────
    public void OnGatherComplete() => StorageUI.Instance?.Open(this);

    // ── Public API ────────────────────────────────────────────────────────────────
    public bool CanUpgrade()
    {
        if (IsMaxLevel) return false;
        var costs = CurrentTier.upgradeCost;
        if (costs == null || costs.Length == 0) return false;
        foreach (var c in costs)
            if (!Inventory.Instance.Has(c.resource, c.amount)) return false;
        return true;
    }

    public void Upgrade()
    {
        if (!CanUpgrade()) return;
        foreach (var c in CurrentTier.upgradeCost)
            Inventory.Instance.Spend(c.resource, c.amount);
        _upgradeLevel++;
        PlayerPrefs.SetInt(LevelKey, _upgradeLevel);
        PlayerPrefs.Save();
        ApplyCapacity();
        Debug.Log($"[Storage] Upgraded to level {_upgradeLevel + 1} — capacity: {CurrentTier.capacity}");
    }

    // ── Private ───────────────────────────────────────────────────────────────────
    private void ApplyCapacity() => Inventory.Instance?.SetCapacity(CurrentTier.capacity);
}
