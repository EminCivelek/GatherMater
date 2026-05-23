using System;
using UnityEngine;

/// <summary>
/// Persists a single active weapon enchantment job across scenes via PlayerPrefs.
/// Duration formula: weaponLevel * 30 seconds (same as UpgradeJobTracker).
/// On failure the weapon keeps its previous enchantment — only write new values on success.
/// </summary>
public static class EnchantJobTracker
{
    const string KeyInstanceId  = "EnchantJob_InstanceId";
    const string KeyTicks       = "EnchantJob_Ticks";
    const string KeyWeaponLevel = "EnchantJob_WeaponLevel";
    const string KeyNewType     = "EnchantJob_NewType";
    const string KeyNewAmount   = "EnchantJob_NewAmount";

    public static bool   IsActive      => !string.IsNullOrEmpty(PlayerPrefs.GetString(KeyInstanceId, ""));
    public static string InstanceId    => PlayerPrefs.GetString(KeyInstanceId, "");
    public static int    WeaponLevel   => PlayerPrefs.GetInt(KeyWeaponLevel, 1);
    public static int    PendingType   => PlayerPrefs.GetInt(KeyNewType, 0);
    public static float  PendingAmount => PlayerPrefs.GetFloat(KeyNewAmount, 0f);

    static long StartTicks =>
        long.TryParse(PlayerPrefs.GetString(KeyTicks, "0"), out long t) ? t : 0;

    public static float Duration   => WeaponLevel * 30f;
    public static float Elapsed    => StartTicks == 0 ? 0f :
        (float)(DateTime.UtcNow - new DateTime(StartTicks, DateTimeKind.Utc)).TotalSeconds;
    public static float Remaining  => Mathf.Max(0f, Duration - Elapsed);
    public static float Progress   => Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 0f;
    public static bool  IsComplete => IsActive && Elapsed >= Duration;

    public static bool IsBeingEnchanted(string instanceId) =>
        IsActive && !string.IsNullOrEmpty(instanceId) && InstanceId == instanceId;

    public static void Save(string instanceId, int weaponLevel, int pendingType, float pendingAmount, long startTicks)
    {
        PlayerPrefs.SetString(KeyInstanceId,  instanceId);
        PlayerPrefs.SetInt(KeyWeaponLevel,    weaponLevel);
        PlayerPrefs.SetInt(KeyNewType,        pendingType);
        PlayerPrefs.SetFloat(KeyNewAmount,    pendingAmount);
        PlayerPrefs.SetString(KeyTicks,       startTicks.ToString());
        PlayerPrefs.Save();
    }

    public static void ApplySpeedUp(int minutes)
    {
        if (!IsActive) return;
        if (!long.TryParse(PlayerPrefs.GetString(KeyTicks, "0"), out long ticks)) return;
        ticks -= TimeSpan.FromMinutes(minutes).Ticks;
        Save(InstanceId, WeaponLevel, PendingType, PendingAmount, ticks);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KeyInstanceId);
        PlayerPrefs.DeleteKey(KeyTicks);
        PlayerPrefs.DeleteKey(KeyWeaponLevel);
        PlayerPrefs.DeleteKey(KeyNewType);
        PlayerPrefs.DeleteKey(KeyNewAmount);
        PlayerPrefs.Save();
    }
}
