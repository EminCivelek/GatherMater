using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 1.5f;

    // Events — UI listens to these
    public event System.Action<IInteractable> OnInteractableChanged;
    public event System.Action<float>         OnGatherProgress;  // 0..1
    public event System.Action               OnGatherStarted;
    public event System.Action               OnGatherFinished;

    private IInteractable currentInteractable;
    private Coroutine     gatherRoutine;

    private void Update()
    {
        ScanForInteractables();

        // Spacebar triggers the same action as the on-screen gather button
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            OnInteractButtonPressed();
    }

    private void ScanForInteractables()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        IInteractable nearest     = null;
        float         nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable == null || !interactable.IsAvailable) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest     = interactable;
            }
        }

        if (nearest != currentInteractable)
        {
            // Cancel any ongoing gather if we moved away
            if (gatherRoutine != null)
                StopGather();

            currentInteractable = nearest;
            OnInteractableChanged?.Invoke(currentInteractable);
        }
    }

    // Called by the UI interaction button
    public void OnInteractButtonPressed()
    {
        if (currentInteractable == null || !currentInteractable.IsAvailable) return;

        if (gatherRoutine != null)
            StopGather();
        else
            gatherRoutine = StartCoroutine(GatherRoutine(currentInteractable));
    }

    private IEnumerator GatherRoutine(IInteractable target)
    {
        OnGatherStarted?.Invoke();

        float elapsed  = 0f;
        float duration = target.GatherDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            OnGatherProgress?.Invoke(elapsed / duration);
            yield return null;
        }

        target.OnGatherComplete();
        gatherRoutine = null;
        OnGatherFinished?.Invoke();
    }

    private void StopGather()
    {
        if (gatherRoutine != null)
        {
            StopCoroutine(gatherRoutine);
            gatherRoutine = null;
        }
        OnGatherFinished?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
