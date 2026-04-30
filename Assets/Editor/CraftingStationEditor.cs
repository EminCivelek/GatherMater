using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftingStation))]
public class CraftingStationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-populate Recipes from Assets/Resources/Recipes/"))
            AutoPopulateRecipes((CraftingStation)target);
    }

    static void AutoPopulateRecipes(CraftingStation station)
    {
        string[] guids = AssetDatabase.FindAssets("t:CraftingRecipe", new[] { "Assets/Resources/Recipes" });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Recipes Found",
                "No CraftingRecipe assets found in Assets/Resources/Recipes/.\nRun GatherMater → Import Items from CSV first.",
                "OK");
            return;
        }

        var so       = new SerializedObject(station);
        var recipesProp = so.FindProperty("recipes");

        recipesProp.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
        {
            string path   = AssetDatabase.GUIDToAssetPath(guids[i]);
            var    recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
            recipesProp.GetArrayElementAtIndex(i).objectReferenceValue = recipe;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(station);

        Debug.Log($"[CraftingStation] Auto-populated {guids.Length} recipes on '{station.gameObject.name}'.");
    }
}
