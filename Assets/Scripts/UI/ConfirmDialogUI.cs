using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// General-purpose modal dialog. Call ShowConfirm() for a two-button prompt
/// or ShowMessage() for a single-button info/error notice.
/// Pass showDontAskAgain=true to ShowConfirm to reveal the session-skip checkbox;
/// read DontShowAgainChecked after Confirm fires to know if the user ticked it.
/// </summary>
public class ConfirmDialogUI : MonoBehaviour
{
    public static ConfirmDialogUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   titleText;
    [SerializeField] private TMP_Text   messageText;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private TMP_Text   confirmLabel;
    [SerializeField] private Button     cancelButton;
    [SerializeField] private TMP_Text   cancelLabel;

    [Header("Don't Show Again")]
    [SerializeField] private GameObject dontShowAgainRow;
    [SerializeField] private Toggle     dontShowAgainToggle;

    public bool DontShowAgainChecked => dontShowAgainToggle != null && dontShowAgainToggle.isOn;

    private Action _onConfirm;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Close);
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Two-button confirmation prompt.</summary>
    public void ShowConfirm(string title, string message, Action onConfirm,
                            string confirmText = "Confirm", string cancelText = "Cancel",
                            bool showDontAskAgain = false)
    {
        titleText.text        = title;
        messageText.text      = message;
        confirmLabel.text     = confirmText;
        cancelLabel.text      = cancelText;
        cancelButton.gameObject.SetActive(true);
        _onConfirm            = onConfirm;

        if (dontShowAgainRow != null)
        {
            dontShowAgainRow.SetActive(showDontAskAgain);
            if (dontShowAgainToggle != null) dontShowAgainToggle.isOn = false;
        }

        panel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.transform as RectTransform);
    }

    /// <summary>Single-button info / error notice.</summary>
    public void ShowMessage(string title, string message, string okText = "OK")
    {
        titleText.text    = title;
        messageText.text  = message;
        confirmLabel.text = okText;
        cancelButton.gameObject.SetActive(false);
        if (dontShowAgainRow != null) dontShowAgainRow.SetActive(false);
        _onConfirm        = null;
        panel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.transform as RectTransform);
    }

    public void Close()
    {
        panel.SetActive(false);
        _onConfirm = null;
    }

    public void Confirm()
    {
        var cb = _onConfirm;
        Close();
        cb?.Invoke();
    }
}
