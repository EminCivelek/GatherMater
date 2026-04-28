using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple progress bar driven by an Image (Fill method: Horizontal).
/// Set BarValue (0–100) to update the fill.
/// Assign fillImage in the Inspector — use any sprite from Cartoon UI/Progress Bar.
/// </summary>
public class ProgressBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    private float barValue;
    public float BarValue
    {
        get => barValue;
        set
        {
            barValue = Mathf.Clamp(value, 0f, 100f);
            if (fillImage != null)
                fillImage.fillAmount = barValue / 100f;
        }
    }
}
