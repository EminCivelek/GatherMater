using System.Text;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines one craftable item.
/// Create via: right-click in Project → GatherMater → Crafting Recipe
/// </summary>
[CreateAssetMenu(fileName = "New Recipe", menuName = "GatherMater/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Output")]
    public string   displayName;
    public Sprite   icon;
    public ItemData outputItem;
    public int      baseLevel = 1;

    [Header("Cost")]
    public ResourceRequirement[] requirements;
    public int goldCost;

    [Header("Timing")]
    [Tooltip("How long this recipe takes to craft in seconds.")]
    public float craftDuration = 10f;

    [System.Serializable]
    public struct ResourceRequirement
    {
        public ResourceType resource;
        public int          amount;
    }

    public bool CanAfford()
    {
        if (Inventory.Instance == null) return false;
        foreach (var req in requirements)
            if (!Inventory.Instance.Has(req.resource, req.amount)) return false;
        if (goldCost > 0 && !Inventory.Instance.Has(ResourceType.Gold, goldCost)) return false;
        return true;
    }

    public void DeductCost()
    {
        foreach (var req in requirements)
            Inventory.Instance.Spend(req.resource, req.amount);
        if (goldCost > 0)
            Inventory.Instance.Spend(ResourceType.Gold, goldCost);
    }

    /// <summary>Returns a human-readable cost string e.g. "3x Wood  2x Stone  50x Gold"</summary>
    public string GetRequirementsText()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < requirements.Length; i++)
        {
            if (i > 0) sb.Append("  ");
            sb.Append($"{requirements[i].amount}x {requirements[i].resource}");
        }
        if (goldCost > 0)
        {
            if (sb.Length > 0) sb.Append("  ");
            sb.Append($"{goldCost}x Gold");
        }
        return sb.ToString();
    }
}
