using UnityEngine;

// Set before loading CombatScene; read by CombatSelectionUI and CombatUI
public static class DuelSession
{
    public static bool         IsActive { get; private set; }
    public static DuelOpponent Opponent { get; private set; }

    public static void Begin(DuelOpponent opponent)
    {
        IsActive = true;
        Opponent = opponent;
    }

    public static void Clear()
    {
        IsActive = false;
        Opponent = null;
    }

    // Build a runtime MobConfig using the opponent's actual stats from leaderboard metadata.
    // Falls back to power-score scaling only when no metadata was available.
    public static MobConfig CreateMobConfig()
    {
        var config = ScriptableObject.CreateInstance<MobConfig>();

        config.mobName  = Opponent?.playerName ?? "Opponent";
        config.xpReward = 0;   // no XP from duels

        if (Opponent != null && Opponent.HasRealStats)
        {
            config.maxHP        = Opponent.maxHP;
            config.attackDamage = Opponent.attackDamage;
            config.attackSpeed  = Mathf.Clamp(Opponent.attackSpeed, 0.1f, 5f);
            Debug.Log($"[DuelSession] Using real stats — HP:{config.maxHP} ATK:{config.attackDamage} SPD:{config.attackSpeed}");
        }
        else
        {
            // Fallback when metadata is missing: mirror player stats for a fair fight
            config.maxHP        = PlayerStats.Instance?.EffectiveMaxHP       ?? 100f;
            config.attackDamage = PlayerStats.Instance?.CombinedAttackDamage ?? 10f;
            config.attackSpeed  = Mathf.Clamp(PlayerStats.Instance?.CombinedAttackSpeed ?? 1f, 0.1f, 5f);
            Debug.LogWarning($"[DuelSession] No metadata for {Opponent?.playerName} — mirroring player stats as fallback");
        }

        return config;
    }
}
