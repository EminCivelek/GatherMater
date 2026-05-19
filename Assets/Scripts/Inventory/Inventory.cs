using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Local-first inventory singleton.
/// Add/Spend/Get are the only write paths — easy to intercept later for server-sync.
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    private readonly Dictionary<ResourceType, int> _items = new();

    /// <summary>Fires with (resourceType, newCount) whenever a resource amount changes.</summary>
    public event Action<ResourceType, int> OnResourceChanged;

    /// <summary>Fires with (resourceType, amountAdded) when resources are successfully added.</summary>
    public event Action<ResourceType, int> OnResourceAdded;

    public int Capacity   { get; private set; } = 1000;
    public int TotalCount => GetTotalCount();

    public int RemainingFor(ResourceType type) =>
        Mathf.Max(0, Capacity - (_items.TryGetValue(type, out int c) ? c : 0));

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            _items[type] = PlayerPrefs.GetInt($"Inv_{type}", 0);
    }

    public void SetCapacity(int capacity) => Capacity = Mathf.Max(1, capacity);

    private void OnApplicationQuit() => Save();
    private void OnApplicationPause(bool paused) { if (paused) Save(); }

    private int GetTotalCount()
    {
        int total = 0;
        foreach (var count in _items.Values) total += count;
        return total;
    }

    public void Save()
    {
        foreach (var pair in _items)
            PlayerPrefs.SetInt($"Inv_{pair.Key}", pair.Value);
        PlayerPrefs.Save();
    }

    /// <summary>Add a positive amount of a resource, clamped to per-resource capacity. Returns amount actually added.</summary>
    public int Add(ResourceType type, int amount)
    {
        if (amount <= 0) return 0;
        int canAdd = Mathf.Min(amount, RemainingFor(type));
        if (canAdd <= 0) return 0;
        _items[type] += canAdd;
        OnResourceChanged?.Invoke(type, _items[type]);
        OnResourceAdded?.Invoke(type, canAdd);
        return canAdd;
    }

    /// <summary>Returns the current count for a resource type.</summary>
    public int Get(ResourceType type) =>
        _items.TryGetValue(type, out int count) ? count : 0;

    /// <summary>Returns true if the player has at least the given amount.</summary>
    public bool Has(ResourceType type, int amount) => Get(type) >= amount;

    /// <summary>
    /// Deducts the amount if available and returns true.
    /// Returns false without modifying anything if the player can't afford it.
    /// </summary>
    public bool Spend(ResourceType type, int amount)
    {
        if (!Has(type, amount)) return false;
        _items[type] -= amount;
        OnResourceChanged?.Invoke(type, _items[type]);
        return true;
    }

    // ── Cloud sync ───────────────────────────────────────────────────────
    public (List<string> keys, List<int> values) GetCloudData()
    {
        var keys   = _items.Keys.Select(k => k.ToString()).ToList();
        var values = _items.Values.ToList();
        return (keys, values);
    }

    public void ApplyCloudData(List<string> keys, List<int> values)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (Enum.TryParse<ResourceType>(keys[i], out var type))
                _items[type] = values[i];
        }
        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            OnResourceChanged?.Invoke(t, Get(t));
        Save();
    }
}
