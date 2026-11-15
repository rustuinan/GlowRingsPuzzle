using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Exercises the original-art contract and restores touched saves.</summary>
public static class CosmeticsChecks
{
    [MenuItem("Tools/Glow Rings/Validate Cosmetics (Edit Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
        var catalog = CosmeticsCatalog.Load();
        var source = AssetDatabase.LoadAssetAtPath<ThemeData>("Assets/_Game/Development/Data/Theme/ThemeData.asset");
        var original = catalog.Themes.Single(x => x.IsOriginal);
        Require(original.CoinPrice == 0 && original.Id == "original", "Original theme must be a free selectable entry.");
        Require(catalog.Themes.All(x => x.IsValid()), "Theme models and colors must remain valid.");
        foreach(var theme in catalog.Themes)foreach(var color in theme.RingColors)
            Require(color.Material.GetColor("_BaseColor")==source.GetColorData(color.ColorType).Material.GetColor("_BaseColor"),
                "A theme must not shift the matching color identity: "+theme.Id+" "+color.ColorType);
        foreach (string shaderName in new[] { "GlowRings/URP/Ring Lit Neon Controlled", "Puzzle/URP/MobileSoftBackground_Unlit_Fixed" })
            Require(Shader.Find(shaderName) != null && !ShaderUtil.ShaderHasError(Shader.Find(shaderName)), "Original shader must compile: " + shaderName);
        var keys = new List<string> { CosmeticsSave.Prefix + "DemoCoins", CosmeticsSave.Prefix + "ThemeEconomyVersion", "GlowRings.HighScore" };
        keys.AddRange(catalog.Themes.Select(x => CosmeticsSave.Prefix + "OwnedTheme." + x.Id));
        var integers = keys.ToDictionary(x => x, x => (PlayerPrefs.HasKey(x), PlayerPrefs.GetInt(x)));
        var strings = new[] { CosmeticsSave.Prefix + "Theme", CosmeticsSave.RewardJournalKey }
            .ToDictionary(x => x, x => (PlayerPrefs.HasKey(x), PlayerPrefs.GetString(x)));
        var scene = EditorSceneManager.NewPreviewScene();
        var instance = typeof(ThemeManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var previous = instance.GetValue(null);
        try
        {
            foreach (var key in keys.Concat(strings.Keys)) PlayerPrefs.DeleteKey(key);
            Require(CosmeticsSave.MenuTheme == original, "Original must be available without coins or score.");
            Require(CosmeticsSave.Coins == CosmeticsSave.DemoStartingCoins, "Initial demo balance is incorrect.");
            var paidTheme = catalog.Themes.First(x => x.CoinPrice > 0);
            var managerObject = new GameObject("Cosmetics Check Manager");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(managerObject, scene);
            var manager = managerObject.AddComponent<ThemeManager>();
            var serialized = new SerializedObject(manager); var themes = serialized.FindProperty("themes"); themes.arraySize = catalog.Themes.Length;
            for (int i = 0; i < themes.arraySize; i++) themes.GetArrayElementAtIndex(i).objectReferenceValue = catalog.Themes[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); instance.SetValue(null, manager);
            PlayerPrefs.SetString(CosmeticsSave.Prefix + "Theme", paidTheme.Id);
            Require(CosmeticsSave.MenuTheme == original, "Legacy selections must resolve to Original.");
            typeof(ThemeManager).GetMethod("ApplyTheme", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { original });
            Require(manager.CurrentTheme == original, "Gameplay must use Original.");
            var ringPrefab = AssetDatabase.LoadAssetAtPath<Ring>("Assets/_Game/Development/Prefab/TutorialRing.prefab");
            var ring = ((GameObject)PrefabUtility.InstantiatePrefab(ringPrefab.gameObject, scene)).GetComponent<Ring>();
            var ringData = new SerializedObject(ring); ringData.FindProperty("modelParent").objectReferenceValue = ring.transform; ringData.ApplyModifiedPropertiesWithoutUndo();
            ring.Initialize(RingLayer.Outer, RingColorType.Blue);
            var renderer = ring.GetComponentInChildren<Renderer>();
            Require(renderer.GetComponent<MeshFilter>().sharedMesh == source.RingModelData.OuterModelPrefab.GetComponentInChildren<MeshFilter>().sharedMesh,
                "Ring must instantiate the original model.");
            Require(renderer.sharedMaterial.shader == source.GetColorData(RingColorType.Blue).Material.shader, "Ring must retain original ring shader.");
            var board = (GameObject)PrefabUtility.InstantiatePrefab(catalog.PreviewBoard, scene);
            foreach (var boardRenderer in board.GetComponentsInChildren<Renderer>())
            {
                var materials = boardRenderer.sharedMaterials;
                var textures = materials.Select(x => x.HasProperty("_BaseMap") ? x.GetTexture("_BaseMap") : null).ToArray();
                ThemeScenePalette.ApplyRenderer(boardRenderer, paidTheme);
                var themed = boardRenderer.sharedMaterials;
                Require(materials.Select(x => x.shader).SequenceEqual(themed.Select(x => x.shader)), "Theme must retain every original board shader.");
                for (int i = 0; i < materials.Length; i++)
                {
                    var block = new MaterialPropertyBlock(); boardRenderer.GetPropertyBlock(block, i);
                    Require(block.GetTexture("_BaseMap") == null, "Theme must preserve original board textures.");
                    Require(!materials[i].HasProperty("_BaseMap") || materials[i].GetTexture("_BaseMap") == textures[i], "Source albedo was edited.");
                }
                ThemeScenePalette.ApplyRenderer(boardRenderer, original);
                Require(materials.SequenceEqual(boardRenderer.sharedMaterials), "Original mode must restore the source material assignments.");
                for (int i = 0; i < materials.Length; i++)
                { var block = new MaterialPropertyBlock(); boardRenderer.GetPropertyBlock(block, i); Require(block.isEmpty, "Original mode must restore initial renderer state."); }
            }
            Debug.Log("CosmeticsChecks PASS: original meshes, shaders and textures preserved.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene); instance.SetValue(null, previous);
            foreach (var entry in integers) { if (entry.Value.Item1) PlayerPrefs.SetInt(entry.Key, entry.Value.Item2); else PlayerPrefs.DeleteKey(entry.Key); }
            foreach (var entry in strings) { if (entry.Value.Item1) PlayerPrefs.SetString(entry.Key, entry.Value.Item2); else PlayerPrefs.DeleteKey(entry.Key); }
            PlayerPrefs.Save();
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException("CosmeticsChecks: " + message); }
}
