using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows one active crafting job with a live progress bar and countdown.
/// </summary>
public class ActiveJobSlotUI : MonoBehaviour
{
    [SerializeField] private Image       icon;
    [SerializeField] private TMP_Text    recipeName;
    [SerializeField] private TMP_Text    timeRemainingText;
    [SerializeField] private ProgressBar progressBar;

    private CraftingJob    _job;
    private CraftingRecipe _recipe;

    public void Init(CraftingJob job, CraftingRecipe recipe)
    {
        _job    = job;
        _recipe = recipe;

        if (recipe.icon != null) icon.sprite = recipe.icon;
        recipeName.text = recipe.displayName;
    }

    private void Update()
    {
        if (_job == null || _recipe == null) return;

        float progress  = _job.GetProgress(_recipe.craftDuration);
        float remaining = _job.GetRemainingSeconds(_recipe.craftDuration);

        progressBar.BarValue     = progress * 100f;
        timeRemainingText.text   = remaining > 0f
            ? $"{Mathf.CeilToInt(remaining)}s"
            : "Done";
    }
}
