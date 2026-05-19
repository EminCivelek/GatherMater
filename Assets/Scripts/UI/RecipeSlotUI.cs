using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeSlotUI : MonoBehaviour
{
    [SerializeField] private Image      icon;
    [SerializeField] private TMP_Text   nameText;
    [SerializeField] private TMP_Text   costText;
    [SerializeField] private TMP_Text   durationText;
    [SerializeField] private Button     selectButton;
    [SerializeField] private GameObject highlight;

    public CraftingRecipe Recipe { get; private set; }

    public void Init(CraftingRecipe recipe, Action<CraftingRecipe> onSelect)
    {
        Recipe = recipe;
        if (icon         != null) icon.sprite    = recipe.icon;
        if (nameText     != null) nameText.text  = recipe.displayName;
        if (costText     != null) costText.text  = recipe.GetRequirementsText();
        if (durationText != null) durationText.text = FormatTime(recipe.craftDuration);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelect?.Invoke(recipe));
        SetHighlight(false);
        RefreshAffordability();
    }

    public void RefreshAffordability()
    {
        if (selectButton != null)
            selectButton.interactable = Recipe != null && Recipe.CanAfford();
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }

    private static string FormatTime(float seconds)
    {
        if (seconds >= 60f)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.CeilToInt(seconds % 60f);
            return s > 0 ? $"{m}m {s}s" : $"{m}m";
        }
        return $"{Mathf.CeilToInt(seconds)}s";
    }
}
