using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   stationNameText;
    [SerializeField] private Button     closeButton;
    [SerializeField] private GameObject inventoryHUD;
    [SerializeField] private GameObject interactionButton;

    [Header("Recipes")]
    [SerializeField] private Transform    recipeListParent;
    [SerializeField] private RecipeSlotUI recipeSlotPrefab;

    [Header("Selection Panel")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Image      selectedIcon;
    [SerializeField] private TMP_Text   selectedNameText;
    [SerializeField] private TMP_Text   selectedRequirementsText;
    [SerializeField] private TMP_Text   selectedStatsText;
    [SerializeField] private TMP_Text   selectedDurationText;
    [SerializeField] private Button     craftButton;

    [Header("Active Job Footer")]
    [SerializeField] private GameObject jobFooter;
    [SerializeField] private Image      jobIcon;
    [SerializeField] private TMP_Text   jobNameText;
    [SerializeField] private Slider     jobProgressBar;
    [SerializeField] private TMP_Text   jobTimeText;
    [SerializeField] private Button     jobSpeedUpButton;

    private CraftingStation             _station;
    private CraftingRecipe              _selectedRecipe;
    private readonly List<RecipeSlotUI> _recipeSlots = new();
    private IsometricPlayerController   _player;
    private GameObject                  _overlay;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
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

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
        if (craftButton      != null) craftButton.onClick.AddListener(OnCraft);
        if (jobSpeedUpButton != null) jobSpeedUpButton.onClick.AddListener(OnSpeedUpClicked);
        _player = FindAnyObjectByType<IsometricPlayerController>();
    }

    private void Update()
    {
        if (_station != null && _player != null && _player.IsMoving) Close();
        UpdateJobFooter();
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open(CraftingStation station)
    {
        EquipmentUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        DailyMissionBoardUI.Instance?.Close();

        if (_station != null) _station.OnJobsChanged -= OnJobsChanged;

        _station        = station;
        _selectedRecipe = null;
        _station.OnJobsChanged += OnJobsChanged;

        stationNameText.text = station.InteractionLabel;
        EnsureOverlay();
        _overlay.SetActive(true);
        _overlay.transform.SetAsLastSibling();
        transform.SetAsLastSibling();
        panel.SetActive(true);
        if (inventoryHUD      != null) inventoryHUD.SetActive(false);
        if (interactionButton != null) interactionButton.SetActive(false);
        if (selectionPanel    != null) selectionPanel.SetActive(false);
        if (jobFooter         != null) jobFooter.SetActive(station.ActiveJobs.Count > 0);

        BuildRecipeList();
    }

    public void Close()
    {
        if (_overlay != null) _overlay.SetActive(false);
        if (_station != null) _station.OnJobsChanged -= OnJobsChanged;
        _station = null;
        panel.SetActive(false);
        if (inventoryHUD      != null) inventoryHUD.SetActive(true);
        if (interactionButton != null) interactionButton.SetActive(true);
    }

    private void EnsureOverlay()
    {
        if (_overlay != null) return;
        _overlay = new GameObject("CloseOverlay");
        _overlay.transform.SetParent(transform.parent, false);
        var rt = _overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var img = _overlay.AddComponent<Image>(); img.color = Color.clear;
        var btn = _overlay.AddComponent<Button>(); btn.targetGraphic = img;
        btn.onClick.AddListener(Close);
        _overlay.SetActive(false);
    }

    // ── Private ───────────────────────────────────────────────────────────────────
    private void BuildRecipeList()
    {
        foreach (var s in _recipeSlots) Destroy(s.gameObject);
        _recipeSlots.Clear();

        foreach (var recipe in _station.Recipes)
        {
            var slot = Instantiate(recipeSlotPrefab, recipeListParent);
            slot.Init(recipe, SelectRecipe);
            _recipeSlots.Add(slot);
        }
    }

    private void SelectRecipe(CraftingRecipe recipe)
    {
        _selectedRecipe = recipe;

        if (selectionPanel           != null) selectionPanel.SetActive(true);
        if (selectedIcon             != null) selectedIcon.sprite             = recipe.icon;
        if (selectedNameText         != null) selectedNameText.text           = recipe.displayName;
        if (selectedRequirementsText != null) selectedRequirementsText.text   = recipe.GetRequirementsText();
        if (selectedStatsText        != null) selectedStatsText.text          = BuildStatsText(recipe);
        if (selectedDurationText     != null) selectedDurationText.text       = $"Time:  {FormatTime(recipe.craftDuration)}";

        foreach (var s in _recipeSlots)
            s.SetHighlight(s.Recipe == recipe);

        RefreshCraftButton();
    }

    private void OnCraft()
    {
        if (_selectedRecipe == null || _station == null) return;
        if (_station.StartCraft(_selectedRecipe))
            RefreshCraftButton();
    }

    private void OnJobsChanged()
    {
        foreach (var s in _recipeSlots) s.RefreshAffordability();
        RefreshCraftButton();
    }

    private void RefreshCraftButton()
    {
        if (craftButton == null || _selectedRecipe == null) return;
        bool slotFree = _station != null && _station.ActiveJobs.Count < 1;
        craftButton.interactable = _selectedRecipe.CanAfford() && slotFree;
    }

    private void UpdateJobFooter()
    {
        if (_station == null || _station.ActiveJobs.Count == 0)
        {
            if (jobFooter != null && jobFooter.activeSelf) jobFooter.SetActive(false);
            return;
        }

        var job    = _station.ActiveJobs[0];
        var recipe = _station.FindRecipe(job.recipeId);
        if (recipe == null) return;

        if (jobFooter != null && !jobFooter.activeSelf) jobFooter.SetActive(true);
        if (jobIcon        != null) jobIcon.sprite       = recipe.icon;
        if (jobNameText    != null) jobNameText.text     = recipe.displayName;
        if (jobProgressBar != null) jobProgressBar.value = job.GetProgress(recipe.craftDuration);
        if (jobTimeText    != null) jobTimeText.text     = FormatTime(job.GetRemainingSeconds(recipe.craftDuration));
    }

    private static string BuildStatsText(CraftingRecipe recipe)
    {
        var data = recipe.outputItem;
        if (data == null) return "";

        var item = new ItemInstance { data = data, level = recipe.baseLevel };
        var sb   = new System.Text.StringBuilder();

        if (item.IsWeapon || item.IsShield)
        {
            sb.Append($"ATK  {item.GetAttack():F1}");
            if (item.IsWeapon) sb.Append($"   SPD  {item.GetAttackSpeed():F2}");
        }

        if (data.category == ItemCategory.Armor || item.IsShield)
        {
            if (sb.Length > 0) sb.Append("   ");
            sb.Append($"ARM  {item.GetArmor():F1}");
        }

        if (data.onHitBonusDamage > 0f)
        {
            if (sb.Length > 0) sb.Append("   ");
            sb.Append($"On-Hit  +{data.onHitBonusDamage:F1}");
        }
        if (data.onHitHPRecovery > 0f)
        {
            if (sb.Length > 0) sb.Append("   ");
            sb.Append($"Lifesteal  +{data.onHitHPRecovery:F1}");
        }

        return sb.ToString();
    }

    private void OnSpeedUpClicked()
    {
        SpeedUpPanelUI.Instance?.Open(item =>
        {
            int minutes = item.data?.speedUpMinutes > 0 ? item.data.speedUpMinutes : 1;
            _station?.ApplySpeedUp(minutes);
        });
    }

    private static string FormatTime(float seconds)
    {
        if (seconds >= 60f)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.CeilToInt(seconds % 60f);
            return s > 0 ? $"{m}m {s}s" : $"{m}m";
        }
        return $"{Mathf.CeilToInt(seconds)}s";
    }
}
