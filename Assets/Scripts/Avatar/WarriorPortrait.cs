using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a stacked-sprite portrait of the warrior with the player's equipped items.
/// All Image children should be anchored stretch-fill so they overlay the same canvas.
/// </summary>
public class WarriorPortrait : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] WarriorAvatarConfig config;

    [Header("Base Body Layers (always visible)")]
    [SerializeField] Image baseBodyImage;
    [SerializeField] Image baseFaceImage;
    [SerializeField] Image baseHeadImage;
    [SerializeField] Image baseLeftArmImage;
    [SerializeField] Image baseRightArmImage;
    [SerializeField] Image baseLeftHandImage;
    [SerializeField] Image baseRightHandImage;
    [SerializeField] Image baseLeftLegImage;
    [SerializeField] Image baseRightLegImage;

    [Header("Equipment Layers (slot-driven)")]
    [SerializeField] Image hatLayer;         // Helmet
    [SerializeField] Image bodyLayer;        // Chest
    [SerializeField] Image leftArmLayer;     // Chest (sleeve)
    [SerializeField] Image rightArmLayer;    // Chest (sleeve)
    [SerializeField] Image leftHandLayer;    // Gloves
    [SerializeField] Image rightHandLayer;   // Gloves
    [SerializeField] Image leftShoeLayer;    // Boots
    [SerializeField] Image rightShoeLayer;   // Boots
    [SerializeField] Image swordLayer;       // MainHand
    [SerializeField] Image shieldLayer;      // OffHand

    Equipment _equipment;

    void OnEnable()
    {
        _equipment = Equipment.Instance;
        if (_equipment != null) _equipment.OnEquipmentChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged -= Refresh;
    }

    public void Refresh() => RefreshWith(Equipment.Instance);

    /// <summary>
    /// Refresh with a specific Equipment instance (useful for opponent portraits).
    /// Pass null to show just the naked base warrior.
    /// </summary>
    public void RefreshWith(Equipment equipment)
    {
        if (config == null) return;

        // ── Base body (always shown) ──────────────────────────────────────
        Set(baseBodyImage,      config.baseBody);
        Set(baseFaceImage,      config.baseFace);
        Set(baseHeadImage,      config.baseHead);
        Set(baseLeftArmImage,   config.baseLeftArm);
        Set(baseRightArmImage,  config.baseRightArm);
        Set(baseLeftHandImage,  config.baseLeftHand);
        Set(baseRightHandImage, config.baseRightHand);
        Set(baseLeftLegImage,   config.baseLeftLeg);
        Set(baseRightLegImage,  config.baseRightLeg);

        // ── Equipment layers ─────────────────────────────────────────────
        var helmet   = equipment?.GetEquipped(EquipSlot.Helmet);
        var chest    = equipment?.GetEquipped(EquipSlot.Chest);
        var gloves   = equipment?.GetEquipped(EquipSlot.Gloves);
        var boots    = equipment?.GetEquipped(EquipSlot.Boots);
        var mainHand = equipment?.GetEquipped(EquipSlot.MainHand);
        var offHand  = equipment?.GetEquipped(EquipSlot.OffHand);

        SetEquip(hatLayer,        helmet,   o => o.hat);
        SetEquip(bodyLayer,       chest,    o => o.body);
        SetEquip(leftArmLayer,    chest,    o => o.leftArm);
        SetEquip(rightArmLayer,   chest,    o => o.rightArm);
        SetEquip(leftHandLayer,   gloves,   o => o.leftHand);
        SetEquip(rightHandLayer,  gloves,   o => o.rightHand);
        SetEquip(leftShoeLayer,   boots,    o => o.leftShoe);
        SetEquip(rightShoeLayer,  boots,    o => o.rightShoe);
        SetEquip(swordLayer,      mainHand, o => o.sword);
        // OffHand can be shield OR off-hand weapon — sword sprite for weapon, shield for shield
        if (offHand != null && offHand.IsWeapon)
            SetEquip(shieldLayer, offHand, o => o.sword);
        else
            SetEquip(shieldLayer, offHand, o => o.shield);
    }

    /// <summary>Show a specific outfit tier regardless of equipped items (e.g. for opponent).</summary>
    public void ShowOutfitTier(ItemTier tier)
    {
        if (config == null) return;

        Set(baseBodyImage,      config.baseBody);
        Set(baseFaceImage,      config.baseFace);
        Set(baseHeadImage,      config.baseHead);
        Set(baseLeftArmImage,   config.baseLeftArm);
        Set(baseRightArmImage,  config.baseRightArm);
        Set(baseLeftHandImage,  config.baseLeftHand);
        Set(baseRightHandImage, config.baseRightHand);
        Set(baseLeftLegImage,   config.baseLeftLeg);
        Set(baseRightLegImage,  config.baseRightLeg);

        var outfit = config.GetOutfitForTier(tier);
        Set(hatLayer,       outfit.hat);
        Set(bodyLayer,      outfit.body);
        Set(leftArmLayer,   outfit.leftArm);
        Set(rightArmLayer,  outfit.rightArm);
        Set(leftHandLayer,  outfit.leftHand);
        Set(rightHandLayer, outfit.rightHand);
        Set(leftShoeLayer,  outfit.leftShoe);
        Set(rightShoeLayer, outfit.rightShoe);
        Set(swordLayer,     outfit.sword);
        Set(shieldLayer,    outfit.shield);
    }

    void SetEquip(Image img, ItemInstance item, Func<WarriorAvatarConfig.OutfitSprites, Sprite> selector)
    {
        if (img == null) return;
        if (item?.data == null) { img.enabled = false; return; }
        var sprite = selector(config.GetOutfitForTier(item.data.itemTier));
        img.sprite  = sprite;
        img.enabled = sprite != null;
    }

    void Set(Image img, Sprite sprite)
    {
        if (img == null) return;
        img.sprite  = sprite;
        img.enabled = sprite != null;
    }
}
