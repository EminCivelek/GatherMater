using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatUI : MonoBehaviour
{
    [Header("Player Sprite")]
    [SerializeField] Image playerSpriteImage;

    [Header("Warrior Portraits")]
    [SerializeField] WarriorPortrait playerPortrait;
    [SerializeField] WarriorPortrait opponentPortrait;

    [Header("Mob List")]
    [SerializeField] Transform mobListContainer;
    [SerializeField] MobRowUI mobRowPrefab;

    [Header("Live Combat Log (in-fight)")]
    [SerializeField] TextMeshProUGUI combatLogText;
    [SerializeField] int maxLogLines = 8;

    [Header("Result Modal")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TextMeshProUGUI resultLabel;
    [SerializeField] TextMeshProUGUI xpResultLabel;
    [SerializeField] TextMeshProUGUI dropsLabel;
    [SerializeField] Button fightAgainBtn;
    [SerializeField] Button returnBtn;
    [SerializeField] Button combatLogBtn;
    [SerializeField] Button selectNewMobBtn;
    [SerializeField] GameObject selectionPanel;

    [Header("Auto-Fight")]
    [SerializeField] Toggle     autoFightToggle;
    [SerializeField] GameObject autoFightLockOverlay;

    [Header("Auto-Fight Countdown")]
    [SerializeField] GameObject        countdownRow;
    [SerializeField] TextMeshProUGUI   countdownLabel;
    [SerializeField] RectTransform     barFill;

    [Header("Combat Log Panel")]
    [SerializeField] GameObject        logPanel;
    [SerializeField] TextMeshProUGUI   logPanelText;
    [SerializeField] ScrollRect        logScrollRect;
    [SerializeField] Button            closeLogPanelBtn;

    [Header("Scene")]
    [SerializeField] string villageSceneName = "VillageScene";

    public static bool AutoFightUnlocked { get; set; } = false;

    const float AutoFightDelay = 5f;
    Coroutine _countdown;

    readonly List<string> _liveLinesBuffer = new();
    readonly List<string> _fullLog         = new();
    readonly List<MobRowUI> _mobRows       = new();

    // ── Unity lifecycle ──────────────────────────────────────────────────────────
    void Start()
    {
        resultPanel.SetActive(false);
        if (logPanel != null) logPanel.SetActive(false);
        if (countdownRow != null) countdownRow.SetActive(false);

        CombatManager.Instance.OnCombatLog += AppendLog;
        CombatManager.Instance.OnCombatEnd += ShowResult;

        // Each button cancels any active countdown first
        fightAgainBtn.onClick.AddListener(() => { CancelCountdown(); FightAgain(); });
        returnBtn.onClick.AddListener(()         => { CancelCountdown(); ReturnToVillage(); });
        combatLogBtn.onClick.AddListener(()      => { CancelCountdown(); OpenLogPanel(); });
        selectNewMobBtn?.onClick.AddListener(()  => { CancelCountdown(); SelectNewMob(); });
        closeLogPanelBtn?.onClick.AddListener(CloseLogPanel);

        RefreshAutoFightLock();
        if (autoFightToggle != null)
            autoFightToggle.onValueChanged.AddListener(_ => { });

        playerPortrait?.Refresh();
        RefreshOpponentPortrait();
    }

    void OnEnable()
    {
        if (CombatManager.Instance == null) return;
        foreach (var row in _mobRows) Destroy(row.gameObject);
        _mobRows.Clear();
        SpawnMobRows();
    }

    void OnDestroy()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatLog -= AppendLog;
            CombatManager.Instance.OnCombatEnd -= ShowResult;
        }
    }

    void Update()
    {
        foreach (var row in _mobRows) row.Refresh();
    }

    // ── Mob rows ─────────────────────────────────────────────────────────────────
    void SpawnMobRows()
    {
        foreach (var mob in CombatManager.Instance.Mobs)
        {
            MobRowUI row = Instantiate(mobRowPrefab, mobListContainer);
            row.Init(mob);
            _mobRows.Add(row);
        }
    }

    // ── Log ──────────────────────────────────────────────────────────────────────
    void AppendLog(string line)
    {
        _fullLog.Add(line);
        _liveLinesBuffer.Add(line);
        if (_liveLinesBuffer.Count > maxLogLines)
            _liveLinesBuffer.RemoveAt(0);
        if (combatLogText != null)
            combatLogText.text = string.Join("\n", _liveLinesBuffer);
    }

    void OpenLogPanel()
    {
        if (logPanel == null) return;
        if (logPanelText != null)
            logPanelText.text = string.Join("\n", _fullLog);
        logPanel.SetActive(true);
        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void CloseLogPanel()
    {
        if (logPanel != null) logPanel.SetActive(false);
    }

    // ── Auto-fight ───────────────────────────────────────────────────────────────
    void RefreshAutoFightLock()
    {
        if (autoFightToggle      != null) autoFightToggle.interactable = AutoFightUnlocked;
        if (autoFightLockOverlay != null) autoFightLockOverlay.SetActive(!AutoFightUnlocked);
    }

    bool IsAutoFightOn => AutoFightUnlocked && autoFightToggle != null && autoFightToggle.isOn;

    // ── Countdown ────────────────────────────────────────────────────────────────
    IEnumerator RunCountdown()
    {
        if (countdownRow != null) countdownRow.SetActive(true);
        fightAgainBtn.interactable = false;

        float elapsed = 0f;
        while (elapsed < AutoFightDelay)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, AutoFightDelay - elapsed);
            float progress  = 1f - (elapsed / AutoFightDelay);   // 1 → 0

            if (countdownLabel != null)
                countdownLabel.text = $"Next fight in {remaining:F1}s…";

            if (barFill != null)
            {
                // Drain the bar right-to-left by shrinking anchorMax.x
                barFill.anchorMax = new Vector2(progress, barFill.anchorMax.y);
            }

            yield return null;
        }

        _countdown = null;
        resultPanel.SetActive(false);
        CloseLogPanel();
        RestartFight();
    }

    void CancelCountdown()
    {
        if (_countdown == null) return;
        StopCoroutine(_countdown);
        _countdown = null;
        if (countdownRow != null) countdownRow.SetActive(false);
        fightAgainBtn.interactable = true;
    }

    // ── Result modal ─────────────────────────────────────────────────────────────
    void ShowResult()
    {
        bool won = CombatManager.Instance.PlayerWon;

        resultPanel.SetActive(true);
        if (countdownRow != null) countdownRow.SetActive(false);

        // Duel mode: show honor result, skip XP/drops/auto-fight
        if (DuelSession.IsActive)
        {
            int playerPower = HonorManager.CalculatePowerScore();
            int honorDelta  = DuelManager.CalculateHonorDelta(won, playerPower, DuelSession.Opponent.powerScore);

            if (won) HonorManager.Instance?.AddHonor(honorDelta);
            else     HonorManager.Instance?.RemoveHonor(-honorDelta);

            resultLabel.text = won ? "Victory!" : "Defeated!";
            string sign = honorDelta >= 0 ? "+" : "";
            if (xpResultLabel  != null) xpResultLabel.text = $"{sign}{honorDelta} Honor";
            if (dropsLabel     != null) dropsLabel.text    = $"vs {DuelSession.Opponent.playerName}";
            if (fightAgainBtn  != null) fightAgainBtn.gameObject.SetActive(false);
            if (selectNewMobBtn != null) selectNewMobBtn.gameObject.SetActive(false);
            if (!won) PlayerStats.Instance.RestoreFullHP();
            return;
        }

        resultLabel.text = won ? "Victory!" : "Defeated!";

        if (xpResultLabel != null)
            xpResultLabel.text = won ? $"+{CombatManager.Instance.LastXPGained} XP" : "No XP gained";

        if (dropsLabel != null)
        {
            var resourceDrops = CombatManager.Instance.LastDrops;
            var scrollDrops   = CombatManager.Instance.LastScrollDrops;
            var itemDrops     = CombatManager.Instance.LastItemDrops;

            if (won && (resourceDrops.Count > 0 || scrollDrops.Count > 0 || itemDrops.Count > 0))
            {
                var sb = new System.Text.StringBuilder();
                foreach (var (type, amount) in resourceDrops)
                    sb.AppendLine($"• {type}  +{amount}");
                foreach (var (type, amount) in scrollDrops)
                    sb.AppendLine($"• {type} Scroll  x{amount}");
                foreach (var (name, amount) in itemDrops)
                    sb.AppendLine($"• {name}  x{amount}");
                dropsLabel.text = sb.ToString().TrimEnd();
            }
            else
            {
                dropsLabel.text = won ? "No drops." : "";
            }
        }

        if (won && IsAutoFightOn)
        {
            _countdown = StartCoroutine(RunCountdown());
        }
        else
        {
            fightAgainBtn.interactable = true;
        }

        if (!won)
            PlayerStats.Instance.RestoreFullHP();
    }

    void FightAgain()
    {
        DuelSession.Clear();
        resultPanel.SetActive(false);
        CloseLogPanel();
        RestartFight();
    }

    void RestartFight()
    {
        _liveLinesBuffer.Clear();
        _fullLog.Clear();
        if (combatLogText != null) combatLogText.text = "";

        foreach (var row in _mobRows) Destroy(row.gameObject);
        _mobRows.Clear();

        CombatManager.Instance.StartFight(CombatSession.SelectedMob, CombatSession.PullSize);
        SpawnMobRows();
    }

    void ReturnToVillage()
    {
        if (DuelSession.IsActive)
        {
            DuelSession.Clear();
            if (fightAgainBtn   != null) fightAgainBtn.gameObject.SetActive(true);
            if (selectNewMobBtn != null) selectNewMobBtn.gameObject.SetActive(true);
        }
        PlayerStats.Instance?.SaveLocal();
        Inventory.Instance?.Save();
        SceneManager.LoadScene(villageSceneName);
    }

    void SelectNewMob()
    {
        resultPanel.SetActive(false);
        CloseLogPanel();
        if (selectionPanel != null) selectionPanel.SetActive(true);
        gameObject.SetActive(false);   // hide CombalPanel; CombatSelectionUI reactivates it on next fight
    }

    void RefreshOpponentPortrait()
    {
        if (opponentPortrait == null) return;
        if (!DuelSession.IsActive) { opponentPortrait.gameObject.SetActive(false); return; }

        opponentPortrait.gameObject.SetActive(true);
        int ps = DuelSession.Opponent?.powerScore ?? 0;
        ItemTier tier = ps >= 5000 ? ItemTier.RizeanSteel
                      : ps >= 2000 ? ItemTier.Steel
                      : ps >= 500  ? ItemTier.Iron
                      : ItemTier.Wooden;
        opponentPortrait.ShowOutfitTier(tier);
    }
}
