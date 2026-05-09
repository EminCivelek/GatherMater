using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ScrollDrop
{
    public ScrollType type;
    [Range(0f, 100f)] public float dropChance = 10f;

    public bool Roll() => Random.Range(0f, 100f) <= dropChance;
}

[System.Serializable]
public class ResourceDrop
{
    public ResourceType type;
    [Range(0f, 100f)] public float dropChance = 100f;
    [Min(1)] public int baseAmount = 1;
    [Min(0)] public int randomExtra;

    // Returns 0 if chance roll fails, otherwise base + random extra.
    public int Roll()
    {
        if (Random.Range(0f, 100f) > dropChance) return 0;
        return baseAmount + (randomExtra > 0 ? Random.Range(0, randomExtra + 1) : 0);
    }
}

[CreateAssetMenu(menuName = "GatherMater/Mob Config")]
public class MobConfig : ScriptableObject
{
    public string mobName = "Slime";
    public Sprite mobSprite;
    public float maxHP = 50f;
    public float attackDamage = 5f;
    public float attackSpeed = 0.8f; // attacks per second
    public int xpReward = 20;

    [Header("Resource Drops")]
    public List<ResourceDrop> drops = new();

    [Header("Scroll Drops")]
    public List<ScrollDrop> scrollDrops = new();
}
