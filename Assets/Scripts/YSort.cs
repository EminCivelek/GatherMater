using UnityEngine;

/// <summary>
/// Add to any sprite that needs isometric depth sorting.
/// Objects higher on screen (larger Y) render behind objects lower on screen.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        // Multiply by 100 to give enough integer range between positions
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }
}
