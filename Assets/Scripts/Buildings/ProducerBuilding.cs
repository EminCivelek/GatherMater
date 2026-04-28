using System;
using System.Collections.Generic;
using UnityEngine;

public class ProducerBuilding : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [SerializeField] private BuildingConfig config;

    [Header("Identity")]
    [Tooltip("Unique string ID used for persistence. Must be unique across all buildings in the scene.")]
    [SerializeField] private string buildingId;

    // ── IInteractable ────────────────────────────────────────────────────
    public string InteractionLabel => config.buildingName;
    public Sprite InteractionIcon  => config.icon;
    public float  GatherDuration   => 0f;
    public bool   IsAvailable      => true;

    // ── Public state ─────────────────────────────────────────────────────
    public BuildingConfig Config        => config;
    public int  UpgradeLevel            => _upgradeLevel;
    public bool IsMaxLevel              => _upgradeLevel >= config.tiers.Length - 1;

    public BuildingConfig.UpgradeTier CurrentTier =>
        config.tiers[Mathf.Clamp(_upgradeLevel, 0, config.tiers.Length - 1)];

    public BuildingConfig.UpgradeCost[] UpgradeCosts =>
        IsMaxLevel ? null : CurrentTier.upgradeCost;

    // ── Private ───────────────────────────────────────────────────────────
    int  _upgradeLevel;
    int  _storedAmount;
    long _lastCollectedUtc;   // Unix seconds UTC

    string LevelKey    => $"{buildingId}_level";
    string StoredKey   => $"{buildingId}_stored";
    string LastSyncKey => $"{buildingId}_lastSync";

    // ── Unity lifecycle ───────────────────────────────────────────────────
    void Awake() => LoadLocal();

    void Start()
    {
        if (UGSManager.Instance == null) return;

        if (UGSManager.Instance.IsSignedIn)
            _ = LoadCloudTimestampAsync();
        else
            UGSManager.Instance.OnSignedIn += OnUGSReady;
    }

    void OnDestroy()
    {
        if (UGSManager.Instance != null)
            UGSManager.Instance.OnSignedIn -= OnUGSReady;
    }

    void OnUGSReady()
    {
        UGSManager.Instance.OnSignedIn -= OnUGSReady;
        _ = LoadCloudTimestampAsync();
    }

    // ── IInteractable ─────────────────────────────────────────────────────
    public void OnGatherComplete() => BuildingCollectUI.Instance?.Open(this);

    // ── Public API ────────────────────────────────────────────────────────
    public int GetStoredAmount()
    {
        double elapsedMinutes = (CloudSaveManager.UtcNowSeconds() - _lastCollectedUtc) / 60.0;
        int newProduced = Mathf.FloorToInt((float)(elapsedMinutes * CurrentTier.produceRatePerMinute));
        return Mathf.Min(_storedAmount + newProduced, CurrentTier.storageCap);
    }

    public void CollectAll()
    {
        _storedAmount       = GetStoredAmount();
        _lastCollectedUtc   = CloudSaveManager.UtcNowSeconds();

        if (_storedAmount <= 0) return;

        int collected = Inventory.Instance?.Add(config.resourceType, _storedAmount) ?? 0;
        _storedAmount -= collected;

        SaveLocal();
        _ = SaveCloudTimestampAsync();

        Debug.Log($"[Building] Collected {collected}x {config.resourceType} from {config.buildingName} — {_storedAmount} remaining");
    }

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
        SaveLocal();
        Debug.Log($"[Building] {config.buildingName} upgraded to level {_upgradeLevel + 1}");
    }

    // ── Local persistence (PlayerPrefs — instant, offline-safe fallback) ──
    void SaveLocal()
    {
        PlayerPrefs.SetInt(LevelKey, _upgradeLevel);
        PlayerPrefs.SetInt(StoredKey, _storedAmount);
        PlayerPrefs.SetString(LastSyncKey, _lastCollectedUtc.ToString());
        PlayerPrefs.Save();
    }

    void LoadLocal()
    {
        _upgradeLevel = PlayerPrefs.GetInt(LevelKey, 0);
        _storedAmount = PlayerPrefs.GetInt(StoredKey, 0);
        string raw    = PlayerPrefs.GetString(LastSyncKey, "");

        if (!string.IsNullOrEmpty(raw) && long.TryParse(raw, out long saved) && saved > 0)
            _lastCollectedUtc = saved;
        else
            _lastCollectedUtc = CloudSaveManager.UtcNowSeconds();
    }

    // ── Cloud persistence (CloudSave — server timestamps, cheat-proof) ────
    async System.Threading.Tasks.Task SaveCloudTimestampAsync()
    {
        if (CloudSaveManager.Instance == null) return;
        await CloudSaveManager.Instance.SaveBuildingTimestampAsync(buildingId, _lastCollectedUtc);
    }

    async System.Threading.Tasks.Task LoadCloudTimestampAsync()
    {
        if (CloudSaveManager.Instance == null) return;

        Dictionary<string, long> timestamps = await CloudSaveManager.Instance.LoadBuildingTimestampsAsync();

        if (timestamps.TryGetValue(buildingId, out long cloudUtc) && cloudUtc > _lastCollectedUtc)
        {
            _lastCollectedUtc = cloudUtc;
            SaveLocal();
            Debug.Log($"[Building] {buildingId} — cloud timestamp applied ({cloudUtc})");
        }
    }
}
