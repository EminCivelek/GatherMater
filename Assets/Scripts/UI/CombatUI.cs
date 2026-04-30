using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class CombatUI : MonoBehaviour
{
    [Header("Player HUD")]
    [SerializeField] Slider hpSlider;
    [SerializeField] TextMeshProUGUI hpLabel;
    [SerializeField] Slider mpSlider;
    [SerializeField] TextMeshProUGUI mpLabel;
    [SerializeField] Slider xpSlider;
    [SerializeField] TextMeshProUGUI xpLabel;
    [SerializeField] TextMeshProUGUI levelLabel;

    [Header("Player Sprite")]
    [SerializeField] Image playerSpriteImage;

    [Header("Mob List")]
    [SerializeField] Transform mobListContainer;
    [SerializeField] MobRowUI mobRowPrefab;

    [Header("Combat Log")]
    [SerializeField] TextMeshProUGUI combatLogText;
    [SerializeField] int maxLogLines = 8;

    [Header("Result Panel")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TextMeshProUGUI resultLabel;
    [SerializeField] TextMeshProUGUI xpResultLabel;
    [SerializeField] Button fightAgainBtn;
    [SerializeField] Button returnBtn;

    [Header("Scene")]
    [SerializeField] string villageSceneName = "SampleScene";

    readonly List<string> _logLines = new();
    readonly List<MobRowUI> _mobRows = new();

    void Start()
    {
        resultPanel.SetActive(false);

        Debug.Log("[CombatUI] Start — subscribing to CombatManager events");
        CombatManager.Instance.OnCombatLog += AppendLog;
        CombatManager.Instance.OnCombatEnd += ShowResult;
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged += RefreshPlayerHUD;

        fightAgainBtn.onClick.AddListener(FightAgain);
        returnBtn.onClick.AddListener(ReturnToVillage);
    }

    void OnEnable()
    {
        if (CombatManager.Instance == null) return;
        foreach (var row in _mobRows) Destroy(row.gameObject);
        _mobRows.Clear();
        SpawnMobRows();
        RefreshPlayerHUD();
    }

    void OnDestroy()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatLog -= AppendLog;
            CombatManager.Instance.OnCombatEnd -= ShowResult;
        }
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= RefreshPlayerHUD;
    }

    void SpawnMobRows()
    {
        Debug.Log($"[CombatUI] SpawnMobRows called. Mob count: {CombatManager.Instance.Mobs.Count}. Prefab: {mobRowPrefab}. Container: {mobListContainer}");
        foreach (var mob in CombatManager.Instance.Mobs)
        {
            MobRowUI row = Instantiate(mobRowPrefab, mobListContainer);
            Debug.Log($"[CombatUI] Spawned row for {mob.Config.mobName}");
            row.Init(mob);
            _mobRows.Add(row);
        }
    }

    void Update()
    {
        foreach (var row in _mobRows)
            row.Refresh();
    }

    void RefreshPlayerHUD()
    {
        PlayerStats ps = PlayerStats.Instance;
        if (ps == null) return;

        hpSlider.value = ps.currentHP / ps.maxHP;
        hpLabel.text = $"{ps.currentHP:F0} / {ps.maxHP:F0}";

        mpSlider.value = ps.currentMP / ps.maxMP;
        mpLabel.text = $"{ps.currentMP:F0} / {ps.maxMP:F0}";

        xpSlider.value = ps.XPToNextLevel > 0 ? (float)ps.currentXP / ps.XPToNextLevel : 0f;
        xpLabel.text = $"{ps.currentXP} / {ps.XPToNextLevel} XP";

        levelLabel.text = $"Lv {ps.level}";
    }

    void AppendLog(string line)
    {
        _logLines.Add(line);
        if (_logLines.Count > maxLogLines)
            _logLines.RemoveAt(0);
        combatLogText.text = string.Join("\n", _logLines);
    }

    void ShowResult()
    {
        Debug.Log($"[CombatUI] ShowResult called. ResultPanel: {resultPanel}. PlayerWon: {CombatManager.Instance.PlayerWon}");
        resultPanel.SetActive(true);
        bool won = CombatManager.Instance.PlayerWon;
        resultLabel.text = won ? "Victory!" : "Defeated!";
        if (xpResultLabel != null)
            xpResultLabel.text = won ? $"+{CombatManager.Instance.LastXPGained} XP" : "";
        fightAgainBtn.interactable = won;

        if (!won)
            PlayerStats.Instance.RestoreFullHP();
    }

    void FightAgain()
    {
        resultPanel.SetActive(false);
        _logLines.Clear();
        combatLogText.text = "";

        foreach (var row in _mobRows) Destroy(row.gameObject);
        _mobRows.Clear();

        CombatManager.Instance.StartFight(CombatSession.SelectedMob, CombatSession.PullSize);
        SpawnMobRows();
    }

    void ReturnToVillage()
    {
        PlayerStats.Instance?.SaveLocal();
        SceneManager.LoadScene(villageSceneName);
    }
}
