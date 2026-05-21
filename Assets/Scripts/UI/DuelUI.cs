using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;  // LeaderboardEntry
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DuelUI : MonoBehaviour
{
    public static DuelUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button     closeButton;

    [Header("Header")]
    [SerializeField] TMP_Text honorLabel;   // "Your Honor: 1,250"

    [Header("Rows")]
    [SerializeField] DuelOpponentRowUI[] opponentRows;  // 5 elements

    [Header("Status")]
    [SerializeField] TMP_Text statusText;

    [Header("Portrait")]
    [SerializeField] WarriorPortrait playerPortrait;

    [Header("Scene")]
    [SerializeField] string combatSceneName = "CombatScene";

    const string BOARD_ID = "honor-points";

    bool _isFetching;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        closeButton?.onClick.AddListener(Close);
        panel?.SetActive(false);

        if (HonorManager.Instance != null)
            HonorManager.Instance.OnHonorChanged += RefreshHonorLabel;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (HonorManager.Instance != null)
            HonorManager.Instance.OnHonorChanged -= RefreshHonorLabel;
    }

    // ── Open / Close ──────────────────────────────────────────────────────
    public void Open()
    {
        CraftingUI.Instance?.Close();
        AlchemyUI.Instance?.Close();
        UpgradeAnvilUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        EquipmentUI.Instance?.Close();
        DailyMissionBoardUI.Instance?.Close();
        ClassSelectionUI.Instance?.Close();

        panel?.SetActive(true);
        transform.SetAsLastSibling();
        RefreshHonorLabel();
        playerPortrait?.Refresh();
        _ = FetchOpponentsAsync();
    }

    public void Close()
    {
        panel?.SetActive(false);
    }

    void RefreshHonorLabel()
    {
        if (honorLabel == null || HonorManager.Instance == null) return;
        honorLabel.text = $"Your Honor: {HonorManager.Instance.HonorPoints:N0}";
    }

    // ── Fetch ─────────────────────────────────────────────────────────────
    async Task FetchOpponentsAsync()
    {
        if (_isFetching) return;
        _isFetching = true;

        foreach (var row in opponentRows) row.gameObject.SetActive(false);
        if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "Loading..."; }

        try
        {
            if (!UGSManager.Instance.IsSignedIn)
            {
                SetStatus("Not signed in."); return;
            }

            List<DuelOpponent> opponents = await GetNearbyOpponentsAsync();

            SetStatus(opponents.Count == 0 ? "No opponents found yet." : "");
            for (int i = 0; i < opponentRows.Length; i++)
            {
                if (i >= opponents.Count) break;
                opponentRows[i].gameObject.SetActive(true);
                opponentRows[i].Refresh(opponents[i], OnDuelClicked);
            }
        }
        catch (Exception e)
        {
            SetStatus("Failed to load opponents.");
            Debug.LogWarning($"[DuelUI] Fetch failed: {e.Message}");
        }
        finally
        {
            _isFetching = false;
        }
    }

    async Task<List<DuelOpponent>> GetNearbyOpponentsAsync()
    {
        string localId = UGSManager.Instance.PlayerId;

        // 1. Try honor leaderboard — nearby rank, then top-10
        var result = await TryFetchHonorOpponents(localId);
        if (result.Count > 0) return result;

        // 2. No one on honor board yet — fall back to power-score leaderboard
        return await TryFetchPowerScoreOpponents(localId);
    }

    async Task<List<DuelOpponent>> TryFetchHonorOpponents(string localId)
    {
        try
        {
            var options = new GetPlayerRangeOptions { RangeLimit = 5, IncludeMetadata = true };
            var page    = await LeaderboardsService.Instance.GetPlayerRangeAsync(BOARD_ID, options);
            var result  = ParseHonorEntries(page.Results, localId);
            if (result.Count > 0) return result;
        }
        catch { /* player not on board yet, fall through */ }

        try
        {
            var options = new GetScoresOptions { Limit = 10, IncludeMetadata = true };
            var page    = await LeaderboardsService.Instance.GetScoresAsync(BOARD_ID, options);
            return ParseHonorEntries(page.Results, localId);
        }
        catch { return new List<DuelOpponent>(); }
    }

    async Task<List<DuelOpponent>> TryFetchPowerScoreOpponents(string localId)
    {
        try
        {
            var options = new GetPlayerRangeOptions { RangeLimit = 5, IncludeMetadata = true };
            var page    = await LeaderboardsService.Instance.GetPlayerRangeAsync("power-score", options);
            var result  = ParsePowerScoreEntries(page.Results, localId);
            if (result.Count > 0) return result;
        }
        catch { /* player not on board yet, fall through */ }

        try
        {
            var options = new GetScoresOptions { Limit = 10, IncludeMetadata = true };
            var page    = await LeaderboardsService.Instance.GetScoresAsync("power-score", options);
            return ParsePowerScoreEntries(page.Results, localId);
        }
        catch { return new List<DuelOpponent>(); }
    }

    // Honor board: Score = honor, all other stats are in metadata
    static List<DuelOpponent> ParseHonorEntries(IEnumerable<LeaderboardEntry> entries, string localId)
    {
        var result = new List<DuelOpponent>();
        foreach (var entry in entries)
        {
            if (entry.PlayerId == localId) continue;
            if (result.Count >= 5) break;

            var meta = ParseMeta(entry.Metadata);
            result.Add(new DuelOpponent
            {
                playerId     = entry.PlayerId,
                playerName   = FormatName(entry),
                honorPoints  = (int)entry.Score,
                powerScore   = meta.ps,
                rank         = entry.Rank + 1,
                maxHP        = meta.hp,
                attackDamage = meta.atk,
                attackSpeed  = meta.spd
            });
        }
        return result;
    }

    // Power-score board: Score = power score, stats also in metadata
    static List<DuelOpponent> ParsePowerScoreEntries(IEnumerable<LeaderboardEntry> entries, string localId)
    {
        var result = new List<DuelOpponent>();
        foreach (var entry in entries)
        {
            if (entry.PlayerId == localId) continue;
            if (result.Count >= 5) break;

            var meta = ParseMeta(entry.Metadata);
            result.Add(new DuelOpponent
            {
                playerId     = entry.PlayerId,
                playerName   = FormatName(entry),
                honorPoints  = 0,
                powerScore   = (int)entry.Score,
                rank         = entry.Rank + 1,
                maxHP        = meta.hp,
                attackDamage = meta.atk,
                attackSpeed  = meta.spd
            });
        }
        return result;
    }

    static HonorMeta ParseMeta(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return new HonorMeta();
        try
        {
            var result = JsonConvert.DeserializeObject<HonorMeta>(raw);
            Debug.Log($"[DuelUI] Parsed meta — ps:{result?.ps} hp:{result?.hp} atk:{result?.atk} spd:{result?.spd}  (raw: {raw})");
            return result ?? new HonorMeta();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DuelUI] ParseMeta failed: {e.Message}  raw: {raw}");
            return new HonorMeta();
        }
    }

    static string FormatName(LeaderboardEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.PlayerName)) return entry.PlayerName;
        string id = entry.PlayerId;
        return "Player_" + (id.Length >= 4 ? id[^4..] : id);
    }

    // ── Duel ─────────────────────────────────────────────────────────────
    void OnDuelClicked(DuelOpponent opponent)
    {
        if (DuelManager.IsUsed(opponent.playerId)) return;

        DuelManager.MarkUsed(opponent.playerId);
        DuelSession.Begin(opponent);
        SceneManager.LoadScene(combatSceneName);
    }

    void SetStatus(string msg)
    {
        if (statusText == null) return;
        bool show = !string.IsNullOrEmpty(msg);
        statusText.gameObject.SetActive(show);
        if (show) statusText.text = msg;
    }

    // ── Nested metadata struct ─────────────────────────────────────────
    [Serializable]
    class HonorMeta
    {
        public int   ps;
        public float hp;
        public float atk;
        public float spd;
    }
}
