using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One slot in the 50-slot inventory grid.
/// Pre-spawned at startup — call SetItem() to fill or SetEmpty() to clear.
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image           itemIcon;
    [SerializeField] private Image           emptyBackground; // darker/greyed image shown when empty
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Button          button;

    private ItemInstance _item;
    private EquipmentUI  _ui;

    public void Init(EquipmentUI ui)
    {
        _ui = ui;
        button.onClick.AddListener(OnClicked);
        SetEmpty();
    }

    public void SetItem(ItemInstance item)
    {
        _item = item;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = item.data?.icon;
        }
        if (emptyBackground != null) emptyBackground.gameObject.SetActive(false);
        if (levelText   != null) levelText.text   = $"Lv{item.level}";
        if (itemNameText != null) itemNameText.text = item.data?.itemName ?? "";

        button.interactable = true;
    }

    public void SetEmpty()
    {
        _item = null;
        if (itemIcon        != null) itemIcon.gameObject.SetActive(false);
        if (emptyBackground != null) emptyBackground.gameObject.SetActive(true);
        if (levelText       != null) levelText.text   = "";
        if (itemNameText    != null) itemNameText.text = "";
        button.interactable = false;
    }

    private void OnClicked()
    {
        if (_item != null) _ui.OnItemClicked(_item);
    }
}
