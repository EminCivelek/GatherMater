using System;
using System.Collections.Generic;
using UnityEngine;

public static class DuelManager
{
    const string PREF_DATE = "duel_used_date";
    const string PREF_IDS  = "duel_used_ids";

    // ── Honor delta (combat-outcome version) ─────────────────────────────
    // Called by CombatUI after the actual fight determines win/loss.
    // Beat a stronger opponent = more honor; lose to a weaker one = bigger penalty.
    public static int CalculateHonorDelta(bool won, int playerPower, int opponentPower)
    {
        float strengthRatio = playerPower > 0 ? (float)opponentPower / playerPower : 1f;

        if (won)
            return Mathf.RoundToInt(Mathf.Lerp(30f, 50f, Mathf.Clamp01((strengthRatio - 0.5f) * 2f)));
        else
            return -Mathf.RoundToInt(Mathf.Lerp(10f, 30f, Mathf.Clamp01(1f - (strengthRatio - 0.5f) * 2f)));
    }

    // Set true to bypass the daily cooldown during testing
    public static bool TestingMode = true;

    // ── Daily cooldown ────────────────────────────────────────────────────
    public static List<string> GetUsedIds()
    {
        RefreshIfNewDay();
        string raw = PlayerPrefs.GetString(PREF_IDS, "");
        if (string.IsNullOrEmpty(raw)) return new List<string>();
        return new List<string>(raw.Split(','));
    }

    public static bool IsUsed(string playerId) => !TestingMode && GetUsedIds().Contains(playerId);

    public static void MarkUsed(string playerId)
    {
        if (TestingMode) return;
        var used = GetUsedIds();
        if (!used.Contains(playerId)) used.Add(playerId);
        PlayerPrefs.SetString(PREF_IDS, string.Join(",", used));
    }

    static void RefreshIfNewDay()
    {
        string today   = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string stored  = PlayerPrefs.GetString(PREF_DATE, "");
        if (stored != today)
        {
            PlayerPrefs.SetString(PREF_DATE, today);
            PlayerPrefs.SetString(PREF_IDS,  "");
        }
    }
}
