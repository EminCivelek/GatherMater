using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single inventory slot showing a resource icon and current count.
/// Set up via InventoryUI — not manually placed.
/// </summary>
public class ResourceSlotUI : MonoBehaviour
{
    [SerializeField] private Image    icon;
    [SerializeField] private TMP_Text countText;

    public ResourceType ResourceType { get; private set; }

    public void Init(ResourceType type, Sprite sprite)
    {
        ResourceType  = type;
        icon.sprite   = sprite;
        countText.text = "0";
    }

    public void UpdateCount(int count)
    {
        countText.text = count.ToString();
    }
}
