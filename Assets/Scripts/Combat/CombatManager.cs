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

        _playerAttackTimer = 1f / PlayerStats.Instance.attackSpeed;
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
                _playerAttackTimer = 1f / PlayerStats.Instance.attackSpeed;
                MobInstance target = Mobs.Find(m => m.IsAlive);
                if (target != null)
                {
                    float dmg = PlayerStats.Instance.attackDamage;
                    target.CurrentHP = Mathf.Max(0f, target.CurrentHP - dmg);
                    OnCombatLog?.Invoke($"You hit {target.Config.mobName} for {dmg:F0} dmg.");

                    if (!target.IsAlive)
                        OnCombatLog?.Invoke($"{target.Config.mobName} died!");
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
                OnCombatLog?.Invoke("You were defeated!");
                OnCombatEnd?.Invoke();
                yield break;
            }

            if (Mobs.TrueForAll(m => !m.IsAlive))
            {
                _fightActive = false;
                int totalXP = 0;
                foreach (var mob in Mobs) totalXP += mob.Config.xpReward;
                LastXPGained = totalXP;
                PlayerStats.Instance.GainXP(totalXP);
                OnCombatLog?.Invoke($"Victory! Gained {totalXP} XP.");
                OnCombatEnd?.Invoke();
            }
        }
    }

    public int LastXPGained { get; private set; }

    public bool PlayerWon => _fightActive == false && PlayerStats.Instance.IsAlive;
}
