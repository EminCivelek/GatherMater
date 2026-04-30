using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] Transform   mobButtonContainer;
    [SerializeField] Button      mobButtonPrefab;
    [SerializeField] Button      greenBtnPrefab;

    [Header("Pull Size")]
    [SerializeField] TextMeshProUGUI pullSizeLabel;
    [SerializeField] Button          pullIncBtn;
    [SerializeField] Button          pullDecBtn;

    [Header("Start")]
    [SerializeField] Button          startFightBtn;
    [SerializeField] TextMeshProUGUI selectedMobLabel;

    MobConfig _selectedMob;
    int       _pullSize = 1;
    const int MaxPull   = 10;

    readonly Dictionary<MobConfig, Button> _mobButtons = new();

    void Start()
    {
        selectionPanel.SetActive(true);
        combatPanel.SetActive(false);

        foreach (var mob in availableMobs)
        {
            Button btn = SpawnButton(mob, mobButtonPrefab);
            _mobButtons[mob] = btn;
        }

        pullIncBtn.onClick.AddListener(() => SetPull(_pullSize + 1));
        pullDecBtn.onClick.AddListener(() => SetPull(_pullSize - 1));
        startFightBtn.onClick.AddListener(StartFight);

        if (availableMobs.Length > 0) SelectMob(availableMobs[0]);
        SetPull(1);
    }

    void SelectMob(MobConfig mob)
    {
        if (_selectedMob == mob) return;

        // Swap previous back to default immediately — it is not being clicked
        if (_selectedMob != null && _mobButtons.TryGetValue(_selectedMob, out var prev))
            _mobButtons[_selectedMob] = SwapButton(prev, _selectedMob, mobButtonPrefab);

        _selectedMob = mob;
        selectedMobLabel.text = $"{mob.mobName}  HP:{mob.maxHP}  ATK:{mob.attackDamage}  SPD:{mob.attackSpeed}/s  XP:{mob.xpReward}";

        // Defer the green swap one frame so the click event finishes before we destroy the button
        StartCoroutine(SwapToGreenNextFrame(mob));
    }

    IEnumerator SwapToGreenNextFrame(MobConfig mob)
    {
        yield return null;
        if (greenBtnPrefab == null)
        {
            Debug.LogError("[CombatSelectionUI] greenBtnPrefab is not assigned in the Inspector!");
            yield break;
        }
        if (_mobButtons.TryGetValue(mob, out var current) && current != null)
            _mobButtons[mob] = SwapButton(current, mob, greenBtnPrefab);
    }

    // Destroys old button and instantiates prefab in its place, keeping sibling order.
    Button SwapButton(Button old, MobConfig mob, Button prefab)
    {
        if (old == null || prefab == null) return old;
        int       siblingIndex = old.transform.GetSiblingIndex();
        Transform parent       = old.transform.parent;
        Destroy(old.gameObject);
        return SpawnButton(mob, prefab, parent, siblingIndex);
    }

    Button SpawnButton(MobConfig mob, Button prefab, Transform parent = null, int siblingIndex = -1)
    {
        Button btn = Instantiate(prefab, parent != null ? parent : mobButtonContainer);

        if (siblingIndex >= 0)
            btn.transform.SetSiblingIndex(siblingIndex);

        btn.GetComponentInChildren<TextMeshProUGUI>().text = mob.mobName;

        MobConfig captured = mob;
        btn.onClick.AddListener(() => SelectMob(captured));

        var le = btn.gameObject.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 60f;
        le.flexibleWidth   = 1f;

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.fontSize = 24f;

        return btn;
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
        CombatSession.PullSize    = _pullSize;

        CombatManager.Instance.StartFight(_selectedMob, _pullSize);

        selectionPanel.SetActive(false);
        combatPanel.SetActive(true);
    }
}
