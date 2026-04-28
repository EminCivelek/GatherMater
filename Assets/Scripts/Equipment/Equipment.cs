using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's equipped items and computes combined stats.
/// Dual wield: each weapon contributes 50% attack and attack speed.
/// Shield in off-hand: full main hand attack, shield armor stacks.
/// </summary>
public class Equipment : MonoBehaviour
{
    public static Equipment Instance { get; private set; }

    private readonly Dictionary<EquipSlot, ItemInstance> _equipped = new();

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── Public state ──────────────────────────────────────────────────────────────
    public bool IsOffHandLocked =>
        _equipped.TryGetValue(EquipSlot.MainHand, out var main) && main.IsTwoHanded;

    public ItemInstance GetEquipped(EquipSlot slot) =>
        _equipped.TryGetValue(slot, out var item) ? item : null;

    // ── Computed stats ────────────────────────────────────────────────────────────
    public float TotalAttack
    {
        get
        {
            var main = GetEquipped(EquipSlot.MainHand);
            var off  = GetEquipped(EquipSlot.OffHand);
            if (main == null) return 0f;
            if (off != null && off.IsWeapon)
                return main.GetAttack() * 0.5f + off.GetAttack() * 0.5f;
            return main.GetAttack();
        }
    }

    public float TotalAttackSpeed
    {
        get
        {
            var main = GetEquipped(EquipSlot.MainHand);
            var off  = GetEquipped(EquipSlot.OffHand);
            if (main == null) return 0f;
            if (off != null && off.IsWeapon)
                return main.GetAttackSpeed() * 0.5f + off.GetAttackSpeed() * 0.5f;
            return main.GetAttackSpeed();
        }
    }

    public float TotalArmor
    {
        get
        {
            float total = 0f;
            foreach (var kvp in _equipped) total += kvp.Value.GetArmor();
            return total;
        }
    }

    public float TotalOnHitBonusDamage => WeaponStat(d => d.onHitBonusDamage);
    public float TotalOnHitHPRecovery  => WeaponStat(d => d.onHitHPRecovery);
    public float TotalOnHitMPRecovery  => WeaponStat(d => d.onHitMPRecovery);

    // ── Public API ────────────────────────────────────────────────────────────────
    public bool CanEquipInSlot(ItemInstance item, EquipSlot slot)
    {
        if (item?.data == null) return false;
        if (slot == EquipSlot.OffHand && IsOffHandLocked) return false;

        return item.data.category switch
        {
            ItemCategory.Weapon => slot == EquipSlot.MainHand ||
                                   (slot == EquipSlot.OffHand && item.data.canEquipOffHand),
            ItemCategory.Shield => slot == EquipSlot.OffHand,
            ItemCategory.Armor  => slot == item.data.primarySlot,
            _                   => false
        };
    }

    public void Equip(ItemInstance item, EquipSlot slot)
    {
        if (!CanEquipInSlot(item, slot)) return;

        // Equipping a two-handed weapon returns any off-hand item to inventory
        if (item.IsTwoHanded && _equipped.TryGetValue(EquipSlot.OffHand, out var offItem))
        {
            ItemInventory.Instance?.Add(offItem);
            _equipped.Remove(EquipSlot.OffHand);
        }

        // Return the item already in the target slot to inventory
        if (_equipped.TryGetValue(slot, out var existing))
            ItemInventory.Instance?.Add(existing);

        _equipped[slot] = item;
        Save();
        OnEquipmentChanged?.Invoke();
    }

    public void Unequip(EquipSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out var item)) return;
        ItemInventory.Instance?.Add(item);
        _equipped.Remove(slot);
        Save();
        OnEquipmentChanged?.Invoke();
    }

    // ── Private helpers ───────────────────────────────────────────────────────────
    private float WeaponStat(Func<ItemData, float> selector)
    {
        float val = 0f;
        var main = GetEquipped(EquipSlot.MainHand);
        var off  = GetEquipped(EquipSlot.OffHand);
        if (main?.data != null) val += selector(main.data);
        if (off?.data  != null && off.IsWeapon) val += selector(off.data);
        return val;
    }

    // ── Persistence ───────────────────────────────────────────────────────────────
    private const string SaveKey = "Equipment";

    private void Save()
    {
        var wrapper = new SaveWrapper { slots = new List<SlotSaveData>() };
        foreach (var kvp in _equipped)
            wrapper.slots.Add(new SlotSaveData { slot = kvp.Key, itemDataId = kvp.Value.itemDataId, level = kvp.Value.level });
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;
        var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        if (wrapper?.slots == null) return;
        foreach (var s in wrapper.slots)
        {
            var data = ItemDatabase.Instance?.FindById(s.itemDataId);
            if (data == null) continue;
            _equipped[s.slot] = new ItemInstance { itemDataId = s.itemDataId, level = s.level, data = data };
        }
    }

    // ── Cloud sync ───────────────────────────────────────────────────────
    public (List<string> slots, List<string> ids, List<int> levels) GetCloudData()
    {
        var slots  = new List<string>();
        var ids    = new List<string>();
        var levels = new List<int>();
        foreach (var kv in _equipped)
        {
            slots.Add(kv.Key.ToString());
            ids.Add(kv.Value.itemDataId);
            levels.Add(kv.Value.level);
        }
        return (slots, ids, levels);
    }

    public void ApplyCloudData(List<string> slots, List<string> ids, List<int> levels)
    {
        _equipped.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            if (!Enum.TryParse<EquipSlot>(slots[i], out var slot)) continue;
            var data = ItemDatabase.Instance?.FindById(ids[i]);
            if (data == null) continue;
            _equipped[slot] = new ItemInstance { itemDataId = ids[i], level = levels[i], data = data };
        }
        Save();
        OnEquipmentChanged?.Invoke();
    }

    [Serializable] private class SlotSaveData  { public EquipSlot slot; public string itemDataId; public int level; }
    [Serializable] private class SaveWrapper   { public List<SlotSaveData> slots; }
}
