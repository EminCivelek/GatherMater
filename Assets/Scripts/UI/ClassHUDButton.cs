using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClassHUDButton : MonoBehaviour
{
    [SerializeField] private Button          button;
    [SerializeField] private TextMeshProUGUI labelText;

    private void OnEnable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged += Refresh;
    }

    private void OnDisable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= Refresh;
    }

    private void Start()
    {
        button?.onClick.AddListener(() => ClassSelectionUI.Instance?.Open());
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= Refresh;
            PlayerStats.Instance.OnStatsChanged += Refresh;
        }
        Refresh();
    }

    private void Refresh()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        bool show = s.level >= 5;
        gameObject.SetActive(show);

        if (labelText != null)
        {
            labelText.text = s.selectedClass == PlayerClass.None
                ? "Class"
                : ClassData.DisplayName(s.selectedClass);
        }
    }
}
