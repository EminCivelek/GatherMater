using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One row in the crafting UI recipe list.
/// Shows the recipe icon, name, cost, duration, and a Craft button.
/// </summary>
public class RecipeSlotUI : MonoBehaviour
{
    [SerializeField] private Image    icon;
    [SerializeField] private TMP_Text recipeName;
    [SerializeField] private TMP_Text requirementsText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private Button   craftButton;
    [SerializeField] private Button   infoButton;

    private CraftingRecipe          _recipe;
    private Action<CraftingRecipe>  _onCraft;

    public void Init(CraftingRecipe recipe, Action<CraftingRecipe> onCraft)
    {
        _recipe  = recipe;
        _onCraft = onCraft;

        if (recipe.icon != null) icon.sprite = recipe.icon;
        recipeName.text       = recipe.displayName;
        requirementsText.text = recipe.GetRequirementsText();
        durationText.text     = $"{recipe.craftDuration}s";

        craftButton.onClick.AddListener(() => _onCraft?.Invoke(_recipe));

        if (infoButton != null)
        {
            infoButton.onClick.AddListener(() =>
            {
                var outputItem = new ItemInstance
                {
                    itemDataId = recipe.outputItem?.name,
                    level      = 1,
                    data       = recipe.outputItem
                };
                ItemInfoPanelUI.Instance?.OpenForCraft(outputItem, () => _onCraft?.Invoke(_recipe));
            });
        }

        RefreshAffordability();
    }

    public void RefreshAffordability()
    {
        craftButton.interactable = _recipe != null && _recipe.CanAfford();
    }
}
