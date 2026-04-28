using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays one equipment slot (icon, label, lock overlay).
/// Assigned in the Inspector on each slot GameObject inside EquipmentUI.
/// </summary>
public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private Image               itemIcon;
    [SerializeField] private GameObject          emptyIcon;
    [SerializeField] private TextMeshProUGUI     slotLabel;
    [SerializeField] private GameObject          lockOverlay;
    [SerializeField] private Button              button;

    private EquipSlot  _slot;
    private EquipmentUI _ui;

    public void Init(EquipSlot slot, EquipmentUI ui)
    {
        _slot = slot;
        _ui   = ui;
        button.onClick.AddListener(() => _ui.OnSlotClicked(_slot));
        Refresh();
    }

    public void Refresh()
    {
        var item    = Equipment.Instance?.GetEquipped(_slot);
        bool locked = _slot == EquipSlot.OffHand && (Equipment.Instance?.IsOffHandLocked ?? false);

        if (itemIcon    != null) itemIcon.gameObject.SetActive(item != null);
        if (emptyIcon   != null) emptyIcon.SetActive(item == null && !locked);
        if (lockOverlay != null) lockOverlay.SetActive(locked);
        if (button      != null) button.interactable = !locked;

        if (item?.data != null)
        {
            if (itemIcon   != null) itemIcon.sprite = item.data.icon;
            if (slotLabel  != null) slotLabel.text  = $"{item.data.itemName} Lv{item.level}";
        }
        else
        {
            if (slotLabel != null) slotLabel.text = locked ? "Locked" : _slot.ToString();
        }
    }
}
