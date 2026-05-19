using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyMissionRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private RectTransform   progressFillRT;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button          claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;
    [SerializeField] private GameObject      claimedOverlay;

    private int _missionIndex;

    private void Start()
    {
        claimButton?.onClick.AddListener(OnClaim);
    }

    public void Refresh(int index, DailyMission mission)
    {
        _missionIndex = index;

        if (descriptionText != null) descriptionText.text = mission.Description;

        float fill = mission.target > 0 ? Mathf.Clamp01((float)mission.progress / mission.target) : 0f;
        if (progressFillRT != null)
        {
            progressFillRT.anchorMin = new Vector2(0f, 0f);
            progressFillRT.anchorMax = new Vector2(fill, 1f);
            progressFillRT.offsetMin = Vector2.zero;
            progressFillRT.offsetMax = Vector2.zero;
        }
        if (progressText != null) progressText.text = $"{mission.progress}/{mission.target}";

        if (rewardText != null) rewardText.text = $"+{mission.goldReward} Gold\n+{mission.xpReward} XP";

        bool canClaim = mission.IsComplete && !mission.claimed;
        if (claimButton != null)    claimButton.interactable = canClaim;
        if (claimButtonText != null) claimButtonText.text = mission.claimed ? "Claimed" : (mission.IsComplete ? "Claim!" : "Claim");
        if (claimedOverlay  != null) claimedOverlay.SetActive(mission.claimed);
    }

    private void OnClaim() => DailyMissionManager.Instance?.ClaimMission(_missionIndex);
}
