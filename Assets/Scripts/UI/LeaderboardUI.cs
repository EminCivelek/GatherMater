using System;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button     closeButton;

    [Header("Tabs")]
    [SerializeField] Button tabPowerScore;
    [SerializeField] Button tabTotalXP;
    [SerializeField] Color  tabActiveColor   = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] Color  tabInactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Rows")]
    [SerializeField] LeaderboardRowUI rowPrefab;
    [SerializeField] Transform        topListContainer;   // parent for top-10 rows
    [SerializeField] GameObject       separator;          // "Your Ranking" divider
    [SerializeField] Transform        playerRowContainer; // parent for local player row

    [Header("Status")]
    [SerializeField] TMP_Text statusText;

    const string BOARD_POWER_SCORE = "power-score";
    const string BOARD_TOTAL_XP    = "total-xp";

    string _currentBoard = BOARD_POWER_SCORE;
    bool   _isFetching;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        closeButton.onClick.AddListener(Close);
        tabPowerScore.onClick.AddListener(() => SwitchBoard(BOARD_POWER_SCORE));
        tabTotalXP.onClick.AddListener(()    => SwitchBoard(BOARD_TOTAL_XP));
        panel.SetActive(false);
    }

    // ── Open / Close ──────────────────────────────────────────────────────
    public void Open()
    {
        panel.SetActive(true);
        SwitchBoard(BOARD_POWER_SCORE);
    }

    public void Close() => panel.SetActive(false);

    // ── Tab switching ─────────────────────────────────────────────────────
    void SwitchBoard(string boardId)
    {
        _currentBoard = boardId;
        SetTabColor(tabPowerScore, boardId == BOARD_POWER_SCORE);
        SetTabColor(tabTotalXP,    boardId == BOARD_TOTAL_XP);
        _ = FetchAsync();
    }

    void SetTabColor(Button tab, bool active)
    {
        var colors = tab.colors;
        colors.normalColor = active ? tabActiveColor : tabInactiveColor;
        tab.colors = colors;
    }

    // ── Fetch ─────────────────────────────────────────────────────────────
    async Task FetchAsync()
    {
        if (_isFetching) return;
        _isFetching = true;

        ClearContainer(topListContainer);
        ClearContainer(playerRowContainer);
        separator.SetActive(false);
        statusText.gameObject.SetActive(true);
        statusText.text = "Loading...";

        try
        {
            if (!UGSManager.Instance.IsSignedIn)
            {
                statusText.text = "Not signed in.";
                return;
            }

            var options = new GetScoresOptions { Limit = 10 };
            var page    = await LeaderboardsService.Instance.GetScoresAsync(_currentBoard, options);

            string localId     = UGSManager.Instance.PlayerId;
            bool   inTop10     = false;

            foreach (LeaderboardEntry entry in page.Results)
            {
                bool isLocal = entry.PlayerId == localId;
                if (isLocal) inTop10 = true;

                var row = Instantiate(rowPrefab, topListContainer);
                row.SetData(entry.Rank + 1, FormatName(entry, isLocal), entry.Score, isLocal);
            }

            statusText.gameObject.SetActive(false);

            if (!inTop10)
                await FetchPlayerRankAsync();
        }
        catch (Exception e)
        {
            statusText.text = "Failed to load rankings.";
            Debug.LogWarning($"[LeaderboardUI] Fetch failed: {e.Message}");
        }
        finally
        {
            _isFetching = false;
        }
    }

    async Task FetchPlayerRankAsync()
    {
        separator.SetActive(true);
        try
        {
            var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(_currentBoard);
            var row   = Instantiate(rowPrefab, playerRowContainer);
            row.SetData(entry.Rank + 1, "You", entry.Score, true);
        }
        catch
        {
            var row = Instantiate(rowPrefab, playerRowContainer);
            row.SetData(0, "You", 0, true);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    static void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    static string FormatName(LeaderboardEntry entry, bool isLocal)
    {
        if (isLocal) return "You";
        if (!string.IsNullOrEmpty(entry.PlayerName)) return entry.PlayerName;
        string id = entry.PlayerId;
        return "Player_" + (id.Length >= 4 ? id.Substring(id.Length - 4) : id);
    }
}
