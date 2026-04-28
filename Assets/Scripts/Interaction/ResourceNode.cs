using System.Collections;
using UnityEngine;

public class ResourceNode : MonoBehaviour, IInteractable
{
    [Header("Resource Settings")]
    [SerializeField] private ResourceType resourceType = ResourceType.Wood;
    [SerializeField] private int yieldAmount = 1;
    [SerializeField] private float gatherTime = 3f;
    [SerializeField] private float respawnTime = 30f;

    [Header("Visuals")]
    [SerializeField] private Sprite interactionIcon;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // IInteractable
    public string InteractionLabel => $"Gather {resourceType}";
    public Sprite InteractionIcon   => interactionIcon;
    public float GatherDuration     => gatherTime;
    public bool IsAvailable         { get; private set; } = true;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnGatherComplete()
    {
        Inventory.Instance.Add(resourceType, yieldAmount);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        IsAvailable = false;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.3f); // fade out to show depleted

        yield return new WaitForSeconds(respawnTime);

        IsAvailable = true;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }
}
