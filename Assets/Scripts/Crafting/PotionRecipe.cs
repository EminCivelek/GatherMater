using UnityEngine;

[CreateAssetMenu(fileName = "New Potion Recipe", menuName = "GatherMater/Potion Recipe")]
public class PotionRecipe : ScriptableObject
{
    public ItemData output;
    public int      herbCost     = 5;
    public int      goldCost     = 0;
    public int      yield        = 1;
    public float    craftingTime = 10f;
}
