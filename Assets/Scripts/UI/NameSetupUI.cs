using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NameSetupUI : MonoBehaviour
{
    public static NameSetupUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] GameObject panel;

    [Header("Input")]
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Button         confirmButton;

    [Header("Feedback")]
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text statusText;

    const string PREF_NAME_SET = "PlayerNameSet";
    const int    MAX_LENGTH    = 20;
    const int    MIN_LENGTH    = 3;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    void Start()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);

        Debug.Log($"[NameSetup] Start — UGSManager={(UGSManager.Instance == null ? "NULL" : "OK")} IsSignedIn={UGSManager.Instance?.IsSignedIn}");

        if (UGSManager.Instance == null) return;

        if (UGSManager.Instance.IsSignedIn)
        {
            Debug.Log("[NameSetup] Already signed in at Start");
            CheckAndShow();
        }
        else
        {
            Debug.Log("[NameSetup] Subscribing to OnSignedIn");
            UGSManager.Instance.OnSignedIn += OnUGSReady;
        }
    }

    void OnDestroy()
    {
        if (UGSManager.Instance != null)
            UGSManager.Instance.OnSignedIn -= OnUGSReady;
    }

    void OnUGSReady()
    {
        UGSManager.Instance.OnSignedIn -= OnUGSReady;
        CheckAndShow();
    }

    void CheckAndShow()
    {
        int prefFlag = PlayerPrefs.GetInt(PREF_NAME_SET, 0);
        string ugsName = AuthenticationService.Instance.PlayerName;
        Debug.Log($"[NameSetup] CheckAndShow — prefFlag={prefFlag} ugsName='{ugsName}'");

        if (prefFlag == 1) { Debug.Log("[NameSetup] Skipped: PlayerPrefs flag set"); return; }

        if (!string.IsNullOrEmpty(ugsName))
        {
            Debug.Log("[NameSetup] Skipped: UGS already has a name");
            PlayerPrefs.SetInt(PREF_NAME_SET, 1);
            PlayerPrefs.Save();
            return;
        }

        Debug.Log("[NameSetup] Showing panel");
        panel.SetActive(true);
    }

    void OnConfirmClicked()
    {
        errorText.text = "";
        string name = nameInput.text.Trim();

        if (name.Length < MIN_LENGTH)
        {
            errorText.text = $"Name must be at least {MIN_LENGTH} characters.";
            return;
        }
        if (name.Length > MAX_LENGTH)
        {
            errorText.text = $"Max {MAX_LENGTH} characters.";
            return;
        }

        _ = SetNameAsync(name);
    }

    async Task SetNameAsync(string name)
    {
        confirmButton.interactable = false;
        statusText.text = "Saving...";

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            PlayerPrefs.SetInt(PREF_NAME_SET, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
            Debug.Log($"[NameSetup] Player name set: {name}");
        }
        catch (Exception e)
        {
            errorText.text          = "Could not save name. Try again.";
            confirmButton.interactable = true;
            statusText.text         = "";
            Debug.LogWarning($"[NameSetup] {e.Message}");
        }
    }
}
