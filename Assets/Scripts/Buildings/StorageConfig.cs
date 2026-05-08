using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Storage Config", menuName = "GatherMater/Storage Config")]
public class StorageConfig : ScriptableObject
{
    [Header("Identity")]
    public string buildingName;
    public Sprite icon;

    [Header("Tiers")]
    [Tooltip("Index 0 = base level. Each tier defines total resource capacity and cost to upgrade to the next tier.")]
    public StorageTier[] tiers;

    [Serializable]
    public class StorageTier
    {
        [Tooltip("Maximum units of each individual resource the player can hold at this tier.")]
        public int capacity = 1000;
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
