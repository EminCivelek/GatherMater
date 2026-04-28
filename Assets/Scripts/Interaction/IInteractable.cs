using UnityEngine;

public interface IInteractable
{
    string InteractionLabel { get; }
    Sprite InteractionIcon { get; }   // can be null
    float GatherDuration { get; }
    bool IsAvailable { get; }

    // Called when gather timer completes
    void OnGatherComplete();
}
