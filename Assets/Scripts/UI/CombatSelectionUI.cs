using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatSelectionUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject selectionPanel;
    [SerializeField] GameObject combatPanel;

    [Header("Mob List")]
    [SerializeField] MobConfig[] availableMobs;
    [SerializeField] Transform mobButtonContainer;
    [SerializeField] Button mobButtonPrefab;

    [Header("Pull Size")]
    [SerializeField] TextMeshProUGUI pullSizeLabel;
    [SerializeField] Button pullIncBtn;
    [SerializeField] Button pullDecBtn;

    [Header("Start")]
    [SerializeField] Button startFightBtn;
    [SerializeField] TextMeshProUGUI selectedMobLabel;

    MobConfig _selectedMob;
    int _pullSize = 1;
    const int MaxPull = 10;

    void Start()
    {
        selectionPanel.SetActive(true);
        combatPanel.SetActive(false);

        foreach (var mob in availableMobs)
        {
            MobConfig captured = mob;
            Button btn = Instantiate(mobButtonPrefab, mobButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = mob.mobName;
            btn.onClick.AddListener(() => SelectMob(captured));

            var le = btn.gameObject.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;
            le.flexibleWidth = 1f;

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.fontSize = 24f;
        }

        pullIncBtn.onClick.AddListener(() => SetPull(_pullSize + 1));
        pullDecBtn.onClick.AddListener(() => SetPull(_pullSize - 1));
        startFightBtn.onClick.AddListener(StartFight);

        if (availableMobs.Length > 0) SelectMob(availableMobs[0]);
        SetPull(1);
    }

    void SelectMob(MobConfig mob)
    {
        _selectedMob = mob;
        selectedMobLabel.text = $"{mob.mobName}  HP:{mob.maxHP}  ATK:{mob.attackDamage}  SPD:{mob.attackSpeed}/s  XP:{mob.xpReward}";
    }

    void SetPull(int value)
    {
        _pullSize = Mathf.Clamp(value, 1, MaxPull);
        pullSizeLabel.text = _pullSize.ToString();
    }

    void StartFight()
    {
        if (_selectedMob == null) return;
        CombatSession.SelectedMob = _selectedMob;
        CombatSession.PullSize = _pullSize;

        CombatManager.Instance.StartFight(_selectedMob, _pullSize);

        selectionPanel.SetActive(false);
        combatPanel.SetActive(true);
    }
}
