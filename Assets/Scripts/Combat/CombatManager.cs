using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public event Action<string> OnCombatLog;
    public event Action OnCombatEnd;

    // Runtime mob state
    public class MobInstance
    {
        public MobConfig Config;
        public float CurrentHP;
        public float AttackTimer;
        public bool IsAlive => CurrentHP > 0f;
    }

    public List<MobInstance> Mobs { get; private set; } = new();

    bool _fightActive;
    float _playerAttackTimer;
    int _totalXPGained;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartFight(MobConfig config, int pullSize)
    {
        Mobs.Clear();
        for (int i = 0; i < pullSize; i++)
        {
            Mobs.Add(new MobInstance
            {
                Config = config,
                CurrentHP = config.maxHP,
                AttackTimer = 1f / config.attackSpeed
            });
        }

        _playerAttackTimer = 1f / PlayerStats.Instance.CombinedAttackSpeed;
        _totalXPGained = 0;
        _fightActive = true;
        StartCoroutine(CombatLoop());
    }

    IEnumerator CombatLoop()
    {
        while (_fightActive)
        {
            yield return null;
            float dt = Time.deltaTime;

            // Player auto-attack — targets first alive mob
            _playerAttackTimer -= dt;
            if (_playerAttackTimer <= 0f)
            {
                _playerAttackTimer = 1f / PlayerStats.Instance.CombinedAttackSpeed;
                MobInstance target = Mobs.Find(m => m.IsAlive);
                if (target != null)
                {
                    float dmg = PlayerStats.Instance.attackDamage;
                    target.CurrentHP = Mathf.Max(0f, target.CurrentHP - dmg);
                    OnCombatLog?.Invoke($"You hit {target.Config.mobName} for {dmg:F0} dmg.");

                    if (!target.IsAlive)
                    {
                        int xp = target.Config.xpReward;
                        _totalXPGained += xp;
                        PlayerStats.Instance.GainXP(xp);
                        OnCombatLog?.Invoke($"{target.Config.mobName} died! +{xp} XP");
                    }
                }
            }

            // Each mob attacks player
            foreach (var mob in Mobs)
            {
                if (!mob.IsAlive) continue;
                mob.AttackTimer -= dt;
                if (mob.AttackTimer <= 0f)
                {
                    mob.AttackTimer = 1f / mob.Config.attackSpeed;
                    float dmg = mob.Config.attackDamage;
                    PlayerStats.Instance.TakeDamage(dmg);
                    OnCombatLog?.Invoke($"{mob.Config.mobName} hits you for {dmg:F0} dmg.");
                }
            }

            // Check end conditions
            if (!PlayerStats.Instance.IsAlive)
            {
                _fightActive = false;
                LastXPGained = 0;
                PlayerStats.Instance.LoseXPOnDeath();
                OnCombatLog?.Invoke("You were defeated!");
                OnCombatEnd?.Invoke();
                yield break;
            }

            if (Mobs.TrueForAll(m => !m.IsAlive))
            {
                _fightActive = false;
                LastXPGained = _totalXPGained;
                OnCombatLog?.Invoke($"Victory! Total XP gained: {_totalXPGained}.");
                OnCombatEnd?.Invoke();
            }
        }
    }

    public int LastXPGained { get; private set; }

    public bool IsFighting => _fightActive;
    public bool PlayerWon  => _fightActive == false && PlayerStats.Instance.IsAlive;
}
