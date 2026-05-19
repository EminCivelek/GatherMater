using UnityEngine;
using UnityEngine.UI;

public class UpgradeInfoPanelUI : MonoBehaviour
{
    public static UpgradeInfoPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button     closeButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }
}
