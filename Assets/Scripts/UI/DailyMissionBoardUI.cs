using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyMissionBoardUI : MonoBehaviour
{
    public static DailyMissionBoardUI Instance { get; private set; }

    [SerializeField] private GameObject          panel;
    [SerializeField] private Button              closeButton;
    [SerializeField] private TextMeshProUGUI     resetTimerText;
    [SerializeField] private DailyMissionRowUI[] missionRows;   // 3 elements

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        closeButton?.onClick.AddListener(Close);
        var mgr = DailyMissionManager.Instance;
        if (mgr != null) mgr.OnMissionsChanged += RefreshRows;
    }

    private void OnDestroy()
    {
        var mgr = DailyMissionManager.Instance;
        if (mgr != null) mgr.OnMissionsChanged -= RefreshRows;
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf) UpdateTimer();
    }

    public void Open()
    {
        CraftingUI.Instance?.Close();
        AlchemyUI.Instance?.Close();
        UpgradeAnvilUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        EquipmentUI.Instance?.Close();
        ClassSelectionUI.Instance?.Close();
        DuelUI.Instance?.Close();
        if (panel != null) panel.SetActive(true);
        RefreshRows();
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void RefreshRows()
    {
        var mgr = DailyMissionManager.Instance;
        if (mgr == null) return;
        for (int i = 0; i < missionRows.Length && i < mgr.Missions.Length; i++)
            missionRows[i]?.Refresh(i, mgr.Missions[i]);
    }

    private void UpdateTimer()
    {
        if (resetTimerText == null) return;
        float secs = DailyMissionManager.Instance?.SecondsUntilReset() ?? 0f;
        int h = (int)(secs / 3600);
        int m = (int)(secs % 3600 / 60);
        int s = (int)(secs % 60);
        resetTimerText.text = $"Resets in {h:D2}:{m:D2}:{s:D2}";
    }
}
