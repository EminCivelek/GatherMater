using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On-screen virtual joystick for mobile input.
/// Attach this to the joystick Background RectTransform.
/// Assign the Handle child RectTransform in the Inspector.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;

    // Normalised direction vector (-1..1 on each axis), read by the player controller
    public Vector2 Direction { get; private set; }

    private RectTransform background;
    private float radius;

    private void Awake()
    {
        background = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Radius is half the background width
        radius = background.sizeDelta.x * 0.5f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateHandle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateHandle(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direction = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    private void UpdateHandle(PointerEventData eventData)
    {
        // Convert screen position to local position relative to the background
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        // Clamp handle within the circular boundary
        Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
        handle.anchoredPosition = clamped;

        // Output normalised direction (-1..1)
        Direction = clamped / radius;
    }
}
