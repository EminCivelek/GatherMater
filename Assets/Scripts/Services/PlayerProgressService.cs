using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerProgressService : MonoBehaviour
{
    public static PlayerProgressService Instance { get; private set; }

    bool _isLoading;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        if (Equipment.Instance != null)
            Equipment.Instance.OnEquipmentChanged += OnProgressChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (Equipment.Instance != null)
            Equipment.Instance.OnEquipmentChanged -= OnProgressChanged;
    }

    void Start()
    {
        if (UGSManager.Instance == null) return;
        if (UGSManager.Instance.IsSignedIn)
            _ = LoadAndApplyAsync();
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
        _ = LoadAndApplyAsync();
    }

    void OnSceneChanged(Scene from, Scene to)
    {
        // Defer by one frame so CraftingStation.Start() can award completed jobs first
        if (to.name == "VillageScene" && !_isLoading)
            StartCoroutine(SaveAfterStartRoutine());
    }

    System.Collections.IEnumerator SaveAfterStartRoutine()
    {
        yield return null;
        if (!_isLoading) _ = SaveAsync();
    }

    void OnApplicationQuit()             => _ = SaveAsync();
    void OnApplicationPause(bool paused) { if (paused && !_isLoading) _ = SaveAsync(); }

    void OnProgressChanged()
    {
        if (!_isLoading) _ = SaveAsync();
    }

    // ── Save ──────────────────────────────────────────────────────────────
    public async Task SaveAsync()
    {
        if (CloudSaveManager.Instance == null) return;

        string json = SnapshotToJson();
        await CloudSaveManager.Instance.SavePlayerProgressAsync(json);
        Debug.Log("[Progress] Saved to cloud.");
    }

    string SnapshotToJson()
    {
        var data = new ProgressData();

        if (PlayerStats.Instance != null)
        {
            data.level        = PlayerStats.Instance.level;
            data.currentXP    = PlayerStats.Instance.currentXP;
            data.totalXpFarmed = PlayerStats.Instance.totalXpFarmed;
            data.maxHP        = PlayerStats.Instance.maxHP;
            data.maxMP        = PlayerStats.Instance.maxMP;
            data.attackDamage = PlayerStats.Instance.attackDamage;
            data.attackSpeed  = PlayerStats.Instance.attackSpeed;
            data.hpRegen      = PlayerStats.Instance.hpRegen;
            data.mpRegen      = PlayerStats.Instance.mpRegen;
        }

        if (Inventory.Instance != null)
        {
            var (keys, values) = Inventory.Instance.GetCloudData();
            data.resourceKeys   = keys;
            data.resourceValues = values;
        }

        if (ItemInventory.Instance != null)
        {
            var (ids, levels, instanceIds) = ItemInventory.Instance.GetCloudData();
            data.invItemIds         = ids;
            data.invItemLevels      = levels;
            data.invItemInstanceIds = instanceIds;
        }

        if (Equipment.Instance != null)
        {
            var (slots, ids, levels, instanceIds) = Equipment.Instance.GetCloudData();
            data.equipSlots       = slots;
            data.equipItemIds     = ids;
            data.equipItemLevels  = levels;
            data.equipInstanceIds = instanceIds;
        }

        if (HonorManager.Instance != null)
            data.honorPoints = HonorManager.Instance.HonorPoints;

        return JsonUtility.ToJson(data);
    }

    // ── Load ──────────────────────────────────────────────────────────────
    async Task LoadAndApplyAsync()
    {
        if (CloudSaveManager.Instance == null) return;

        string json = await CloudSaveManager.Instance.LoadPlayerProgressAsync();
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<ProgressData>(json);
        if (data == null) return;

        _isLoading = true;
        ApplyData(data);
        _isLoading = false;

        Debug.Log("[Progress] Loaded from cloud.");
    }

    void ApplyData(ProgressData data)
    {
        PlayerStats.Instance?.ApplyCloudData(
            data.level, data.currentXP, data.totalXpFarmed,
            data.maxHP, data.maxMP, data.attackDamage, data.attackSpeed,
            data.hpRegen, data.mpRegen
        );

        if (data.resourceKeys?.Count > 0)
            Inventory.Instance?.ApplyCloudData(data.resourceKeys, data.resourceValues);

        // Always apply even when empty — empty means the player's inventory IS empty
        // (e.g. everything equipped). The old Count > 0 guard let stale local data survive.
        ItemInventory.Instance?.ApplyCloudData(
            data.invItemIds         ?? new System.Collections.Generic.List<string>(),
            data.invItemLevels      ?? new System.Collections.Generic.List<int>(),
            data.invItemInstanceIds);

        Equipment.Instance?.ApplyCloudData(
            data.equipSlots       ?? new System.Collections.Generic.List<string>(),
            data.equipItemIds     ?? new System.Collections.Generic.List<string>(),
            data.equipItemLevels  ?? new System.Collections.Generic.List<int>(),
            data.equipInstanceIds);

        if (data.honorPoints > 0)
            HonorManager.Instance?.SetFromCloud(data.honorPoints);
    }

    // ── Data structure ────────────────────────────────────────────────────
    [Serializable]
    class ProgressData
    {
        public int   level        = 1;
        public int   currentXP    = 0;
        public long  totalXpFarmed = 0;
        public float maxHP        = 100f;
        public float maxMP        = 50f;
        public float attackDamage = 10f;
        public float attackSpeed  = 1f;
        public float hpRegen      = 1f;
        public float mpRegen      = 1f;

        public List<string> resourceKeys   = new();
        public List<int>    resourceValues = new();

        public List<string> invItemIds         = new();
        public List<int>    invItemLevels      = new();
        public List<string> invItemInstanceIds = new();

        public List<string> equipSlots       = new();
        public List<string> equipItemIds     = new();
        public List<int>    equipItemLevels  = new();
        public List<string> equipInstanceIds = new();

        public int honorPoints = 0;
    }
}
