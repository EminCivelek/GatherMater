using UnityEngine;

[CreateAssetMenu(fileName = "WarriorAvatarConfig", menuName = "GatherMater/Warrior Avatar Config")]
public class WarriorAvatarConfig : ScriptableObject
{
    [System.Serializable]
    public class OutfitSprites
    {
        public Sprite hat;
        public Sprite body;
        public Sprite leftArm;
        public Sprite rightArm;
        public Sprite leftHand;
        public Sprite rightHand;
        public Sprite leftShoe;
        public Sprite rightShoe;
        public Sprite sword;
        public Sprite shield;
    }

    [Header("Base Body (naked warrior — Front view)")]
    public Sprite baseBody;
    public Sprite baseFace;
    public Sprite baseHead;
    public Sprite baseLeftArm;
    public Sprite baseRightArm;
    public Sprite baseLeftHand;
    public Sprite baseRightHand;
    public Sprite baseLeftLeg;
    public Sprite baseRightLeg;

    [Header("Light Tier Outfit — Wooden / Iron  (clothes_1)")]
    public OutfitSprites lightOutfit;

    [Header("Heavy Tier Outfit — Steel / Rizean Steel  (clothes_2)")]
    public OutfitSprites heavyOutfit;

    public OutfitSprites GetOutfitForTier(ItemTier tier) =>
        (tier == ItemTier.Steel || tier == ItemTier.RizeanSteel) ? heavyOutfit : lightOutfit;
}
