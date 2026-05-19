using System;
using UnityEngine;

public class UpgradeAnvil : MonoBehaviour, IInteractable
{
    [Header("Anvil Info")]
    [SerializeField] private string anvilName = "Upgrade Anvil";
    [SerializeField] private Sprite anvilIcon;

    // ── IInteractable ────────────────────────────────────────────────────────────
    public string InteractionLabel => anvilName;
    public Sprite InteractionIcon  => anvilIcon;
    public float  GatherDuration   => 0f;
    public bool   IsAvailable      => true;

    public void OnGatherComplete() => UpgradeAnvilUI.Instance?.Open(this);

    // ── Public API ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Begins an upgrade: consumes the scroll, unequips the item (if equipped),
    /// and saves the job to UpgradeJobTracker. Call FinishUpgrade() when the timer expires.
    /// Returns false if preconditions are not met.
    /// </summary>
    public bool StartUpgrade(ItemInstance target, ItemInstance scroll, bool isEquipped)
    {
        if (target == null || scroll == null) return false;
        if (UpgradeJobTracker.IsActive) return false;

        // Unequip the item if it's currently worn
        if (isEquipped)
        {
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                if (Equipment.Instance?.GetEquipped(slot) == target)
                {
                    Equipment.Instance.Unequip(slot);
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(target.instanceId))
            target.instanceId = System.Guid.NewGuid().ToString("N");

        ItemInventory.Instance?.RemoveOne(scroll);
        UpgradeJobTracker.Save(target.instanceId, target.itemDataId, target.level + 1, DateTime.UtcNow.Ticks);

        Debug.Log($"[UpgradeAnvil] Started upgrading {target.data?.itemName} → Lv{target.level + 1}  ({UpgradeJobTracker.Duration}s)");
        return true;
    }

    /// <summary>
    /// Rolls success/fail and applies the outcome. Call only when the timer has elapsed.
    /// </summary>
    public UpgradeResult FinishUpgrade(ItemInstance target)
    {
        if (target == null) { UpgradeJobTracker.Clear(); return UpgradeResult.SafeFail; }
        int   targetLevel = target.level + 1;
        float rate        = ItemData.GetUpgradeSuccessRate(targetLevel);
        bool  success     = UnityEngine.Random.value < rate;
        UpgradeJobTracker.Clear();
        var result = ApplyOutcome(target, targetLevel, success);
        Debug.Log($"[UpgradeAnvil] {target.data?.itemName} → Lv{targetLevel}: {result} (rate={rate:P0})");
        return result;
    }

    /// <summary>
    /// Core outcome logic, also callable as a static fallback when no anvil instance exists.
    /// Failure zones: targetLevel ≤3 = safe, ≤15 = downgrade, >15 = destroy.
    /// </summary>
    public static UpgradeResult ApplyOutcome(ItemInstance target, int targetLevel, bool success)
    {
        if (success)
        {
            target.level++;
            ItemInventory.Instance?.NotifyItemUpgraded();
            DailyMissionManager.Instance?.ReportUpgrade();
            return UpgradeResult.Success;
        }

        if (targetLevel <= 3)
            return UpgradeResult.SafeFail;

        if (targetLevel <= 15)
        {
            target.level = Mathf.Max(1, target.level - 1);
            ItemInventory.Instance?.NotifyItemUpgraded();
            return UpgradeResult.Downgrade;
        }

        ItemInventory.Instance?.RemoveOne(target);
        return UpgradeResult.Destroy;
    }
}
