using System;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HonorManager : MonoBehaviour
{
    public static HonorManager Instance { get; private set; }

    const string PREF_KEY = "HonorPoints";
    const string BOARD_ID = "honor-points";

    public int HonorPoints { get; private set; }

    public event Action OnHonorChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        HonorPoints = PlayerPrefs.GetInt(PREF_KEY, 0);
    }

    void OnEnable()  => SceneManager.activeSceneChanged += OnSceneChanged;
    void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

    void OnSceneChanged(Scene from, Scene to)
    {
        if (to.name == "VillageScene") SubmitToLeaderboard();
    }

    public void AddHonor(int amount)
    {
        HonorPoints = Math.Max(0, HonorPoints + amount);
        Save();
        SubmitToLeaderboard();
        OnHonorChanged?.Invoke();
    }

    public void RemoveHonor(int amount)
    {
        HonorPoints = Math.Max(0, HonorPoints - amount);
        Save();
        SubmitToLeaderboard();
        OnHonorChanged?.Invoke();
    }

    // Called by PlayerProgressService to restore from cloud save
    public void SetFromCloud(int value)
    {
        HonorPoints = Math.Max(0, value);
        PlayerPrefs.SetInt(PREF_KEY, HonorPoints);
        OnHonorChanged?.Invoke();
    }

    void Save() => PlayerPrefs.SetInt(PREF_KEY, HonorPoints);

    public void SubmitToLeaderboard() => _ = SubmitAsync();

    async Task SubmitAsync()
    {
        if (UGSManager.Instance == null || !UGSManager.Instance.IsSignedIn) return;

        int   ps  = CalculatePowerScore();
        float hp  = PlayerStats.Instance?.EffectiveMaxHP        ?? 0f;
        float atk = PlayerStats.Instance?.CombinedAttackDamage  ?? 0f;
        float spd = PlayerStats.Instance?.CombinedAttackSpeed   ?? 0f;

        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(
                BOARD_ID, HonorPoints,
                new AddPlayerScoreOptions { Metadata = new { ps, hp, atk, spd } }
            );
            Debug.Log($"[Honor] Submitted — Honor: {HonorPoints}  PowerScore: {ps}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Honor] Submit failed: {e.Message}");
        }
    }

    public static int CalculatePowerScore()
    {
        if (PlayerStats.Instance == null) return 0;
        double score = PlayerStats.Instance.level * 500;
        if (Equipment.Instance != null)
        {
            score += Equipment.Instance.TotalAttack      * 10;
            score += Equipment.Instance.TotalArmor       * 10;
            score += Equipment.Instance.TotalAttackSpeed * 100;
        }
        return (int)Math.Floor(score);
    }
}
