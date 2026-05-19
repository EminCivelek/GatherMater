using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AlchemyUI : MonoBehaviour
{
    public static AlchemyUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button     closeButton;
    [SerializeField] private GameObject inventoryHUD;
    [SerializeField] private GameObject interactionButton;

    [Header("Recipes")]
    [SerializeField] private PotionRecipe[]     recipes;
    [SerializeField] private Transform          recipeListParent;
    [SerializeField] private PotionRecipeSlotUI recipeSlotPrefab;

    [Header("Selection Info")]
    [SerializeField] private GameObject   selectionPanel;
    [SerializeField] private Image        selectedIcon;
    [SerializeField] private TMP_Text     selectedNameText;
    [SerializeField] private TMP_Text     selectedEffectText;
    [SerializeField] private TMP_Text     selectedCostText;
    [SerializeField] private TMP_Text     herbCountText;
    [SerializeField] private Button       craftButton;

    [Header("Crafting Footer")]
    [SerializeField] private GameObject craftingFooter;
    [SerializeField] private Image      craftingFooterIcon;
    [SerializeField] private TMP_Text   craftingFooterNameText;
    [SerializeField] private Slider     craftingProgressBar;
    [SerializeField] private TMP_Text   craftingTimeText;

    public PotionRecipe[] Recipes => recipes;

    private const string SaveKeyRecipe = "AlchemyCraft_RecipeId";
    private const string SaveKeyTicks  = "AlchemyCraft_StartTicks";

    private AlchemyBuilding                   _building;
    private PotionRecipe                      _selectedRecipe;
    private readonly List<PotionRecipeSlotUI> _slots = new();
    private IsometricPlayerController         _player;
    private Coroutine                         _craftCoroutine;
    private bool                              _isCrafting;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
        if (craftingFooter != null) craftingFooter.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Close);
        craftButton.onClick.AddListener(OnCraft);
        _player = FindAnyObjectByType<IsometricPlayerController>();

        foreach (var recipe in recipes)
        {
            var slot = Instantiate(recipeSlotPrefab, recipeListParent);
            PotionRecipe captured = recipe;
            slot.Init(recipe, () => SelectRecipe(captured));
            _slots.Add(slot);
        }

        ResumeSavedCraft();
    }

    private void Update()
    {
        if (_building != null && _player != null && _player.IsMoving) Close();
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public void Open(AlchemyBuilding building)
    {
        EquipmentUI.Instance?.Close();
        LeaderboardUI.Instance?.Close();
        DailyMissionBoardUI.Instance?.Close();

        _building       = building;
        _selectedRecipe = null;

        panel.SetActive(true);
        if (inventoryHUD      != null) inventoryHUD.SetActive(false);
        if (interactionButton != null) interactionButton.SetActive(false);
        if (selectionPanel    != null) selectionPanel.SetActive(false);
        if (craftingFooter    != null) craftingFooter.SetActive(_isCrafting);

        RefreshAll();
    }

    public void Close()
    {
        _building = null;
        panel.SetActive(false);
        if (inventoryHUD      != null) inventoryHUD.SetActive(true);
        if (interactionButton != null) interactionButton.SetActive(true);
    }

    // ── Selection ─────────────────────────────────────────────────────────────────
    private void SelectRecipe(PotionRecipe recipe)
    {
        _selectedRecipe = recipe;

        if (selectionPanel != null) selectionPanel.SetActive(true);

        var data = recipe.output;
        if (selectedIcon       != null) selectedIcon.sprite      = data?.icon;
        if (selectedNameText   != null) selectedNameText.text    = data?.itemName ?? "";
        if (selectedEffectText != null) selectedEffectText.text  = BuildEffectText(data);
        string costStr = $"{recipe.herbCost} Herbs";
        if (recipe.goldCost > 0) costStr += $"  +  {recipe.goldCost} Gold";
        if (selectedCostText != null) selectedCostText.text = $"Cost: {costStr}  •  {FormatTime(recipe.craftingTime)}";

        foreach (var s in _slots)
            s.SetHighlight(s.Recipe == recipe);

        RefreshCraftButton();
    }

    // ── Craft ─────────────────────────────────────────────────────────────────────
    private void OnCraft()
    {
        if (_selectedRecipe?.output == null || _isCrafting) return;
        var inv = Inventory.Instance;
        if (inv == null) return;

        if (!inv.Spend(ResourceType.Herbs, _selectedRecipe.herbCost)) return;
        if (_selectedRecipe.goldCost > 0 && !inv.Spend(ResourceType.Gold, _selectedRecipe.goldCost))
        {
            // Refund herbs if gold spend fails
            inv.Add(ResourceType.Herbs, _selectedRecipe.herbCost);
            return;
        }

        long startTicks = DateTime.UtcNow.Ticks;
        SaveCraftJob(_selectedRecipe, startTicks);
        _craftCoroutine = StartCoroutine(CraftCoroutine(_selectedRecipe, startTicks));
        RefreshAll();
    }

    private void ResumeSavedCraft()
    {
        string recipeId = PlayerPrefs.GetString(SaveKeyRecipe, "");
        string ticksStr = PlayerPrefs.GetString(SaveKeyTicks,  "0");
        if (string.IsNullOrEmpty(recipeId) || !long.TryParse(ticksStr, out long ticks) || ticks == 0) return;

        PotionRecipe recipe = FindRecipeById(recipeId);
        if (recipe == null) { ClearCraftJob(); return; }

        float elapsed = (float)(DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
        if (elapsed >= recipe.craftingTime)
        {
            // Finished while away — award immediately
            AwardPotion(recipe);
            ClearCraftJob();
            RefreshAll();
        }
        else
        {
            _craftCoroutine = StartCoroutine(CraftCoroutine(recipe, ticks));
        }
    }

    private IEnumerator CraftCoroutine(PotionRecipe recipe, long startTicks)
    {
        _isCrafting = true;

        if (craftingFooter         != null) craftingFooter.SetActive(true);
        if (craftingFooterIcon     != null) craftingFooterIcon.sprite         = recipe.output?.icon;
        if (craftingFooterNameText != null) craftingFooterNameText.text       = recipe.output?.itemName ?? "";
        if (craftingProgressBar    != null) { craftingProgressBar.minValue = 0f; craftingProgressBar.maxValue = 1f; }

        var startTime = new DateTime(startTicks, DateTimeKind.Utc);
        float duration = Mathf.Max(recipe.craftingTime, 0.1f);

        while (true)
        {
            float elapsed   = (float)(DateTime.UtcNow - startTime).TotalSeconds;
            float remaining = Mathf.Max(0f, duration - elapsed);
            float t         = Mathf.Clamp01(elapsed / duration);

            if (craftingProgressBar != null) craftingProgressBar.value = t;
            if (craftingTimeText    != null) craftingTimeText.text     = FormatTime(remaining);

            if (elapsed >= duration) break;
            yield return null;
        }

        AwardPotion(recipe);
        ClearCraftJob();

        _isCrafting     = false;
        _craftCoroutine = null;

        if (craftingFooter != null) craftingFooter.SetActive(false);
        RefreshAll();
    }

    private void AwardPotion(PotionRecipe recipe)
    {
        for (int i = 0; i < recipe.yield; i++)
        {
            ItemInventory.Instance?.Add(new ItemInstance
            {
                itemDataId = recipe.output.itemName,
                level      = 1,
                data       = recipe.output
            });
        }
    }

    // ── Persistence ───────────────────────────────────────────────────────────────
    private void SaveCraftJob(PotionRecipe recipe, long startTicks)
    {
        PlayerPrefs.SetString(SaveKeyRecipe, recipe.output?.itemName ?? "");
        PlayerPrefs.SetString(SaveKeyTicks,  startTicks.ToString());
        PlayerPrefs.Save();
    }

    private void ClearCraftJob()
    {
        PlayerPrefs.DeleteKey(SaveKeyRecipe);
        PlayerPrefs.DeleteKey(SaveKeyTicks);
        PlayerPrefs.Save();
    }

    private PotionRecipe FindRecipeById(string itemName)
    {
        foreach (var r in recipes)
            if (r != null && r.output != null && r.output.itemName == itemName) return r;
        return null;
    }

    // ── Refresh ───────────────────────────────────────────────────────────────────
    private void RefreshAll()
    {
        int herbs = Inventory.Instance?.Get(ResourceType.Herbs) ?? 0;
        if (herbCountText != null) herbCountText.text = $"Herbs: {herbs}";

        foreach (var s in _slots) s.Refresh();
        RefreshCraftButton();
    }

    private void RefreshCraftButton()
    {
        if (craftButton == null || _selectedRecipe == null) return;
        var inv  = Inventory.Instance;
        bool canAfford = inv != null
            && inv.Has(ResourceType.Herbs, _selectedRecipe.herbCost)
            && (_selectedRecipe.goldCost <= 0 || inv.Has(ResourceType.Gold, _selectedRecipe.goldCost));
        craftButton.interactable = canAfford && !_isCrafting;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────
    private static string BuildEffectText(ItemData data)
    {
        if (data == null) return "";
        return data.potionEffect switch
        {
            PotionEffect.HealHP          => $"+{data.potionEffectAmount} HP (instant)",
            PotionEffect.HealMP          => $"+{data.potionEffectAmount} MP (instant)",
            PotionEffect.BoostHPRegen    => $"+{data.potionEffectAmount} HP/s for {data.potionDuration}s",
            PotionEffect.BoostMPRegen    => $"+{data.potionEffectAmount} MP/s for {data.potionDuration}s",
            PotionEffect.BoostArmor      => $"+{data.potionEffectAmount} Armor for {data.potionDuration}s",
            PotionEffect.BoostAttack     => $"+{data.potionEffectAmount} Attack for {data.potionDuration}s",
            PotionEffect.BoostAttackSpeed=> $"+{data.potionEffectAmount} Atk Speed for {data.potionDuration}s",
            _                            => ""
        };
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
