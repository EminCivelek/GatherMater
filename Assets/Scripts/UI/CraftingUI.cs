using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space panel opened when the player interacts with a CraftingStation.
/// Shows the recipe list and currently active jobs.
/// </summary>
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

    [Header("Active Jobs")]
    [SerializeField] private Transform       activeJobsParent;
    [SerializeField] private ActiveJobSlotUI activeJobSlotPrefab;

    private CraftingStation                _station;
    private readonly List<RecipeSlotUI>    _recipeSlots = new();
    private readonly List<ActiveJobSlotUI> _jobSlots    = new();
    private IsometricPlayerController      _player;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
        _player = FindAnyObjectByType<IsometricPlayerController>();
    }

    private void Update()
    {
        if (_station != null && _player != null && _player.IsMoving)
            Close();
    }

    // ── Public API ───────────────────────────────────────────────────────────────
    public void Open(CraftingStation station)
    {
        if (_station != null) _station.OnJobsChanged -= RefreshActiveJobs;

        _station = station;
        _station.OnJobsChanged += RefreshActiveJobs;

        stationNameText.text = station.InteractionLabel;
        panel.SetActive(true);
        if (inventoryHUD != null)        inventoryHUD.SetActive(false);
        if (interactionButton != null)   interactionButton.SetActive(false);

        BuildRecipeList();
        RefreshActiveJobs();
    }

    public void Close()
    {
        if (_station != null) _station.OnJobsChanged -= RefreshActiveJobs;
        _station = null;
        panel.SetActive(false);
        if (inventoryHUD != null)        inventoryHUD.SetActive(true);
        if (interactionButton != null)   interactionButton.SetActive(true);
    }

    // ── Private ──────────────────────────────────────────────────────────────────
    private void BuildRecipeList()
    {
        foreach (var s in _recipeSlots) Destroy(s.gameObject);
        _recipeSlots.Clear();

        foreach (var recipe in _station.Recipes)
        {
            var slot = Instantiate(recipeSlotPrefab, recipeListParent);
            slot.Init(recipe, TryCraft);
            _recipeSlots.Add(slot);
        }
    }

    private void RefreshActiveJobs()
    {
        foreach (var s in _jobSlots) Destroy(s.gameObject);
        _jobSlots.Clear();

        foreach (var job in _station.ActiveJobs)
        {
            var recipe = _station.FindRecipe(job.recipeId);
            if (recipe == null) continue;
            var slot = Instantiate(activeJobSlotPrefab, activeJobsParent);
            slot.Init(job, recipe);
            _jobSlots.Add(slot);
        }

        // Affordability may have changed after spending resources
        foreach (var s in _recipeSlots)
            s.RefreshAffordability();
    }

    private void TryCraft(CraftingRecipe recipe) => _station.StartCraft(recipe);
}
