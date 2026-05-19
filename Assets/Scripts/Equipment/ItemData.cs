using UnityEngine;

/// <summary>
/// Defines an equippable item template. Stats scale linearly with item level.
/// Create via: right-click in Project → GatherMater → Item Data
/// Place all ItemData assets inside Assets/Resources/Items/ for runtime lookup.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "GatherMater/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string        itemName;
    public Sprite        icon;
    public ItemCategory  category;
    public EquipSlot     primarySlot;

    [Header("Weapon Options")]
    public WeaponType weaponType;
    [Tooltip("Two-handed weapons lock the off-hand slot when equipped in the main hand.")]
    public bool isTwoHanded;
    [Tooltip("One-handed weapons can be equipped in either the main hand or off-hand.")]
    public bool canEquipOffHand;

    [Header("Weapon Stats (scale per level)")]
    public float baseAttack;
    public float attackPerLevel;
    public float baseAttackSpeed;
    public float attackSpeedPerLevel;

    [Header("On-Hit Effects")]
    public float onHitBonusDamage;
    public float onHitHPRecovery;
    public float onHitMPRecovery; // placeholder — MP system coming with XP/skill update

    [Header("Armor Stats (scale per level)")]
    public float baseArmor;
    public float armorPerLevel;

    [Header("Upgrade")]
    public ItemTier itemTier;

    public int MaxUpgradeLevel => itemTier switch
    {
        ItemTier.Wooden      => 5,
        ItemTier.Iron        => 10,
        ItemTier.Steel       => 15,
        ItemTier.RizeanSteel => 21,
        _                    => 10
    };

    // Returns 0–1 success chance for upgrading TO targetLevel.
    // Curve: –4.75% per level, floored at 5% (reached at +20→+21).
    public static float GetUpgradeSuccessRate(int targetLevel) =>
        Mathf.Clamp(1f - (targetLevel - 1) * 0.0475f, 0.05f, 1f);

    [Header("Sell")]
    public int sellPrice;

    [Header("Speed Up")]
    [Tooltip("Minutes to cut from an active crafting or upgrade job. Only used for SpeedUp category items.")]
    public int speedUpMinutes = 1;

    [Header("Potion")]
    public PotionEffect potionEffect;
    public float        potionEffectAmount;
    [Tooltip("0 = instant. Timed effects boost regen for this many seconds.")]
    public float        potionDuration;

    // ── Stat getters ──────────────────────────────────────────────────────────────
    public float GetAttack(int level)      => baseAttack      + (level - 1) * attackPerLevel;
    public float GetAttackSpeed(int level) => baseAttackSpeed + (level - 1) * attackSpeedPerLevel;
    public float GetArmor(int level)       => baseArmor       + (level - 1) * armorPerLevel;
}
