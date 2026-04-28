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

    public IReadOnlyList<ItemInstance> Items => _items;

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

    // ── Persistence ───────────────────────────────────────────────────────────────
    private const string SaveKey = "ItemInventory";

    private void Save()
    {
        var wrapper = new SaveWrapper { items = new List<ItemSaveData>() };
        foreach (var item in _items)
            wrapper.items.Add(new ItemSaveData { itemDataId = item.itemDataId, level = item.level });
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
            _items.Add(new ItemInstance { itemDataId = saved.itemDataId, level = saved.level, data = data });
        }
    }

    // ── Cloud sync ───────────────────────────────────────────────────────
    public (List<string> ids, List<int> levels) GetCloudData()
    {
        var ids    = _items.Select(i => i.itemDataId).ToList();
        var levels = _items.Select(i => i.level).ToList();
        return (ids, levels);
    }

    public void ApplyCloudData(List<string> ids, List<int> levels)
    {
        _items.Clear();
        for (int i = 0; i < ids.Count; i++)
        {
            var data = ItemDatabase.Instance?.FindById(ids[i]);
            if (data == null) continue;
            _items.Add(new ItemInstance { itemDataId = ids[i], level = levels[i], data = data });
        }
        OnItemsChanged?.Invoke();
        Save();
    }

    [Serializable] private class ItemSaveData { public string itemDataId; public int level; }
    [Serializable] private class SaveWrapper  { public List<ItemSaveData> items; }
}
