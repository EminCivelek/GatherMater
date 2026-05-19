using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Stores the player's unequipped item instances.
/// Replaces the old count-based system — items are now individual instances with levels.
/// </summary>
public class ItemInventory : MonoBehaviour
{
    public static ItemInventory Instance { get; private set; }

    private readonly List<ItemInstance> _items = new();

    public const int MaxSlots = 50;

    public IReadOnlyList<ItemInstance> Items => _items;
    public bool IsFull => _items.Count >= MaxSlots;

    public event Action OnItemsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnApplicationQuit()             => Save();
    private void OnApplicationPause(bool paused) { if (paused) Save(); }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Add(ItemInstance item)
    {
        if (item == null) return;
        if (item.data == null)
            item.data = ItemDatabase.Instance?.FindById(item.itemDataId);
        if (item.stackCount < 1) item.stackCount = 1;

        // Merge into existing stack for stackable categories
        if (item.IsStackable)
        {
            var existing = _items.Find(i => i.itemDataId == item.itemDataId);
            if (existing != null)
            {
                existing.stackCount += item.stackCount;
                OnItemsChanged?.Invoke();
                Save();
                return;
            }
        }

        if (string.IsNullOrEmpty(item.instanceId))
            item.instanceId = Guid.NewGuid().ToString("N");
        _items.Add(item);
        OnItemsChanged?.Invoke();
        Save();
    }

    public void Remove(ItemInstance item)
    {
        if (_items.Remove(item))
        {
            OnItemsChanged?.Invoke();
            Save();
        }
    }

    /// <summary>
    /// Consume one from a stack. Removes the entry only when the last one is consumed.
    /// </summary>
    public void RemoveOne(ItemInstance item)
    {
        if (item == null) return;
        if (item.IsStackable && item.stackCount > 1)
        {
            item.stackCount--;
            OnItemsChanged?.Invoke();
            Save();
        }
        else
        {
            Remove(item);
        }
    }

    /// <summary>Call after directly mutating an ItemInstance's level (e.g. upgrade).</summary>
    public void NotifyItemUpgraded()
    {
        OnItemsChanged?.Invoke();
        Save();
    }

    // ── Persistence ───────────────────────────────────────────────────────────────
    private const string SaveKey = "ItemInventory";

    private void Save()
    {
        var wrapper = new SaveWrapper { items = new List<ItemSaveData>() };
        foreach (var item in _items)
        {
            if (string.IsNullOrEmpty(item.instanceId))
                item.instanceId = Guid.NewGuid().ToString("N");
            wrapper.items.Add(new ItemSaveData { instanceId = item.instanceId, itemDataId = item.itemDataId, level = item.level, stackCount = item.stackCount });
        }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;
        var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        if (wrapper?.items == null) return;
        foreach (var saved in wrapper.items)
        {
            var data = ItemDatabase.Instance?.FindById(saved.itemDataId);
            if (data == null) continue;
            _items.Add(new ItemInstance
            {
                instanceId = string.IsNullOrEmpty(saved.instanceId) ? Guid.NewGuid().ToString("N") : saved.instanceId,
                itemDataId = saved.itemDataId,
                level      = saved.level,
                stackCount = saved.stackCount > 0 ? saved.stackCount : 1,
                data       = data
            });
        }
    }

    // ── Cloud sync ───────────────────────────────────────────────────────
    public (List<string> ids, List<int> levels, List<string> instanceIds) GetCloudData()
    {
        var ids         = _items.Select(i => i.itemDataId).ToList();
        var levels      = _items.Select(i => i.level).ToList();
        var instanceIds = _items.Select(i => string.IsNullOrEmpty(i.instanceId) ? Guid.NewGuid().ToString("N") : i.instanceId).ToList();
        return (ids, levels, instanceIds);
    }

    public void ApplyCloudData(List<string> ids, List<int> levels, List<string> instanceIds = null)
    {
        _items.Clear();
        for (int i = 0; i < ids.Count; i++)
        {
            var data = ItemDatabase.Instance?.FindById(ids[i]);
            if (data == null) continue;
            string iid = (instanceIds != null && i < instanceIds.Count && !string.IsNullOrEmpty(instanceIds[i]))
                ? instanceIds[i] : Guid.NewGuid().ToString("N");
            _items.Add(new ItemInstance { instanceId = iid, itemDataId = ids[i], level = levels[i], data = data });
        }
        OnItemsChanged?.Invoke();
        Save();
    }

    [Serializable] private class ItemSaveData { public string instanceId; public string itemDataId; public int level; public int stackCount = 1; }
    [Serializable] private class SaveWrapper  { public List<ItemSaveData> items; }
}
