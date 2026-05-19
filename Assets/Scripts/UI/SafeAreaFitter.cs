using UnityEngine;

/// <summary>
/// Resizes this RectTransform to stay within the device's safe area (avoids notches,
/// home indicators, and status bars). Attach to any full-screen UI panel whose
/// children should be inset from the edges.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform       _rt;
    private Rect                _lastSafeArea;
    private ScreenOrientation   _lastOrientation;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != _lastSafeArea || Screen.orientation != _lastOrientation)
            Apply();
    }

    private void Apply()
    {
        _lastSafeArea    = Screen.safeArea;
        _lastOrientation = Screen.orientation;

        var sa = Screen.safeArea;
        var anchorMin = new Vector2(sa.x      / Screen.width,  sa.y      / Screen.height);
        var anchorMax = new Vector2((sa.x + sa.width) / Screen.width,
                                   (sa.y + sa.height) / Screen.height);

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
