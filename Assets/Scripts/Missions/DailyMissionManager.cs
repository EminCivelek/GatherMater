using System;
using System.Collections.Generic;
using UnityEngine;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance { get; private set; }

    public DailyMission[] Missions { get; private set; } = new DailyMission[3];

    public event Action OnMissionsChanged;

    private const string SaveKey = "DailyMissions_v1";
    private const string DayKey  = "DailyMissionsDay";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshIfNewDay();
    }

    private void Start()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnResourceAdded += OnResourceAdded;
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnResourceAdded -= OnResourceAdded;
    }

    private void OnResourceAdded(ResourceType type, int amount) => ReportGather(type, amount);

    private void OnApplicationQuit()             => Save();
    private void OnApplicationPause(bool paused) { if (paused) Save(); }

    // ── Public report API ─────────────────────────────────────────

    public void ReportKill()           => Report(MissionType.KillEnemies, 1);
    public void ReportCraft()          => Report(MissionType.CraftItems, 1);
    public void ReportUpgrade()        => Report(MissionType.UpgradeItems, 1);
    public void ReportCombatComplete() => Report(MissionType.CompleteCombats, 1);

    public void ReportGather(ResourceType type, int amount)
    {
        MissionType? mType = type switch
        {
            ResourceType.Wood  => MissionType.GatherWood,
            ResourceType.Stone => MissionType.GatherStone,
            ResourceType.Herbs => MissionType.GatherHerbs,
            _                  => (MissionType?)null
        };
        if (mType.HasValue) Report(mType.Value, amount);
    }

    public bool ClaimMission(int index)
    {
        if (index < 0 || index >= Missions.Length) return false;
        var m = Missions[index];
        if (!m.IsComplete || m.claimed) return false;

        Inventory.Instance?.Add(ResourceType.Gold, m.goldReward);
        PlayerStats.Instance?.GainXP(m.xpReward);
        m.claimed = true;
        Save();
        OnMissionsChanged?.Invoke();
        return true;
    }

    public float SecondsUntilReset()
    {
        var now  = DateTime.UtcNow;
        var next = now.Date.AddDays(1);
        return (float)(next - now).TotalSeconds;
    }

    // ── Internal ──────────────────────────────────────────────────

    private void Report(MissionType type, int amount)
    {
        bool changed = false;
        foreach (var m in Missions)
        {
            if (m.type != type || m.IsComplete || m.claimed) continue;
            m.progress = Mathf.Min(m.progress + amount, m.target);
            changed = true;
        }
        if (changed)
        {
            Save();
            OnMissionsChanged?.Invoke();
        }
    }

    private void RefreshIfNewDay()
    {
        int today = DayNumber();
        int saved = PlayerPrefs.GetInt(DayKey, -1);
        if (saved == today) { Load(); return; }

        GenerateMissions(today);
        PlayerPrefs.SetInt(DayKey, today);
        Save();
    }

    private static int DayNumber() =>
        (int)(DateTime.UtcNow - new DateTime(2024, 1, 1)).TotalDays;

    private void GenerateMissions(int seed)
    {
        var rng  = new System.Random(seed);
        var pool = new List<MissionType>((MissionType[])Enum.GetValues(typeof(MissionType)));
        Missions = new DailyMission[3];
        for (int i = 0; i < 3; i++)
        {
            int idx  = rng.Next(pool.Count);
            var type = pool[idx];
            pool.RemoveAt(idx);
            Missions[i] = BuildMission(type, rng);
        }
    }

    private static DailyMission BuildMission(MissionType type, System.Random rng)
    {
        (int target, int gold, int xp) = type switch
        {
            MissionType.KillEnemies     => (rng.Next(10, 31), rng.Next(150, 301), rng.Next(300,  601)),
            MissionType.CraftItems      => (rng.Next(2,  6),  rng.Next(200, 401), rng.Next(400,  801)),
            MissionType.UpgradeItems    => (rng.Next(1,  4),  rng.Next(300, 601), rng.Next(600, 1201)),
            MissionType.GatherWood      => (rng.Next(50, 201),rng.Next(100, 251), rng.Next(200,  501)),
            MissionType.GatherStone     => (rng.Next(50, 201),rng.Next(100, 251), rng.Next(200,  501)),
            MissionType.GatherHerbs     => (rng.Next(30, 101),rng.Next(150, 301), rng.Next(300,  601)),
            MissionType.CompleteCombats => (rng.Next(3,  9),  rng.Next(200, 401), rng.Next(400,  801)),
            _                           => (5, 200, 400)
        };
        return new DailyMission
        {
            type = type, target = target, progress = 0,
            claimed = false, goldReward = gold, xpReward = xp
        };
    }

    // ── Persistence ───────────────────────────────────────────────

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new SaveWrapper { missions = Missions }));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) { GenerateMissions(DayNumber()); return; }
        var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        if (wrapper?.missions != null && wrapper.missions.Length == 3)
            Missions = wrapper.missions;
        else
            GenerateMissions(DayNumber());
    }

    [Serializable]
    private class SaveWrapper { public DailyMission[] missions; }
}
