using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PotionRecipeSlotUI : MonoBehaviour
{
    [SerializeField] private Image    icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text canCraftText;
    [SerializeField] private Button   selectButton;
    [SerializeField] private GameObject highlight;

    public PotionRecipe Recipe { get; private set; }

    public void Init(PotionRecipe recipe, Action onSelect)
    {
        Recipe = recipe;
        if (icon     != null) icon.sprite  = recipe.output?.icon;
        if (nameText != null) nameText.text = recipe.output?.itemName ?? "Unknown";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelect?.Invoke());
        SetHighlight(false);
        Refresh();
    }

    public void Refresh()
    {
        if (Recipe == null) return;
        var inv = Inventory.Instance;
        int herbs = inv?.Get(ResourceType.Herbs) ?? 0;
        int gold  = inv?.Get(ResourceType.Gold)  ?? 0;

        int canCraftByHerbs = Recipe.herbCost > 0 ? herbs / Recipe.herbCost : int.MaxValue;
        int canCraftByGold  = Recipe.goldCost  > 0 ? gold  / Recipe.goldCost  : int.MaxValue;
        int canCraft = System.Math.Min(canCraftByHerbs, canCraftByGold);
        if (canCraft == int.MaxValue) canCraft = 0;

        string costStr = $"{Recipe.herbCost} Herbs";
        if (Recipe.goldCost > 0) costStr += $"  {Recipe.goldCost} Gold";
        if (costText    != null) costText.text     = costStr;
        if (canCraftText != null) canCraftText.text = $"x{canCraft}";

        if (selectButton != null) selectButton.interactable = canCraft > 0;
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }
}
