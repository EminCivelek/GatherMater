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

    [Header("Mob Card")]
    [SerializeField] Image  mobSpriteImage;
    [SerializeField] Button leftArrowBtn;
    [SerializeField] Button rightArrowBtn;

    [Header("Info")]
    [SerializeField] TextMeshProUGUI selectedMobLabel;

    [Header("Pull Size")]
    [SerializeField] TextMeshProUGUI pullSizeLabel;
    [SerializeField] Button          pullIncBtn;
    [SerializeField] Button          pullDecBtn;

    [Header("Start")]
    [SerializeField] Button startFightBtn;
    [SerializeField] Button returnToVillageBtn;

    private const string PrefKey = "LastMobIndex";

    int _currentIndex;
    int _pullSize = 1;
    const int MaxPull = 10;

    void Start()
    {
        // Duel path: skip selection, build opponent mob at runtime and start immediately
        if (DuelSession.IsActive)
        {
            var config = DuelSession.CreateMobConfig();
            CombatSession.SelectedMob = config;
            CombatSession.PullSize    = 1;
            CombatManager.Instance.StartFight(config, 1);
            selectionPanel.SetActive(false);
            combatPanel.SetActive(true);
            return;
        }

        selectionPanel.SetActive(true);
        combatPanel.SetActive(false);

        leftArrowBtn.onClick.AddListener(PrevMob);
        rightArrowBtn.onClick.AddListener(NextMob);
        pullIncBtn.onClick.AddListener(() => SetPull(_pullSize + 1));
        pullDecBtn.onClick.AddListener(() => SetPull(_pullSize - 1));
        startFightBtn.onClick.AddListener(StartFight);
        returnToVillageBtn?.onClick.AddListener(ReturnToVillage);

        int saved = PlayerPrefs.GetInt(PrefKey, 0);
        _currentIndex = availableMobs.Length > 0
            ? Mathf.Clamp(saved, 0, availableMobs.Length - 1)
            : 0;

        SetPull(1);
        ShowCurrent();
    }

    void PrevMob()
    {
        if (availableMobs.Length == 0) return;
        _currentIndex = (_currentIndex - 1 + availableMobs.Length) % availableMobs.Length;
        ShowCurrent();
    }

    void NextMob()
    {
        if (availableMobs.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % availableMobs.Length;
        ShowCurrent();
    }

    void ShowCurrent()
    {
        if (availableMobs.Length == 0) return;
        var mob = availableMobs[_currentIndex];

        if (mobSpriteImage != null)
        {
            mobSpriteImage.sprite  = mob.mobSprite;
            mobSpriteImage.enabled = mob.mobSprite != null;
        }

        if (selectedMobLabel != null)
            selectedMobLabel.text =
                $"{mob.mobName}\n" +
                $"HP: {mob.maxHP}   ATK: {mob.attackDamage}   SPD: {mob.attackSpeed:F1}/s   XP: {mob.xpReward}";
    }

    void SetPull(int value)
    {
        _pullSize = Mathf.Clamp(value, 1, MaxPull);
        if (pullSizeLabel != null) pullSizeLabel.text = _pullSize.ToString();
    }

    void ReturnToVillage()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");
    }

    void StartFight()
    {
        if (availableMobs.Length == 0) return;
        var mob = availableMobs[_currentIndex];

        PlayerPrefs.SetInt(PrefKey, _currentIndex);
        PlayerPrefs.Save();

        CombatSession.SelectedMob = mob;
        CombatSession.PullSize    = _pullSize;

        CombatManager.Instance.StartFight(mob, _pullSize);

        selectionPanel.SetActive(false);
        combatPanel.SetActive(true);
    }
}
