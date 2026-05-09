#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// Adds a 1444x812 "GatherMater" Game View size and selects it before
/// entering Play Mode, preventing URP's odd-resolution post-process warning.
[InitializeOnLoad]
static class EvenGameViewResolution
{
    const int    W    = 1444;
    const int    H    = 812;
    const string Name = "GatherMater";

    static EvenGameViewResolution()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                Apply();
        };
    }

    static void Apply()
    {
        var asm           = typeof(EditorWindow).Assembly;
        var sizesType     = asm.GetType("UnityEditor.GameViewSizes");
        var sizeType      = asm.GetType("UnityEditor.GameViewSize");
        var groupType     = asm.GetType("UnityEditor.GameViewSizeGroup");
        var sizeEnumType  = asm.GetType("UnityEditor.GameViewSizeType");
        var gameViewType  = asm.GetType("UnityEditor.GameView");

        if (sizesType == null || sizeType == null || groupType == null) return;

        // Singleton instance
        var instanceProp = sizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
        if (instanceProp == null) return;
        var sizes = instanceProp.GetValue(null);

        // Current group (Standalone / WebGL / etc.)
        var groupTypeProp = sizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public);
        if (groupTypeProp == null) return;
        int groupTypeVal = (int)groupTypeProp.GetValue(sizes);

        var getGroup = sizesType.GetMethod("GetGroup");
        if (getGroup == null) return;
        var group = getGroup.Invoke(sizes, new object[] { groupTypeVal });

        var getTotal    = groupType.GetMethod("GetTotalCount");
        var getSize     = groupType.GetMethod("GetGameViewSize");
        var widthProp   = sizeType.GetProperty("width");
        var heightProp  = sizeType.GetProperty("height");
        if (getTotal == null || getSize == null || widthProp == null || heightProp == null) return;

        int total      = (int)getTotal.Invoke(group, null);
        int foundIndex = -1;

        for (int i = 0; i < total; i++)
        {
            var s = getSize.Invoke(group, new object[] { i });
            if (s == null) continue;
            if ((int)widthProp.GetValue(s) == W && (int)heightProp.GetValue(s) == H)
            { foundIndex = i; break; }
        }

        if (foundIndex < 0)
        {
            var ctor = sizeType.GetConstructor(new[] { sizeEnumType, typeof(int), typeof(int), typeof(string) });
            if (ctor == null) return;
            var fixedRes = Enum.ToObject(sizeEnumType, 1); // 1 = FixedResolution
            var newSize  = ctor.Invoke(new object[] { fixedRes, W, H, Name });
            var addCustom = groupType.GetMethod("AddCustomSize");
            if (addCustom == null) return;
            addCustom.Invoke(group, new object[] { newSize });
            foundIndex = total;
        }

        // Select it in every open Game View window
        if (gameViewType == null) return;
        var selectedIndexProp = gameViewType.GetProperty("selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (selectedIndexProp == null) return;

        foreach (var gv in Resources.FindObjectsOfTypeAll(gameViewType))
            selectedIndexProp.SetValue(gv, foundIndex);
    }
}
#endif
