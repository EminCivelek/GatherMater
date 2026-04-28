using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Level")]
    public int level = 1;
    public int currentXP = 0;
    public long totalXpFarmed = 0;

    [Header("HP")]
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Header("MP")]
    public float maxMP = 50f;
    public float currentMP = 50f;

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackSpeed = 1f;

    public const int MaxLevel = 60;

    public event Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadLocal();
    }

    void OnApplicationQuit()             => SaveLocal();
    void OnApplicationPause(bool paused) { if (paused) SaveLocal(); }

    public int XPToNextLevel => Mathf.RoundToInt(100f * Mathf.Pow(level, 1.5f));

    public bool IsMaxLevel => level >= MaxLevel;

    public void GainXP(int amount)
    {
        totalXpFarmed += amount;

        if (IsMaxLevel)
        {
            OnStatsChanged?.Invoke();
            SaveLocal();
            return;
        }

        currentXP += amount;
        while (currentXP >= XPToNextLevel && !IsMaxLevel)
        {
            currentXP -= XPToNextLevel;
            level++;
            OnLevelUp();
        }

        if (IsMaxLevel) currentXP = 0;

        OnStatsChanged?.Invoke();
        SaveLocal();
    }

    void OnLevelUp()
    {
        maxHP        += 10f;
        maxMP        += 5f;
        attackDamage += 2f;
        currentHP     = maxHP;
        currentMP     = maxMP;
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(0f, currentHP - amount);
        OnStatsChanged?.Invoke();
    }

    public void RestoreFullHP()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        OnStatsChanged?.Invoke();
    }

    public bool IsAlive => currentHP > 0f;

    // ── Local persistence ────────────────────────────────────────────────
    public void SaveLocal()
    {
        PlayerPrefs.SetInt("PS_level",          level);
        PlayerPrefs.SetInt("PS_currentXP",      currentXP);
        PlayerPrefs.SetString("PS_totalXP",     totalXpFarmed.ToString());
        PlayerPrefs.SetFloat("PS_maxHP",        maxHP);
        PlayerPrefs.SetFloat("PS_maxMP",        maxMP);
        PlayerPrefs.SetFloat("PS_attackDamage", attackDamage);
        PlayerPrefs.SetFloat("PS_attackSpeed",  attackSpeed);
        PlayerPrefs.Save();
    }

    void LoadLocal()
    {
        level        = PlayerPrefs.GetInt("PS_level", 1);
        currentXP    = PlayerPrefs.GetInt("PS_currentXP", 0);
        long.TryParse(PlayerPrefs.GetString("PS_totalXP", "0"), out totalXpFarmed);
        maxHP        = PlayerPrefs.GetFloat("PS_maxHP",        100f);
        maxMP        = PlayerPrefs.GetFloat("PS_maxMP",        50f);
        attackDamage = PlayerPrefs.GetFloat("PS_attackDamage", 10f);
        attackSpeed  = PlayerPrefs.GetFloat("PS_attackSpeed",  1f);
        currentHP    = maxHP;
        currentMP    = maxMP;
    }

    // ── Cloud sync ───────────────────────────────────────────────────────
    public void ApplyCloudData(int lvl, int xp, long totalXp, float mHP, float mMP, float atk, float spd)
    {
        level        = lvl;
        currentXP    = xp;
        totalXpFarmed = totalXp;
        maxHP        = mHP;
        maxMP        = mMP;
        attackDamage = atk;
        attackSpeed  = spd;
        currentHP    = maxHP;
        currentMP    = maxMP;
        SaveLocal();
        OnStatsChanged?.Invoke();
    }
}
