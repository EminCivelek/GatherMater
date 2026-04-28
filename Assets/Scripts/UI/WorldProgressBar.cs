using UnityEngine;

/// <summary>
/// World-space progress bar that floats above the player while gathering.
/// Attach to a World Space Canvas child of the player.
/// </summary>
public class WorldProgressBar : MonoBehaviour
{
    [SerializeField] private ProgressBar progressBar;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

    private PlayerInteraction playerInteraction;
    private Transform         playerTransform;

    private void Start()
    {
        playerInteraction = GetComponentInParent<PlayerInteraction>();
        playerTransform   = playerInteraction != null
            ? playerInteraction.transform
            : transform.parent;

        if (playerInteraction != null)
        {
            playerInteraction.OnGatherStarted  += Show;
            playerInteraction.OnGatherProgress += SetProgress;
            playerInteraction.OnGatherFinished += Hide;
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerInteraction == null) return;
        playerInteraction.OnGatherStarted  -= Show;
        playerInteraction.OnGatherProgress -= SetProgress;
        playerInteraction.OnGatherFinished -= Hide;
    }

    private void LateUpdate()
    {
        if (playerTransform != null)
            transform.position = playerTransform.position + offset;
    }

    private void Show()
    {
        gameObject.SetActive(true);
        SetProgress(0f);
    }

    private void SetProgress(float t)
    {
        if (progressBar != null)
            progressBar.BarValue = t * 100f;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
