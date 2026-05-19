using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton panel that opens when the player interacts with a StorageBuilding.
/// Wire up all references in the Inspector.
/// </summary>
public class StorageUI : MonoBehaviour
{
    public static StorageUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI capacityText;     // "340 / 500 total resources"

    [Header("Upgrade")]
    [SerializeField] private GameObject      upgradeSection;
    [SerializeField] private TextMeshProUGUI upgradeLabel;     // "Upgrade to Lv 2 (600 capacity)"
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Button          upgradeButton;

    private StorageBuilding           _current;
    private IsometricPlayerController _player;

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
        RefreshCapacityText();
    }

    public void Open(StorageBuilding building)
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

    public void OnUpgradeClicked()
    {
        _current?.Upgrade();
        Refresh();
    }

    private void Refresh()
    {
        if (_current == null) return;
        buildingNameText.text = _current.Config.buildingName;
        RefreshCapacityText();

        bool showUpgrade = !_current.IsMaxLevel;
        upgradeSection.SetActive(showUpgrade);
        if (showUpgrade)
        {
            var nextTier = _current.Config.tiers[_current.UpgradeLevel + 1];
            upgradeLabel.text = $"Upgrade to Lv {_current.UpgradeLevel + 2}  ({nextTier.capacity}/resource)";

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

    private void RefreshCapacityText()
    {
        int cap = Inventory.Instance?.Capacity ?? 0;
        capacityText.text = $"Capacity: {cap} per resource";
    }
}
