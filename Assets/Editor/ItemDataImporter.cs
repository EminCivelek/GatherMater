using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ItemDataImporter
{
    const string ItemsFolder   = "Assets/Resources/Items";
    const string RecipesFolder = "Assets/Resources/Recipes";
    const string DbPath        = "Assets/Resources/ItemDatabase.asset";

    [MenuItem("GatherMater/Import Items from CSV")]
    public static void ImportItems()
    {
        string csvPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../items.csv"));

        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("Error", $"items.csv not found at:\n{csvPath}", "OK");
            return;
        }

        EnsureFolder("Assets/Resources", "Items");
        EnsureFolder("Assets/Resources", "Recipes");

        string[] lines = File.ReadAllLines(csvPath);
        int itemsCreated = 0, itemsUpdated = 0;
        int recipesCreated = 0, recipesUpdated = 0;
        var allItems = new List<ItemData>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = line.Split(',');
            if (cols.Length < 20)
            {
                Debug.LogWarning($"[ItemDataImporter] Skipping malformed row {i + 1}: {line}");
                continue;
            }

            string itemName = cols[0].Trim();

            // ── ItemData ─────────────────────────────────────────────────────────
            string itemPath = $"{ItemsFolder}/{itemName}.asset";
            ItemData item   = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
            bool itemIsNew  = item == null;
            if (itemIsNew) item = ScriptableObject.CreateInstance<ItemData>();

            item.itemName = itemName;

            if (Enum.TryParse(cols[1].Trim(), out ItemCategory cat))  item.category    = cat;
            if (Enum.TryParse(cols[2].Trim(), out EquipSlot slot))     item.primarySlot = slot;

            string wtStr = cols[3].Trim();
            if (!string.IsNullOrEmpty(wtStr) && Enum.TryParse(wtStr, out WeaponType wt))
                item.weaponType = wt;

            item.isTwoHanded     = cols[4].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            item.canEquipOffHand = cols[5].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

            item.baseAttack          = ParseFloat(cols[6]);
            item.attackPerLevel      = ParseFloat(cols[7]);
            item.baseAttackSpeed     = ParseFloat(cols[8]);
            item.attackSpeedPerLevel = ParseFloat(cols[9]);
            item.onHitBonusDamage    = ParseFloat(cols[10]);
            item.onHitHPRecovery     = ParseFloat(cols[11]);
            item.onHitMPRecovery     = ParseFloat(cols[12]);
            item.baseArmor           = ParseFloat(cols[13]);
            item.armorPerLevel       = ParseFloat(cols[14]);

            if (itemIsNew) { AssetDatabase.CreateAsset(item, itemPath); itemsCreated++; }
            else           { EditorUtility.SetDirty(item);              itemsUpdated++; }

            allItems.Add(item);

            // ── CraftingRecipe ───────────────────────────────────────────────────
            float craftDuration  = ParseFloat(cols[15]);
            string req1Resource  = cols[16].Trim();
            int    req1Amount    = ParseInt(cols[17]);
            string req2Resource  = cols[18].Trim();
            int    req2Amount    = ParseInt(cols[19]);

            var requirements = new List<CraftingRecipe.ResourceRequirement>();
            if (!string.IsNullOrEmpty(req1Resource) && req1Amount > 0 &&
                Enum.TryParse(req1Resource, out ResourceType rt1))
                requirements.Add(new CraftingRecipe.ResourceRequirement { resource = rt1, amount = req1Amount });

            if (!string.IsNullOrEmpty(req2Resource) && req2Amount > 0 &&
                Enum.TryParse(req2Resource, out ResourceType rt2))
                requirements.Add(new CraftingRecipe.ResourceRequirement { resource = rt2, amount = req2Amount });

            string recipePath       = $"{RecipesFolder}/{itemName}.asset";
            CraftingRecipe recipe   = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(recipePath);
            bool recipeIsNew        = recipe == null;
            if (recipeIsNew) recipe = ScriptableObject.CreateInstance<CraftingRecipe>();

            recipe.displayName    = itemName;
            recipe.outputItem     = item;
            recipe.baseLevel      = 1;
            recipe.craftDuration  = craftDuration;
            recipe.requirements   = requirements.ToArray();

            if (recipeIsNew) { AssetDatabase.CreateAsset(recipe, recipePath); recipesCreated++; }
            else             { EditorUtility.SetDirty(recipe);                recipesUpdated++; }
        }

        AssetDatabase.SaveAssets();

        // ── ItemDatabase ─────────────────────────────────────────────────────────
        ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, DbPath);
            Debug.Log("[ItemDataImporter] Created new ItemDatabase asset.");
        }

        var so   = new SerializedObject(db);
        var prop = so.FindProperty("items");
        prop.arraySize = allItems.Count;
        for (int i = 0; i < allItems.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = allItems[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Import Complete",
            $"Done!\n\n" +
            $"Items    — Created: {itemsCreated}  Updated: {itemsUpdated}\n" +
            $"Recipes  — Created: {recipesCreated}  Updated: {recipesUpdated}\n\n" +
            $"Total in database: {allItems.Count}",
            "OK");
    }

    static void EnsureFolder(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    static float ParseFloat(string s) =>
        float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

    static int ParseInt(string s) =>
        int.TryParse(s.Trim(), out int v) ? v : 0;
}
