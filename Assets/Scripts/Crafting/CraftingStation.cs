using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to any crafting building (Blacksmith, Tailor, etc.).
/// Implements IInteractable so the player can walk up and open the crafting UI.
/// Active jobs persist across sessions via PlayerPrefs UTC timestamps.
/// </summary>
public class CraftingStation : MonoBehaviour, IInteractable
{
    [Header("Station Info")]
    [SerializeField] private string stationName = "Blacksmith";
    [SerializeField] private Sprite stationIcon;

    [Header("Recipes")]
    [SerializeField] private CraftingRecipe[] recipes;

    [Header("Settings")]
    [SerializeField] private int maxConcurrentJobs = 1;

    // ── IInteractable ───────────────────────────────────────────────────────────
    public string InteractionLabel => stationName;
    public Sprite InteractionIcon  => stationIcon;
    public float  GatherDuration   => 0f;   // instant — opens UI immediately
    public bool   IsAvailable      => true;

    // ── Public state ────────────────────────────────────────────────────────────
    public CraftingRecipe[]          Recipes    => recipes;
    public IReadOnlyList<CraftingJob> ActiveJobs => _activeJobs;

    /// <summary>Fired whenever a job is added or completed.</summary>
    public event Action OnJobsChanged;

    // ── Private ─────────────────────────────────────────────────────────────────
    private readonly List<CraftingJob> _activeJobs = new();
    private string SaveKey => $"CraftStation_{gameObject.name}";

    // ── Unity lifecycle ─────────────────────────────────────────────────────────
    private void Awake()  => LoadJobs();
    private void Start()  => ProcessCompletedJobs(); // catch any jobs that finished while offline

    private void Update()
    {
        bool anyDone = false;
        for (int i = _activeJobs.Count - 1; i >= 0; i--)
        {
            var recipe = FindRecipe(_activeJobs[i].recipeId);
            if (recipe != null && _activeJobs[i].IsComplete(recipe.craftDuration))
            {
                AwardOutput(recipe);
                _activeJobs.RemoveAt(i);
                anyDone = true;
            }
        }
        if (anyDone)
        {
            SaveJobs();
            OnJobsChanged?.Invoke();
        }
    }

    // ── IInteractable ────────────────────────────────────────────────────────────
    public void OnGatherComplete() => CraftingUI.Instance?.Open(this);

    // ── Public API ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Starts crafting a recipe. Deducts resources immediately.
    /// Returns false if the player can't afford it or slots are full.
    /// </summary>
    public bool StartCraft(CraftingRecipe recipe)
    {
        if (_activeJobs.Count >= maxConcurrentJobs) return false;
        if (!recipe.CanAfford()) return false;

        recipe.DeductCost();
        _activeJobs.Add(new CraftingJob
        {
            recipeId       = recipe.name,
            startTimeTicks = DateTime.UtcNow.Ticks
        });

        SaveJobs();
        OnJobsChanged?.Invoke();
        return true;
    }

    public CraftingRecipe FindRecipe(string id) =>
        Array.Find(recipes, r => r.name == id);

    /// <summary>Speeds up the first active job by the given number of minutes.</summary>
    public void ApplySpeedUp(int minutes)
    {
        if (_activeJobs.Count == 0) return;
        _activeJobs[0].SpeedUp(minutes);
        SaveJobs();
        OnJobsChanged?.Invoke();
    }

    // ── Private helpers ──────────────────────────────────────────────────────────
    private void ProcessCompletedJobs()
    {
        bool anyDone = false;
        for (int i = _activeJobs.Count - 1; i >= 0; i--)
        {
            var recipe = FindRecipe(_activeJobs[i].recipeId);
            if (recipe != null && _activeJobs[i].IsComplete(recipe.craftDuration))
            {
                AwardOutput(recipe);
                _activeJobs.RemoveAt(i);
                anyDone = true;
            }
        }
        if (anyDone) SaveJobs();
        OnJobsChanged?.Invoke();
    }

    private void AwardOutput(CraftingRecipe recipe)
    {
        if (recipe.outputItem == null) return;
        var item = new ItemInstance
        {
            itemDataId = recipe.outputItem.name,
            level      = recipe.baseLevel,
            data       = recipe.outputItem
        };
        ItemInventory.Instance?.Add(item);
        DailyMissionManager.Instance?.ReportCraft();
        Debug.Log($"[Crafting] Completed: {recipe.outputItem.itemName} Lv{recipe.baseLevel}");
    }

    // ── Persistence (local) ──────────────────────────────────────────────────────
    private void SaveJobs()
    {
        // TODO: replace PlayerPrefs with server-side timestamps for multiplayer
        var wrapper = new JobListWrapper { jobs = new List<CraftingJob>(_activeJobs) };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void LoadJobs()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;
        var wrapper = JsonUtility.FromJson<JobListWrapper>(json);
        if (wrapper?.jobs != null)
        {
            _activeJobs.Clear();
            _activeJobs.AddRange(wrapper.jobs);
        }
    }

    [Serializable]
    private class JobListWrapper { public List<CraftingJob> jobs; }
}
