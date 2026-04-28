using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance { get; private set; }

    const string KEY_BUILDING_TIMESTAMPS = "building_timestamps";
    const string KEY_PLAYER_PROGRESS     = "player_progress";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Building Timestamps ──────────────────────────────────────────────

    public async Task SaveBuildingTimestampAsync(string buildingId, long utcSeconds)
    {
        if (!UGSManager.Instance.IsSignedIn) return;

        var timestamps = await LoadBuildingTimestampsAsync();
        timestamps[buildingId] = utcSeconds;

        string json = JsonUtility.ToJson(new SerializableTimestamps(timestamps));
        await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
        {
            { KEY_BUILDING_TIMESTAMPS, json }
        });
    }

    public async Task<Dictionary<string, long>> LoadBuildingTimestampsAsync()
    {
        if (!UGSManager.Instance.IsSignedIn) return new Dictionary<string, long>();

        try
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { KEY_BUILDING_TIMESTAMPS }
            );

            if (result.TryGetValue(KEY_BUILDING_TIMESTAMPS, out var item))
            {
                string json = item.Value.GetAs<string>();
                var wrapper = JsonUtility.FromJson<SerializableTimestamps>(json);
                return wrapper.ToDictionary();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSave] Could not load timestamps: {e.Message}");
        }

        return new Dictionary<string, long>();
    }

    // ── Player Progress ──────────────────────────────────────────────────

    public async Task SavePlayerProgressAsync(string json)
    {
        if (!UGSManager.Instance.IsSignedIn) return;
        await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
        {
            { KEY_PLAYER_PROGRESS, json }
        });
    }

    public async Task<string> LoadPlayerProgressAsync()
    {
        if (!UGSManager.Instance.IsSignedIn) return null;
        try
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { KEY_PLAYER_PROGRESS }
            );
            if (result.TryGetValue(KEY_PLAYER_PROGRESS, out var item))
                return item.Value.GetAs<string>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSave] Could not load player progress: {e.Message}");
        }
        return null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    public static long UtcNowSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // ── Serialization shim (JsonUtility doesn't support Dictionary) ──────

    [Serializable]
    class SerializableTimestamps
    {
        public List<string> keys = new();
        public List<long> values = new();

        public SerializableTimestamps(Dictionary<string, long> dict)
        {
            foreach (var kv in dict) { keys.Add(kv.Key); values.Add(kv.Value); }
        }

        public Dictionary<string, long> ToDictionary()
        {
            var dict = new Dictionary<string, long>();
            for (int i = 0; i < keys.Count; i++) dict[keys[i]] = values[i];
            return dict;
        }
    }
}
