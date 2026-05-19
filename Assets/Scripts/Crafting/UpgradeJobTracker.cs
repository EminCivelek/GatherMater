using System;
using UnityEngine;

/// <summary>
/// Static helper that persists a single active upgrade job across scenes via PlayerPrefs.
/// Duration formula: targetLevel * 30 seconds (level 20 = 600 s = 10 min).
/// </summary>
public static class UpgradeJobTracker
{
    private const string KeyId         = "UpgradeJob_ItemId";
    private const string KeyInstanceId = "UpgradeJob_InstanceId";
    private const string KeyTicks      = "UpgradeJob_Ticks";
    private const string KeyLevel      = "UpgradeJob_TargetLevel";

    public static bool   IsActive          => !string.IsNullOrEmpty(PlayerPrefs.GetString(KeyId, ""));
    public static string ItemDataId        => PlayerPrefs.GetString(KeyId, "");
    public static string UpgradingInstance => PlayerPrefs.GetString(KeyInstanceId, "");
    public static int    TargetLevel       => PlayerPrefs.GetInt(KeyLevel, 0);

    private static long StartTicks =>
        long.TryParse(PlayerPrefs.GetString(KeyTicks, "0"), out long t) ? t : 0;

    public static float Duration   => TargetLevel * 30f;
    public static float Elapsed    => StartTicks == 0 ? 0f :
        (float)(DateTime.UtcNow - new DateTime(StartTicks, DateTimeKind.Utc)).TotalSeconds;
    public static float Remaining  => Mathf.Max(0f, Duration - Elapsed);
    public static float Progress   => Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 0f;
    public static bool  IsComplete => IsActive && Elapsed >= Duration;

    public static bool IsBeingUpgraded(string instanceId) =>
        IsActive && !string.IsNullOrEmpty(instanceId) && UpgradingInstance == instanceId;

    public static void Save(string instanceId, string itemDataId, int targetLevel, long startTicks)
    {
        PlayerPrefs.SetString(KeyInstanceId, instanceId);
        PlayerPrefs.SetString(KeyId, itemDataId);
        PlayerPrefs.SetInt(KeyLevel, targetLevel);
        PlayerPrefs.SetString(KeyTicks, startTicks.ToString());
        PlayerPrefs.Save();
    }

    public static void ApplySpeedUp(int minutes)
    {
        if (!IsActive) return;
        if (!long.TryParse(PlayerPrefs.GetString(KeyTicks, "0"), out long ticks)) return;
        ticks -= TimeSpan.FromMinutes(minutes).Ticks;
        Save(UpgradingInstance, ItemDataId, TargetLevel, ticks);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KeyInstanceId);
        PlayerPrefs.DeleteKey(KeyId);
        PlayerPrefs.DeleteKey(KeyTicks);
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.Save();
    }
}
