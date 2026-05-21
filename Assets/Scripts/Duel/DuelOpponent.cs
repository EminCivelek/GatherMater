using System;

[Serializable]
public class DuelOpponent
{
    public string playerId;
    public string playerName;
    public int    honorPoints;
    public int    powerScore;
    public int    rank;

    // Actual combat stats pulled from leaderboard metadata
    public float maxHP;
    public float attackDamage;
    public float attackSpeed;

    public bool HasRealStats => maxHP > 0f;
}
