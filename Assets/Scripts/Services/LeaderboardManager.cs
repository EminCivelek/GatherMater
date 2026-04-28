using System;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    const string BOARD_POWER_SCORE = "power-score";
    const string BOARD_TOTAL_XP    = "total-xp";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        if (Equipment.Instance != null)
            Equipment.Instance.OnEquipmentChanged += SubmitScores;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (Equipment.Instance != null)
            Equipment.Instance.OnEquipmentChanged -= SubmitScores;
    }

    // Submit whenever player returns to village (after combat) or changes gear
    void OnSceneChanged(Scene from, Scene to)
    {
        if (to.name == "VillageScene")
            SubmitScores();
    }

    public void SubmitScores() => _ = SubmitScoresAsync();

    async Task SubmitScoresAsync()
    {
        if (UGSManager.Instance == null || !UGSManager.Instance.IsSignedIn) return;

        double powerScore = CalculatePowerScore();
        double totalXp    = PlayerStats.Instance != null ? PlayerStats.Instance.totalXpFarmed : 0;

        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(BOARD_POWER_SCORE, powerScore);
            await LeaderboardsService.Instance.AddPlayerScoreAsync(BOARD_TOTAL_XP, totalXp);
            Debug.Log($"[Leaderboard] Submitted — PowerScore: {powerScore}  TotalXP: {totalXp}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Leaderboard] Submit failed: {e.Message}");
        }
    }

    // ── Power Score Formula ───────────────────────────────────────────────
    // Level × 500 + TotalAttack × 10 + TotalArmor × 10 + TotalAttackSpeed × 100
    static double CalculatePowerScore()
    {
        if (PlayerStats.Instance == null) return 0;

        double score = PlayerStats.Instance.level * 500;

        if (Equipment.Instance != null)
        {
            score += Equipment.Instance.TotalAttack      * 10;
            score += Equipment.Instance.TotalArmor       * 10;
            score += Equipment.Instance.TotalAttackSpeed * 100;
        }

        return Math.Floor(score);
    }
}
