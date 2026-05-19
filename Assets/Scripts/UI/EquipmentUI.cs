using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Combined equipment panel — left side shows owned items, right side shows equipped slots.
/// Open via the screen button wired to EquipmentUI.Open().
/// Closes automatically when the player moves.
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    public static EquipmentUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Inventory Grid")]
    [SerializeField] private Transform  itemGridParent;
    [SerializeField] private ItemSlotUI itemSlotPrefab;

    [Header("Equipment Slots")]
    [SerializeField] private Transform        equipSlotsParent;
    [SerializeField] private EquipmentSlotUI  equipSlotPrefab;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;
    [SerializeField] private TextMeshProUGUI armorText;

    private const int InventorySlotCount = 50;

    private bool _initialized;
    private GameObject _overlay;
    private IsometricPlayerController      _player;
    private readonly List<ItemSlotUI>      _itemSlots  = new();
    private readonly List<EquipmentSlotUI> _equipSlots = new();

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start() => Initialize();

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _player = FindAnyObjectByType<IsometricPlayerController>();

        // Pre-spawn all 50 inventory slots
        for (int i = 0; i < InventorySlotCount; i++)
        {
            var slot = Instantiate(itemSlotPrefab, itemGridParent);
            slot.Init(this);
            _itemSlots.Add(slot);
        }

        // Spawn one equipment slot per EquipSlot enum value and position as paperdoll
        foreach (EquipSlot equipSlot in Enum.GetValues(typeof(EquipSlot)))
        {
            var slot = Instantiate(equipSlotPrefab, equipSlotsParent);
            slot.Init(equipSlot, this);
            _equipSlots.Add(slot);
            PositionEquipSlot(slot.GetComponent<RectTransform>(), equipSlot);
        }
    }

    private void Update()
    {
        if (!panel.activeSelf) return;
        if (_player != null && _player.IsMoving) Close();
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open()
    {
        CraftingUI.Instance?.Close();
        AlchemyUI.Instance?.Close();
        UpgradeAnvilUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        DailyMissionBoardUI.Instance?.Close();

        Initialize();

        if (ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnItemsChanged -= RefreshInventory;
            ItemInventory.Instance.OnItemsChanged += RefreshInventory;
        }
        if (Equipment.Instance != null)
        {
            Equipment.Instance.OnEquipmentChanged -= RefreshAll;
            Equipment.Instance.OnEquipmentChanged += RefreshAll;
        }

        EnsureOverlay();
        _overlay.SetActive(true);
        _overlay.transform.SetAsLastSibling();
        transform.SetAsLastSibling();
        panel.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        if (_overlay != null) _overlay.SetActive(false);
        panel.SetActive(false);
    }

    private void EnsureOverlay()
    {
        if (_overlay != null) return;
        _overlay = new GameObject("CloseOverlay");
        _overlay.transform.SetParent(transform.parent, false);
        var rt = _overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var img = _overlay.AddComponent<Image>(); img.color = Color.clear;
        var btn = _overlay.AddComponent<Button>(); btn.targetGraphic = img;
        btn.onClick.AddListener(Close);
        _overlay.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (ItemInventory.Instance != null)
            ItemInventory.Instance.OnItemsChanged -= RefreshInventory;
        if (Equipment.Instance != null)
            Equipment.Instance.OnEquipmentChanged -= RefreshAll;
    }

    public void OnItemClicked(ItemInstance item, RectTransform slotRect)
    {
        if (item?.data == null) return;
        ItemInfoPanelUI.Instance?.Open(item);
    }

    public void EquipItem(ItemInstance item)
    {
        var eq  = Equipment.Instance;
        var inv = ItemInventory.Instance;
        if (eq == null || inv == null || item?.data == null) return;

        var targetSlot = item.PrimarySlot;
        if (!eq.CanEquipInSlot(item, targetSlot))
        {
            if (item.CanEquipOffHand && eq.CanEquipInSlot(item, EquipSlot.OffHand))
                targetSlot = EquipSlot.OffHand;
            else
                return;
        }

        inv.Remove(item);
        eq.Equip(item, targetSlot);
    }

    public void OnSlotClicked(EquipSlot slot) => Equipment.Instance?.Unequip(slot);

    // ── Private ───────────────────────────────────────────────────────────────────
    private void RefreshAll()
    {
        RefreshInventory();
        RefreshSlots();
        RefreshStats();
    }

    private void RefreshInventory()
    {
        var items = ItemInventory.Instance?.Items;
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            if (items != null && i < items.Count)
                _itemSlots[i].SetItem(items[i]);
            else
                _itemSlots[i].SetEmpty();
        }
    }

    private void RefreshSlots()
    {
        foreach (var slot in _equipSlots) slot.Refresh();
    }

    private void RefreshStats()
    {
        var eq = Equipment.Instance;
        if (eq == null) return;
        if (attackText      != null) attackText.text      = $"Attack: {PlayerStats.Instance?.CombinedAttackDamage:F1}";
        if (attackSpeedText != null) attackSpeedText.text = $"Atk Speed: {PlayerStats.Instance?.CombinedAttackSpeed:F2}";
        if (armorText       != null) armorText.text       = $"Armor: {eq.TotalArmor:F1}";
    }

    // Paperdoll layout — anchor top-left of EquipSlotsParent (606×520), pivot center
    //            [Helmet]
    // [Chest]            [Gloves]
    // [Pants]            [Boots]
    //       [MainHand][OffHand]
    private static void PositionEquipSlot(RectTransform rt, EquipSlot slot)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120f, 120f);
        rt.anchoredPosition = slot switch
        {
            EquipSlot.Helmet   => new Vector2(303f,  -70f),
            EquipSlot.Chest    => new Vector2( 70f, -205f),
            EquipSlot.Pants    => new Vector2( 70f, -340f),
            EquipSlot.Gloves   => new Vector2(536f, -205f),
            EquipSlot.Boots    => new Vector2(536f, -340f),
            EquipSlot.MainHand => new Vector2(233f, -475f),
            EquipSlot.OffHand  => new Vector2(373f, -475f),
            _                  => rt.anchoredPosition,
        };
    }
}
