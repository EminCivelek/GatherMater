using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MobRowUI : MonoBehaviour
{
    [SerializeField] Image mobSprite;
    [SerializeField] TextMeshProUGUI nameLabel;
    [SerializeField] RectTransform hpFillRT;
    [SerializeField] TextMeshProUGUI hpLabel;

    CombatManager.MobInstance _mob;

    public void Init(CombatManager.MobInstance mob)
    {
        _mob = mob;
        if (mobSprite != null && mob.Config.mobSprite != null)
            mobSprite.sprite = mob.Config.mobSprite;
        nameLabel.text = mob.Config.mobName;
        Refresh();
    }

    public void Refresh()
    {
        if (_mob == null) return;
        float pct = _mob.Config.maxHP > 0 ? _mob.CurrentHP / _mob.Config.maxHP : 0f;
        SetFill(hpFillRT, pct);
        if (hpLabel != null)
            hpLabel.text = $"{_mob.CurrentHP:F0} / {_mob.Config.maxHP:F0}";

        float alpha = _mob.IsAlive ? 1f : 0.35f;
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = alpha;
    }

    static void SetFill(RectTransform rt, float pct)
    {
        if (rt == null) return;
        var amax = rt.anchorMax;
        amax.x = Mathf.Clamp01(pct);
        rt.anchorMax = amax;
    }
}
