using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Building Config", menuName = "GatherMater/Building Config")]
public class BuildingConfig : ScriptableObject
{
    [Header("Identity")]
    public string buildingName;
    public Sprite icon;
    public ResourceType resourceType;

    [Header("Tiers")]
    [Tooltip("Index 0 = base level, index 1 = after first upgrade, etc.")]
    public UpgradeTier[] tiers;

    [Serializable]
    public class UpgradeTier
    {
        [Tooltip("Resources produced per minute at this tier.")]
        public float produceRatePerMinute = 1f;
        [Tooltip("Maximum resources stored before the building is full.")]
        public int storageCap = 50;
        [Tooltip("Cost to upgrade FROM this tier to the next. Leave empty on the last tier.")]
        public UpgradeCost[] upgradeCost;
    }

    [Serializable]
    public struct UpgradeCost
    {
        public ResourceType resource;
        public int amount;
    }
}
