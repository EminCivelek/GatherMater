using UnityEngine;

[CreateAssetMenu(menuName = "GatherMater/Mob Config")]
public class MobConfig : ScriptableObject
{
    public string mobName = "Slime";
    public Sprite mobSprite;
    public float maxHP = 50f;
    public float attackDamage = 5f;
    public float attackSpeed = 0.8f; // attacks per second
    public int xpReward = 20;
}
