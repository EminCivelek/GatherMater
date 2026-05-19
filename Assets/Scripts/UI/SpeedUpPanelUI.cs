using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedUpPanelUI : MonoBehaviour
{
    public static SpeedUpPanelUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button     closeButton;

    [Header("Item List")]
    [SerializeField] private Transform         itemListParent;
    [SerializeField] private UpgradeItemSlotUI slotPrefab;

    [Header("Action")]
    [SerializeField] private Button       speedUpButton;
    [SerializeField] private TMP_Text     infoText;

    private Action<ItemInstance>          _onApply;
    private ItemInstance                  _selectedItem;
    private readonly List<UpgradeItemSlotUI> _slots = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
        speedUpButton.onClick.AddListener(OnApply);
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open(Action<ItemInstance> onApply)
    {
        _onApply      = onApply;
        _selectedItem = null;
        panel.SetActive(true);
        BuildList();
        RefreshButton();
    }

    public void Close()
    {
        panel.SetActive(false);
        _selectedItem = null;
        _onApply      = null;
    }

    // ── Private ───────────────────────────────────────────────────────────────────
    private void BuildList()
    {
        foreach (var s in _slots) Destroy(s.gameObject);
        _slots.Clear();

        if (ItemInventory.Instance == null) return;

        foreach (var item in ItemInventory.Instance.Items)
        {
            if (item.data?.category != ItemCategory.SpeedUp) continue;
            var slot = Instantiate(slotPrefab, itemListParent);
            ItemInstance captured = item;
            slot.Init(item, false, () => SelectItem(captured));
            OverrideSlotText(slot, item);
            _slots.Add(slot);
        }

        if (infoText != null && _slots.Count == 0)
            infoText.text = "No Speed Up items";
    }

    private static void OverrideSlotText(UpgradeItemSlotUI slot, ItemInstance item)
    {
        int mins = item.data?.speedUpMinutes > 0 ? item.data.speedUpMinutes : 1;
        foreach (var t in slot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.text.StartsWith("Lv "))
            {
                t.text = $"x{item.stackCount}  (−{mins} min)";
                break;
            }
        }
    }

    private void SelectItem(ItemInstance item)
    {
        _selectedItem = item;
        foreach (var s in _slots)
            s.SetHighlight(s.BoundItem == item);
        RefreshButton();
    }

    private void RefreshButton()
    {
        if (speedUpButton != null)
            speedUpButton.interactable = _selectedItem != null;

        if (infoText != null)
        {
            if (_selectedItem != null)
            {
                int mins = _selectedItem.data?.speedUpMinutes > 0 ? _selectedItem.data.speedUpMinutes : 1;
                infoText.text = $"−{mins} minute{(mins > 1 ? "s" : "")} from active job";
            }
            else if (_slots.Count > 0)
            {
                infoText.text = "Select an item";
            }
        }
    }

    private void OnApply()
    {
        if (_selectedItem == null) return;
        _onApply?.Invoke(_selectedItem);
        ItemInventory.Instance?.RemoveOne(_selectedItem);
        BuildList();
        _selectedItem = null;
        RefreshButton();
        if (_slots.Count == 0) Close();
    }
}
