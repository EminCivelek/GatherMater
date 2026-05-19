using System;

[Serializable]
public class DailyMission
{
    public MissionType type;
    public int target;
    public int progress;
    public bool claimed;
    public int goldReward;
    public int xpReward;

    public bool IsComplete => progress >= target;

    public string Description => type switch
    {
        MissionType.KillEnemies     => $"Kill {target} enemies",
        MissionType.CraftItems      => $"Craft {target} item{(target > 1 ? "s" : "")}",
        MissionType.UpgradeItems    => $"Upgrade {target} item{(target > 1 ? "s" : "")}",
        MissionType.GatherWood      => $"Gather {target} Wood",
        MissionType.GatherStone     => $"Gather {target} Stone",
        MissionType.GatherHerbs     => $"Gather {target} Herbs",
        MissionType.CompleteCombats => $"Complete {target} combat{(target > 1 ? "s" : "")}",
        _                           => "Unknown mission"
    };
}
