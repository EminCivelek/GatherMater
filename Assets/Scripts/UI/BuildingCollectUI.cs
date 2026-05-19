using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton panel that opens when the player interacts with a ProducerBuilding.
/// Wire up all references in the Inspector.
/// </summary>
public class BuildingCollectUI : MonoBehaviour
{
    public static BuildingCollectUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private Image           resourceIcon;
    [SerializeField] private TextMeshProUGUI storedText;        // "34 / 50 Wood"

    [Header("Collect")]
    [SerializeField] private Button          collectButton;
    [SerializeField] private TextMeshProUGUI collectButtonLabel;

    [Header("Upgrade")]
    [SerializeField] private GameObject      upgradeSection;    // hide when max level
    [SerializeField] private TextMeshProUGUI upgradeLabel;      // "Upgrade to Lv 2"
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Button          upgradeButton;

    // ── Private ───────────────────────────────────────────────────────────────────
    private ProducerBuilding              _current;
    private IsometricPlayerController     _player;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start() => _player = FindAnyObjectByType<IsometricPlayerController>();

    private void Update()
    {
        if (_current == null || !panel.activeSelf) return;
        if (_player != null && _player.IsMoving) { Close(); return; }
        RefreshStoredText(); // keep the stored count live so it doesn't feel stale
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open(ProducerBuilding building)
    {
        EquipmentUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        DailyMissionBoardUI.Instance?.Close();

        _current = building;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        panel.SetActive(false);
        _current = null;
    }

    // ── Button callbacks (wire in Inspector) ──────────────────────────────────────
    public void OnCollectClicked()
    {
        _current?.CollectAll();
        Refresh();
    }

    public void OnUpgradeClicked()
    {
        _current?.Upgrade();
        Refresh();
    }

    // ── Private helpers ───────────────────────────────────────────────────────────
    private void Refresh()
    {
        if (_current == null) return;

        var config = _current.Config;
        var tier   = _current.CurrentTier;

        buildingNameText.text = config.buildingName;
        if (resourceIcon != null) resourceIcon.sprite = config.icon;

        RefreshStoredText();

        // Upgrade section
        bool showUpgrade = !_current.IsMaxLevel;
        upgradeSection.SetActive(showUpgrade);
        if (showUpgrade)
        {
            upgradeLabel.text = $"Upgrade to Lv {_current.UpgradeLevel + 2}";

            var costs = _current.UpgradeCosts;
            var sb = new StringBuilder();
            foreach (var c in costs)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append($"{c.amount}x {c.resource}");
            }
            upgradeCostText.text = sb.ToString();
            upgradeButton.interactable = _current.CanUpgrade();
        }
    }

    private void RefreshStoredText()
    {
        var config = _current.Config;
        var tier   = _current.CurrentTier;
        int stored = _current.GetStoredAmount();

        storedText.text = $"{stored} / {tier.storageCap} {config.resourceType}";

        collectButton.interactable = stored > 0;
        collectButtonLabel.text = stored > 0 ? $"Collect {stored}" : "Nothing to collect";
    }
}
