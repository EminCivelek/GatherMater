using System;
using UnityEngine;

/// <summary>
/// A single owned item — links to an ItemData template and stores its rolled level.
/// itemDataId matches the ItemData ScriptableObject asset name.
/// </summary>
[Serializable]
public class ItemInstance
{
    public string instanceId;   // unique per item, generated on first save
    public string itemDataId;
    public int    level      = 1;
    public int    stackCount = 1;   // >1 only for stackable categories (Scroll, Potion)

    [NonSerialized] public ItemData data;

    public EnchantmentType enchantmentType   = EnchantmentType.None;
    public float           enchantmentAmount = 0f;

    public bool IsStackable => data?.category == ItemCategory.Scroll
                            || data?.category == ItemCategory.Potion
                            || data?.category == ItemCategory.SpeedUp
                            || data?.category == ItemCategory.EnchantmentScroll;

    // ── Stat getters ──────────────────────────────────────────────────────────────
    public float GetAttack()      => data?.GetAttack(level)      ?? 0f;
    public float GetAttackSpeed() => data?.GetAttackSpeed(level) ?? 0f;
    public float GetArmor()       => data?.GetArmor(level)       ?? 0f;

    // ── Helpers ───────────────────────────────────────────────────────────────────
    public bool      IsWeapon       => data?.category    == ItemCategory.Weapon;
    public bool      IsShield       => data?.category    == ItemCategory.Shield;
    public bool      IsTwoHanded    => data?.isTwoHanded ?? false;
    public bool      CanEquipOffHand => data?.canEquipOffHand ?? false;
    public EquipSlot PrimarySlot    => data?.primarySlot ?? EquipSlot.MainHand;
}
