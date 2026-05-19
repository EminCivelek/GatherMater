using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable slot row for the Upgrade Anvil panel.
/// Used for both the item list and the scroll list.
/// </summary>
public class UpgradeItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image    icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text equippedTag;  // shown only for equipped items
    [SerializeField] private Button   selectButton;
    [SerializeField] private GameObject highlight;  // active when this slot is selected
    [SerializeField] private Image    background;   // root Image tinted when selected

    private static readonly Color NormalBg   = new Color(0.18f, 0.18f, 0.25f, 1f);
    private static readonly Color SelectedBg = new Color(0.30f, 0.22f, 0.08f, 1f);

    public ItemInstance BoundItem { get; private set; }

    private static readonly Color TagColorEquipped = new Color(0.60f, 0.85f, 1.00f, 1f);
    private static readonly Color TagColorMaxLevel = new Color(1.00f, 0.78f, 0.20f, 1f);

    public void Init(ItemInstance item, bool isEquipped, Action onSelect, bool isMaxLevel = false)
    {
        BoundItem = item;
        Refresh();

        bool upgrading = UpgradeJobTracker.IsBeingUpgraded(item?.instanceId);

        if (equippedTag != null)
        {
            if (isMaxLevel)
            {
                equippedTag.text  = "MAX LVL";
                equippedTag.color = TagColorMaxLevel;
                equippedTag.gameObject.SetActive(true);
            }
            else
            {
                equippedTag.text  = "Equipped";
                equippedTag.color = TagColorEquipped;
                equippedTag.gameObject.SetActive(isEquipped && !upgrading);
            }
        }

        if (levelText != null && upgrading) levelText.text = "Upgrading…";

        bool canSelect = !upgrading && !isMaxLevel;
        selectButton.onClick.RemoveAllListeners();
        if (canSelect) selectButton.onClick.AddListener(() => onSelect?.Invoke());
        selectButton.interactable = canSelect;
        SetHighlight(false);
    }

    /// <summary>Re-reads level from the bound item (call after an upgrade).</summary>
    public void Refresh()
    {
        if (BoundItem == null) return;
        if (icon      != null) icon.sprite  = BoundItem.data?.icon;
        if (nameText  != null) nameText.text = BoundItem.data?.itemName ?? "Unknown";
        if (levelText != null) levelText.text = $"Lv {BoundItem.level}";
    }

    public void SetHighlight(bool on)
    {
        if (highlight   != null) highlight.SetActive(on);
        if (background  != null) background.color = on ? SelectedBg : NormalBg;
    }
}
