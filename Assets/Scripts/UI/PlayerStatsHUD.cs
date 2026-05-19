using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;

public class PlayerStatsHUD : MonoBehaviour
{
    [Header("Name")]
    [SerializeField] TMP_Text playerNameText;

    [Header("HP")]
    [SerializeField] RectTransform hpFillRT;
    [SerializeField] TMP_Text      hpText;

    [Header("MP")]
    [SerializeField] RectTransform mpFillRT;
    [SerializeField] TMP_Text      mpText;

    [Header("XP")]
    [SerializeField] RectTransform xpFillRT;
    [SerializeField] TMP_Text      xpLevelText;
    [SerializeField] TMP_Text      xpText;

    void OnEnable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged += Refresh;
        if (UGSManager.Instance != null)
            UGSManager.Instance.OnSignedIn += RefreshName;
    }

    void OnDisable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= Refresh;
        if (UGSManager.Instance != null)
            UGSManager.Instance.OnSignedIn -= RefreshName;
    }

    void Start()
    {
        // Re-attempt subscription here in case PlayerStats.Instance was null in OnEnable
        // (OnEnable fires per-object and can run before PlayerStats.Awake sets its Instance).
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= Refresh;
            PlayerStats.Instance.OnStatsChanged += Refresh;
        }
        if (UGSManager.Instance != null)
        {
            UGSManager.Instance.OnSignedIn -= RefreshName;
            UGSManager.Instance.OnSignedIn += RefreshName;
        }
        RefreshName();
        Refresh();
    }

    void RefreshName()
    {
        if (playerNameText == null) return;
        string n = null;
        try { n = AuthenticationService.Instance?.PlayerName; } catch { }
        playerNameText.text = string.IsNullOrEmpty(n) ? "Player" : n;
    }

    void Refresh()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        float effHP = s.EffectiveMaxHP;
        float effMP = s.EffectiveMaxMP;
        SetFill(hpFillRT, effHP > 0 ? s.currentHP / effHP : 0f);
        if (hpText != null) hpText.text = $"{Mathf.CeilToInt(s.currentHP)}/{Mathf.RoundToInt(effHP)}";

        SetFill(mpFillRT, effMP > 0 ? s.currentMP / effMP : 0f);
        if (mpText != null) mpText.text = $"{Mathf.CeilToInt(s.currentMP)}/{Mathf.RoundToInt(effMP)}";

        int xpNeeded = s.XPToNextLevel;
        SetFill(xpFillRT, xpNeeded > 0 ? (float)s.currentXP / xpNeeded : 1f);
        if (xpLevelText != null) xpLevelText.text = $"Lv.{s.level}";
        if (xpText     != null) xpText.text = s.IsMaxLevel ? "MAX" : $"{s.currentXP}/{xpNeeded}";
    }

    static void SetFill(RectTransform rt, float pct)
    {
        if (rt == null) return;
        var amax = rt.anchorMax;
        amax.x = Mathf.Clamp01(pct);
        rt.anchorMax = amax;
    }
}
