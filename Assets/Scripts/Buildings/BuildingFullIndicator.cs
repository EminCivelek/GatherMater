using UnityEngine;

/// <summary>
/// Attach to a child GameObject centred on a ProducerBuilding sprite.
/// Shows a chest icon when the building's storage is full and production is
/// blocked. Tapping/clicking the icon collects the stored resources.
///
/// Setup per building:
///   1. Add a child GameObject named e.g. "FullIndicator".
///   2. Add this component to it (SpriteRenderer + CircleCollider2D are added automatically).
///   3. Assign the Chest sprite (Assets/Cartoon UI/Icons/Chest.png) in the Inspector.
///   4. Leave child position at (0, 0, 0) so it sits in the centre of the building.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class BuildingFullIndicator : MonoBehaviour
{
    [SerializeField] Sprite chestIcon;

    ProducerBuilding _building;
    SpriteRenderer   _sr;
    CircleCollider2D _col;
    float            _checkTimer;

    const float CheckInterval = 1f;

    void Awake()
    {
        _building = GetComponentInParent<ProducerBuilding>();
        _sr       = GetComponent<SpriteRenderer>();
        _col      = GetComponent<CircleCollider2D>();

        _sr.sprite = chestIcon;

        // Render above the building sprite on the same sorting layer
        var parentSR = transform.parent != null
            ? transform.parent.GetComponent<SpriteRenderer>()
            : null;
        if (parentSR != null)
        {
            _sr.sortingLayerID = parentSR.sortingLayerID;
            _sr.sortingOrder   = parentSR.sortingOrder + 1;
        }

        SetVisible(false);
    }

    void Update()
    {
        _checkTimer -= Time.deltaTime;
        if (_checkTimer <= 0f)
        {
            _checkTimer = CheckInterval;
            bool full = _building != null &&
                        _building.GetStoredAmount() >= _building.CurrentTier.storageCap;
            SetVisible(full);
        }

        if (!_col.enabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (_col.OverlapPoint(worldPoint))
                Collect();
        }
    }

    void Collect()
    {
        _building?.CollectAll();
        SetVisible(false);
        _checkTimer = 0f; // re-evaluate on the very next frame
    }

    void SetVisible(bool visible)
    {
        _sr.enabled  = visible;
        _col.enabled = visible;
    }
}
