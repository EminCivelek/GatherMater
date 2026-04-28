using System;
using UnityEngine;

/// <summary>
/// Represents one active crafting job. Stored as UTC ticks so it survives
/// app restarts — offline time is automatically counted.
/// Replace startTimeTicks source with a server timestamp when adding multiplayer.
/// </summary>
[Serializable]
public class CraftingJob
{
    /// <summary>Matches CraftingRecipe.name (the asset filename).</summary>
    public string recipeId;

    /// <summary>DateTime.UtcNow.Ticks at the moment crafting started.</summary>
    public long startTimeTicks;

    private DateTime StartTime => new DateTime(startTimeTicks, DateTimeKind.Utc);

    public float GetProgress(float duration)
    {
        if (duration <= 0f) return 1f;
        float elapsed = (float)(DateTime.UtcNow - StartTime).TotalSeconds;
        return Mathf.Clamp01(elapsed / duration);
    }

    public float GetRemainingSeconds(float duration)
    {
        float elapsed = (float)(DateTime.UtcNow - StartTime).TotalSeconds;
        return Mathf.Max(0f, duration - elapsed);
    }

    public bool IsComplete(float duration) =>
        (DateTime.UtcNow - StartTime).TotalSeconds >= duration;
}
