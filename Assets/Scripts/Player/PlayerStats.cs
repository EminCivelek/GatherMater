using UnityEngine;
using System;
using System.Collections;

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
    public float attackSpeed  = 1f;

    [Header("Regen")]
    public float hpRegen = 1f;
    public float mpRegen = 1f;

    [Header("Class")]
    public PlayerClass selectedClass = PlayerClass.None;

    public const int MaxLevel = 60;

    public event Action OnStatsChanged;
    public event Action OnClassSelected;

    // ── Class-effective stats ────────────────────────────────────────────
    public float EffectiveMaxHP   => maxHP   * ClassData.HPMult(selectedClass);
    public float EffectiveMaxMP   => maxMP;
    public float EffectiveHPRegen => hpRegen * ClassData.HPRegenMult(selectedClass);
    public float EffectiveMPRegen => mpRegen * ClassData.MPRegenMult(selectedClass);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadLocal();
    }

    void Start() => StartCoroutine(RegenLoop());

    void OnApplicationQuit()             => SaveLocal();
    void OnApplicationPause(bool paused) { if (paused) SaveLocal(); }

    public int XPToNextLevel => Mathf.RoundToInt(100f * Mathf.Pow(level, 1.5f));

    public bool IsMaxLevel => level >= MaxLevel;

    public void SelectClass(PlayerClass cls)
    {
        if (cls == PlayerClass.None) return;
        selectedClass = cls;
        currentHP = EffectiveMaxHP;
        currentMP = EffectiveMaxMP;
        SaveLocal();
        OnStatsChanged?.Invoke();
        OnClassSelected?.Invoke();
    }

    public void GainXP(int amount)
    {
        amount = Mathf.RoundToInt(amount * ClassData.XPMult(selectedClass));
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
        attackSpeed  += 0.05f;
        hpRegen      += 0.1f;
        mpRegen      += 0.05f;
        currentHP     = EffectiveMaxHP;
        currentMP     = EffectiveMaxMP;
    }

    IEnumerator RegenLoop()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            if (!IsAlive) continue;

            bool changed = false;
            if (currentHP < EffectiveMaxHP) { currentHP = Mathf.Min(currentHP + EffectiveHPRegen, EffectiveMaxHP); changed = true; }
            if (currentMP < EffectiveMaxMP) { currentMP = Mathf.Min(currentMP + EffectiveMPRegen, EffectiveMaxMP); changed = true; }
            if (changed) OnStatsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Base player attack damage plus total weapon attack from equipment.
    /// </summary>
    // Transient potion bonuses — not saved, reset on scene load / death
    private float _bonusArmor;
    private float _bonusAttack;
    private float _bonusAttackSpeed;

    public float CombinedAttackDamage
    {
        get
        {
            float weaponAtk = Equipment.Instance?.TotalAttack ?? 0f;
            return (attackDamage + weaponAtk + _bonusAttack) * ClassData.AttackMult(selectedClass);
        }
    }

    /// <summary>
    /// Average of base player attack speed and equipped weapon attack speed.
    /// Falls back to base player speed when no weapon is equipped.
    /// </summary>
    public float CombinedAttackSpeed
    {
        get
        {
            float weaponSpeed = Equipment.Instance?.TotalAttackSpeed ?? 0f;
            float base_ = weaponSpeed <= 0f ? attackSpeed : (attackSpeed + weaponSpeed) * 0.5f;
            return (base_ + _bonusAttackSpeed) * ClassData.AttackSpeedMult(selectedClass);
        }
    }

    public float TakeDamage(float amount)
    {
        float equipArmor = Equipment.Instance != null ? Equipment.Instance.TotalArmor * ClassData.ArmorMult(selectedClass) : 0f;
        float armor = equipArmor + _bonusArmor;
        float reduced = amount * (100f / (100f + armor));
        currentHP = Mathf.Max(0f, currentHP - reduced);
        OnStatsChanged?.Invoke();
        return reduced;
    }

    public void LoseXPOnDeath()
    {
        if (IsMaxLevel) return;
        int penalty = Mathf.RoundToInt(XPToNextLevel * 0.05f);
        currentXP = currentXP >= penalty ? currentXP - penalty : 0;
        OnStatsChanged?.Invoke();
        SaveLocal();
    }

    public void RestoreFullHP()
    {
        currentHP = EffectiveMaxHP;
        currentMP = EffectiveMaxMP;
        OnStatsChanged?.Invoke();
    }

    public void ApplyPotion(ItemData data)
    {
        if (data == null) return;
        switch (data.potionEffect)
        {
            case PotionEffect.HealHP:
                currentHP = Mathf.Min(currentHP + data.potionEffectAmount, maxHP);
                OnStatsChanged?.Invoke();
                break;
            case PotionEffect.HealMP:
                currentMP = Mathf.Min(currentMP + data.potionEffectAmount, maxMP);
                OnStatsChanged?.Invoke();
                break;
            case PotionEffect.BoostHPRegen:
                StartCoroutine(TemporaryRegen(true,  data.potionEffectAmount, data.potionDuration));
                break;
            case PotionEffect.BoostMPRegen:
                StartCoroutine(TemporaryRegen(false, data.potionEffectAmount, data.potionDuration));
                break;
            case PotionEffect.BoostArmor:
                StartCoroutine(TemporaryStatBoost(PotionEffect.BoostArmor,        data.potionEffectAmount, data.potionDuration));
                break;
            case PotionEffect.BoostAttack:
                StartCoroutine(TemporaryStatBoost(PotionEffect.BoostAttack,       data.potionEffectAmount, data.potionDuration));
                break;
            case PotionEffect.BoostAttackSpeed:
                StartCoroutine(TemporaryStatBoost(PotionEffect.BoostAttackSpeed,  data.potionEffectAmount, data.potionDuration));
                break;
        }
    }

    private IEnumerator TemporaryRegen(bool isHP, float amount, float duration)
    {
        if (isHP) hpRegen += amount;
        else      mpRegen += amount;
        OnStatsChanged?.Invoke();

        yield return new WaitForSeconds(duration);

        if (isHP) hpRegen -= amount;
        else      mpRegen -= amount;
        OnStatsChanged?.Invoke();
    }

    private IEnumerator TemporaryStatBoost(PotionEffect stat, float amount, float duration)
    {
        ApplyStatDelta(stat,  amount);
        OnStatsChanged?.Invoke();
        yield return new WaitForSeconds(duration);
        ApplyStatDelta(stat, -amount);
        OnStatsChanged?.Invoke();
    }

    private void ApplyStatDelta(PotionEffect stat, float delta)
    {
        switch (stat)
        {
            case PotionEffect.BoostArmor:        _bonusArmor        += delta; break;
            case PotionEffect.BoostAttack:       _bonusAttack       += delta; break;
            case PotionEffect.BoostAttackSpeed:  _bonusAttackSpeed  += delta; break;
        }
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
        PlayerPrefs.SetFloat("PS_currentHP",    currentHP);
        PlayerPrefs.SetFloat("PS_currentMP",    currentMP);
        PlayerPrefs.SetFloat("PS_attackDamage", attackDamage);
        PlayerPrefs.SetFloat("PS_attackSpeed",  attackSpeed);
        PlayerPrefs.SetFloat("PS_hpRegen",      hpRegen);
        PlayerPrefs.SetFloat("PS_mpRegen",      mpRegen);
        PlayerPrefs.SetInt("PS_class",          (int)selectedClass);
        PlayerPrefs.SetString("PS_saveTime",    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
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
        hpRegen      = PlayerPrefs.GetFloat("PS_hpRegen",      1f);
        mpRegen      = PlayerPrefs.GetFloat("PS_mpRegen",      1f);
        selectedClass = (PlayerClass)PlayerPrefs.GetInt("PS_class", (int)PlayerClass.None);
        currentHP    = PlayerPrefs.GetFloat("PS_currentHP",    maxHP);
        currentMP    = PlayerPrefs.GetFloat("PS_currentMP",    maxMP);

        ApplyOfflineRegen();
    }

    void ApplyOfflineRegen()
    {
        if (currentHP <= 0f) return;
        if (!long.TryParse(PlayerPrefs.GetString("PS_saveTime", "0"), out long savedUnix) || savedUnix <= 0) return;

        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - savedUnix;
        if (elapsed <= 0) return;

        currentHP = Mathf.Min(currentHP + hpRegen * elapsed, maxHP);
        currentMP = Mathf.Min(currentMP + mpRegen * elapsed, maxMP);
    }

    // ── Cloud sync ───────────────────────────────────────────────────────
    public void ApplyCloudData(int lvl, int xp, long totalXp, float mHP, float mMP, float atk, float spd, float hpRgn, float mpRgn)
    {
        level         = lvl;
        currentXP     = xp;
        totalXpFarmed = totalXp;
        maxHP         = mHP;
        maxMP         = mMP;
        attackDamage  = atk;
        attackSpeed   = spd;
        hpRegen       = hpRgn;
        mpRegen       = mpRgn;
        currentHP     = Mathf.Min(currentHP, maxHP);
        currentMP     = Mathf.Min(currentMP, maxMP);
        SaveLocal();
        OnStatsChanged?.Invoke();
    }
}
