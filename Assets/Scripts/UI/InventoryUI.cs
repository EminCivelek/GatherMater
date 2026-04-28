using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns one ResourceSlotUI per resource type and keeps counts in sync
/// with the Inventory singleton via its OnResourceChanged event.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Serializable]
    private struct ResourceIconEntry
    {
        public ResourceType type;
        public Sprite       icon;
    }

    [SerializeField] private ResourceSlotUI     slotPrefab;
    [SerializeField] private Transform          slotsParent;
    [SerializeField] private ResourceIconEntry[] resourceIcons;

    private readonly Dictionary<ResourceType, ResourceSlotUI> _slots = new();

    private void Start()
    {
        foreach (var entry in resourceIcons)
        {
            ResourceSlotUI slot = Instantiate(slotPrefab, slotsParent);
            slot.Init(entry.type, entry.icon);
            _slots[entry.type] = slot;
        }

        if (Inventory.Instance == null) return;

        Inventory.Instance.OnResourceChanged += Refresh;

        // Populate initial values (all 0 on a fresh session)
        foreach (var entry in resourceIcons)
            Refresh(entry.type, Inventory.Instance.Get(entry.type));
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnResourceChanged -= Refresh;
    }

    private void Refresh(ResourceType type, int count)
    {
        if (_slots.TryGetValue(type, out ResourceSlotUI slot))
            slot.UpdateCount(count);
    }
}
