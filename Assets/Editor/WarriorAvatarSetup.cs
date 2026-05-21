#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GatherMater → Setup Warrior Avatar
/// 1. Fixes texture import settings for all warrior PNGs.
/// 2. Creates animation clips (Idle, Walk, Attack, Hurt, Died) per direction.
/// 3. Builds AnimatorController matching elf_fighter's parameters.
/// 4. Creates WarriorAvatarConfig asset with auto-assigned portrait sprites.
/// 5. Creates WarriorPortrait prefab.
/// 6. Swaps Player's AnimatorController to the new warrior controller.
/// </summary>
public static class WarriorAvatarSetup
{
    const string ROOT         = "Assets/Warrior Avatar";
    const string ANIM_ROOT    = ROOT + "/Warrior_animations";
    const string OUT_ANIMS    = "Assets/Animations/Warrior";
    const string OUT_CONFIG   = "Assets/Resources/WarriorAvatarConfig.asset";
    const string OUT_PREFAB   = "Assets/Prefabs/WarriorPortrait.prefab";
    const string OUT_CTRL     = OUT_ANIMS + "/WarriorController.controller";
    const float  FPS          = 12f;

    // Direction vectors for 2D blend trees (match elf_fighter convention)
    static readonly (string dir, Vector2 blend)[] Directions = {
        ("Front",      new Vector2( 0, -1)),
        ("Back",       new Vector2( 0,  1)),
        ("Left_Side",  new Vector2(-1,  0)),
        ("Right_Side", new Vector2( 1,  0)),
    };

    // Animation names in the PNG-sequences folder (warrior naming)
    static readonly (string folder, string stateName, bool loop)[] Animations = {
        ("Idle",     "Idle",   true),
        ("Walk",     "Walk",   true),
        ("Attack_1", "Attack", false),
        ("Hurt",     "Hurt",   false),
        ("Died",     "Died",   false),
    };

    [MenuItem("GatherMater/Setup Warrior Avatar")]
    public static void Run()
    {
        EnsureFolder(OUT_ANIMS);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Resources");
        AssetDatabase.Refresh(); // register newly created folders before writing assets

        // ── 1. Fix sprite import settings ─────────────────────────────────
        FixImportSettings();

        // ── 2. Create animation clips ─────────────────────────────────────
        var clips = CreateAllClips();

        // ── 3. Build AnimatorController ───────────────────────────────────
        var controller = BuildController(clips);

        // ── 4. Create WarriorAvatarConfig ─────────────────────────────────
        var cfg = CreateConfig();

        // ── 5. Create WarriorPortrait prefab ──────────────────────────────
        CreatePortraitPrefab(cfg);

        // ── 6. Swap Player animator ───────────────────────────────────────
        SwapPlayerAnimator(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WarriorSetup] Done! Check console for any warnings.");
    }

    // ── 1. Import settings ────────────────────────────────────────────────
    static void FixImportSettings()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ROOT });
        var toReimport = new List<string>();

        // Pass 1 — update .meta files on disk without triggering any reimport
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureType      == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single    &&
                !importer.mipmapEnabled) continue;

            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled    = false;
            importer.filterMode       = FilterMode.Point;
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(path); // writes .meta, no reimport
            toReimport.Add(path);
        }

        // Pass 2 — single batched reimport of all changed textures
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in toReimport)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            AssetDatabase.StopAssetEditing(); // one reimport pass for everything
        }

        Debug.Log($"[WarriorSetup] Fixed import settings on {toReimport.Count} textures.");
    }

    // ── 2. Clips ──────────────────────────────────────────────────────────
    static Dictionary<string, AnimationClip> CreateAllClips()
    {
        var clips = new Dictionary<string, AnimationClip>();
        string clothesFolder = "Warrior_clothes_1";

        foreach (var (dir, _) in Directions)
        {
            foreach (var (folder, stateName, loop) in Animations)
            {
                string seqPath = $"{ANIM_ROOT}/{dir}/PNG Sequences/{clothesFolder}/{folder}";
                if (!Directory.Exists(Path.Combine(Application.dataPath, "..", seqPath)))
                {
                    Debug.LogWarning($"[WarriorSetup] Sequence not found: {seqPath}");
                    continue;
                }

                string clipKey  = $"{dir}_{stateName}";
                string clipPath = $"{OUT_ANIMS}/{clipKey}.anim";

                var clip = LoadOrCreate<AnimationClip>(clipPath);
                clip.frameRate = FPS;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                var sprites = LoadSequenceSprites(seqPath);
                if (sprites.Count == 0) { Debug.LogWarning($"[WarriorSetup] No sprites in {seqPath}"); continue; }

                var binding   = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                var keyframes = new ObjectReferenceKeyframe[sprites.Count];
                for (int i = 0; i < sprites.Count; i++)
                    keyframes[i] = new ObjectReferenceKeyframe { time = i / FPS, value = sprites[i] };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

                SaveOrUpdate(clip, clipPath);
                clips[clipKey] = clip;
            }
        }
        Debug.Log($"[WarriorSetup] Created {clips.Count} animation clips.");
        return clips;
    }

    static List<Sprite> LoadSequenceSprites(string folderAssetPath)
    {
        var list  = new List<Sprite>();
        string abs = Path.Combine(Application.dataPath, "..", folderAssetPath);
        if (!Directory.Exists(abs)) return list;
        var files = Directory.GetFiles(abs, "*.png");
        System.Array.Sort(files);
        foreach (var f in files)
        {
            string rel = "Assets" + f.Replace(Application.dataPath, "").Replace('\\', '/');
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(rel);
            if (sp != null) list.Add(sp);
        }
        return list;
    }

    // ── 3. AnimatorController ─────────────────────────────────────────────
    static AnimatorController BuildController(Dictionary<string, AnimationClip> clips)
    {
        var ctrl = LoadOrCreateController(OUT_CTRL);
        var sm   = ctrl.layers[0].stateMachine;

        // Clear existing states except Entry/Exit/Any
        foreach (var s in sm.states)
            if (s.state.name != "New State") sm.RemoveState(s.state);

        // Parameters (match elf_fighter)
        EnsureParam(ctrl, "x",      AnimatorControllerParameterType.Float);
        EnsureParam(ctrl, "y",      AnimatorControllerParameterType.Float);
        EnsureParam(ctrl, "Walk",   AnimatorControllerParameterType.Bool);
        EnsureParam(ctrl, "Slash",  AnimatorControllerParameterType.Trigger);
        EnsureParam(ctrl, "Damage", AnimatorControllerParameterType.Trigger);
        EnsureParam(ctrl, "Dead",   AnimatorControllerParameterType.Trigger);

        // States
        var stayState   = AddBlendState(ctrl, "Stay",   "Idle",   clips);
        var walkState   = AddBlendState(ctrl, "Walk",   "Walk",   clips);
        var slashState  = AddBlendState(ctrl, "Slash",  "Attack", clips);
        var damageState = AddBlendState(ctrl, "Damage", "Hurt",   clips);
        var deadState   = AddBlendState(ctrl, "Dead",   "Died",   clips);

        sm.defaultState = stayState;

        // Transitions: Stay ↔ Walk
        AddTriggerless(stayState,  walkState,  "Walk", true);
        AddTriggerless(walkState,  stayState,  "Walk", false);
        // Stay → Slash/Damage/Dead
        AddTrigger(stayState,  slashState,  "Slash");
        AddTrigger(walkState,  slashState,  "Slash");
        AddTrigger(stayState,  damageState, "Damage");
        AddTrigger(walkState,  damageState, "Damage");
        AddTrigger(stayState,  deadState,   "Dead");
        AddTrigger(walkState,  deadState,   "Dead");
        // Return from Slash/Damage → Stay
        AddExitTime(slashState,  stayState);
        AddExitTime(damageState, stayState);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log($"[WarriorSetup] AnimatorController saved: {OUT_CTRL}");
        return ctrl;
    }

    static AnimatorState AddBlendState(AnimatorController ctrl, string stateName,
        string clipKey, Dictionary<string, AnimationClip> clips)
    {
        BlendTree tree;
        var state = ctrl.CreateBlendTreeInController(stateName, out tree);
        tree.blendType       = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter  = "x";
        tree.blendParameterY = "y";

        foreach (var (dir, blend) in Directions)
        {
            string key = $"{dir}_{clipKey}";
            if (clips.TryGetValue(key, out var clip))
                tree.AddChild(clip, blend);
        }
        return state;
    }

    static void AddTriggerless(AnimatorState from, AnimatorState to, string param, bool value)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    static void AddTrigger(AnimatorState from, AnimatorState to, string param)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0;
        t.AddCondition(AnimatorConditionMode.If, 0, param);
    }

    static void AddExitTime(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0;
    }

    // ── 4. WarriorAvatarConfig ────────────────────────────────────────────
    static WarriorAvatarConfig CreateConfig()
    {
        var cfg = LoadOrCreateSO<WarriorAvatarConfig>(OUT_CONFIG);

        // Base warrior (Front view)
        cfg.baseBody      = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Body.png");
        cfg.baseHead      = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Head.png");
        cfg.baseLeftArm   = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Left_Arm.png");
        cfg.baseRightArm  = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Right_Arm.png");
        cfg.baseLeftHand  = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Left_Hand.png");
        cfg.baseRightHand = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Right_Hand.png");
        cfg.baseLeftLeg   = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Left_Leg.png");
        cfg.baseRightLeg  = LoadSprite($"{ROOT}/Warrior_base/PNG/Front/Right_Leg.png");

        // Light outfit (clothes_1 — Wooden / Iron)
        FillOutfit(ref cfg.lightOutfit, "Warrior_clothes_1");

        // Heavy outfit (clothes_2 — Steel / Rizean Steel)
        FillOutfit(ref cfg.heavyOutfit, "Warrior_clothes_2");

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        Debug.Log($"[WarriorSetup] WarriorAvatarConfig saved: {OUT_CONFIG}");
        return cfg;
    }

    static void FillOutfit(ref WarriorAvatarConfig.OutfitSprites outfit, string clothesFolder)
    {
        string p = $"{ROOT}/{clothesFolder}/PNG/Front";
        outfit.hat       = LoadSprite($"{p}/Hat.png");
        outfit.body      = LoadSprite($"{p}/Body_clothes.png");
        outfit.leftArm   = LoadSprite($"{p}/Left_Arm_clothes.png");
        outfit.rightArm  = LoadSprite($"{p}/Right_Arm_clothes.png");
        outfit.leftHand  = LoadSprite($"{p}/Left_Hand_clothes.png");
        outfit.rightHand = LoadSprite($"{p}/Right_Hand_clothes.png");
        outfit.leftShoe  = LoadSprite($"{p}/Left_Shoes.png");
        outfit.rightShoe = LoadSprite($"{p}/Right_Shoes.png");
        outfit.sword     = LoadSprite($"{p}/Sword.png");
        outfit.shield    = LoadSprite($"{p}/Shiled.png");   // note: typo is in asset filename
    }

    // ── 5. WarriorPortrait prefab ─────────────────────────────────────────
    static void CreatePortraitPrefab(WarriorAvatarConfig cfg)
    {
        var root = new GameObject("WarriorPortrait");
        root.layer = 5;
        var rootRT = root.AddComponent<RectTransform>();

        // Portrait script on root
        var portrait = root.AddComponent<WarriorPortrait>();

        // Helper: create a full-stretch Image child
        Image MkLayer(string name, int order)
        {
            var go = new GameObject(name);
            go.layer = 5;
            go.transform.SetParent(root.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        // Base layers (bottom)
        var baseBody      = MkLayer("BaseBody",      0);
        var baseHead      = MkLayer("BaseHead",      1);
        var baseLeftLeg   = MkLayer("BaseLeftLeg",   2);
        var baseRightLeg  = MkLayer("BaseRightLeg",  3);
        var baseLeftArm   = MkLayer("BaseLeftArm",   4);
        var baseRightArm  = MkLayer("BaseRightArm",  5);
        var baseLeftHand  = MkLayer("BaseLeftHand",  6);
        var baseRightHand = MkLayer("BaseRightHand", 7);

        // Equipment layers (top)
        var bodyLayer      = MkLayer("BodyLayer",      8);
        var leftArmLayer   = MkLayer("LeftArmLayer",   9);
        var rightArmLayer  = MkLayer("RightArmLayer",  10);
        var leftShoeLayer  = MkLayer("LeftShoeLayer",  11);
        var rightShoeLayer = MkLayer("RightShoeLayer", 12);
        var leftHandLayer  = MkLayer("LeftHandLayer",  13);
        var rightHandLayer = MkLayer("RightHandLayer", 14);
        var hatLayer       = MkLayer("HatLayer",       15);
        var swordLayer     = MkLayer("SwordLayer",     16);
        var shieldLayer    = MkLayer("ShieldLayer",    17);

        // Wire serialized fields via SerializedObject
        var so = new SerializedObject(portrait);
        so.FindProperty("config").objectReferenceValue           = cfg;
        so.FindProperty("baseBodyImage").objectReferenceValue    = baseBody;
        so.FindProperty("baseHeadImage").objectReferenceValue    = baseHead;
        so.FindProperty("baseLeftArmImage").objectReferenceValue = baseLeftArm;
        so.FindProperty("baseRightArmImage").objectReferenceValue= baseRightArm;
        so.FindProperty("baseLeftHandImage").objectReferenceValue= baseLeftHand;
        so.FindProperty("baseRightHandImage").objectReferenceValue=baseRightHand;
        so.FindProperty("baseLeftLegImage").objectReferenceValue = baseLeftLeg;
        so.FindProperty("baseRightLegImage").objectReferenceValue= baseRightLeg;
        so.FindProperty("hatLayer").objectReferenceValue         = hatLayer;
        so.FindProperty("bodyLayer").objectReferenceValue        = bodyLayer;
        so.FindProperty("leftArmLayer").objectReferenceValue     = leftArmLayer;
        so.FindProperty("rightArmLayer").objectReferenceValue    = rightArmLayer;
        so.FindProperty("leftHandLayer").objectReferenceValue    = leftHandLayer;
        so.FindProperty("rightHandLayer").objectReferenceValue   = rightHandLayer;
        so.FindProperty("leftShoeLayer").objectReferenceValue    = leftShoeLayer;
        so.FindProperty("rightShoeLayer").objectReferenceValue   = rightShoeLayer;
        so.FindProperty("swordLayer").objectReferenceValue       = swordLayer;
        so.FindProperty("shieldLayer").objectReferenceValue      = shieldLayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, OUT_PREFAB);
        Object.DestroyImmediate(root);
        Debug.Log($"[WarriorSetup] WarriorPortrait prefab saved: {OUT_PREFAB}");
    }

    // ── 6. Swap Player animator ───────────────────────────────────────────
    static void SwapPlayerAnimator(AnimatorController controller)
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogWarning("[WarriorSetup] Player GO not found — open VillageScene."); return; }
        var anim = player.GetComponent<Animator>();
        if (anim == null) return;
        anim.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[WarriorSetup] Player AnimatorController swapped to WarriorController.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    static Sprite LoadSprite(string path)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null) Debug.LogWarning($"[WarriorSetup] Sprite not found: {path}");
        return sp;
    }

    static T LoadOrCreate<T>(string path) where T : Object, new()
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var obj = new T();
        AssetDatabase.CreateAsset(obj, path);
        return obj;
    }

    static T LoadOrCreateSO<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var obj = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(obj, path);
        return obj;
    }

    static AnimatorController LoadOrCreateController(string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;
        return AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    static void SaveOrUpdate(AnimationClip clip, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) == null)
            AssetDatabase.CreateAsset(clip, path);
        else
            EditorUtility.SetDirty(clip);
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path)) return;

        // Walk each segment and create any missing intermediate folders
        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static void EnsureParam(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in ctrl.parameters) if (p.name == name) return;
        ctrl.AddParameter(name, type);
    }
}
#endif
