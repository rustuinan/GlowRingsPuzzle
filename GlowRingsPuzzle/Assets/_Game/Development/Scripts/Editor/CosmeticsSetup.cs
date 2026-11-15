using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CosmeticsSetup
{
    private const string Root = "Assets/_Game/Development/Data/Cosmetics";
    private const string GameScene = "Assets/_Game/Development/Scene/Game.unity";
    [MenuItem("Tools/Glow Rings/Install Theme Catalog")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Install in Edit Mode.");
        Directory.CreateDirectory(Root); Directory.CreateDirectory("Assets/_Game/Resources"); AssetDatabase.Refresh();
        var original = AssetDatabase.LoadAssetAtPath<ThemeData>("Assets/_Game/Development/Data/Theme/ThemeData.asset");
        var catalog = Asset<CosmeticsCatalog>("Assets/_Game/Resources/CosmeticsCatalog.asset");
        string[] themeIds = { "midnight", "ocean", "sunset", "forest", "aurora" };
        string[] titles = { "Midnight", "Ocean", "Sunset", "Forest", "Aurora" };
        int[] scores = { 0, 500, 1500, 3000, 6000 };
        string[] top = { "21143F", "063D59", "582342", "103D33", "30225D" };
        string[] bottom = { "090720", "031823", "210D25", "061A17", "100B2D" };
        string[] surface = { "292044", "10435A", "532C46", "174536", "382856" };
        string[] accent = { "8CEAFF", "72F1E1", "FFD18A", "B9F18C", "E6B1FF" };
        string[] secondary = { "A17BFF", "75BFFF", "FF8DAD", "68DDC0", "81F4EB" };
        var themes = new ThemeData[5];
        for (int i = 0; i < themes.Length; i++)
        {
            var theme = Asset<ThemeData>(Root + "/Theme_" + themeIds[i] + ".asset");
            EditorUtility.CopySerialized(original, theme); theme.name = "Theme_" + themeIds[i];
            theme.Id = themeIds[i]; theme.BackgroundTop = Hex(top[i]); theme.BackgroundBottom = Hex(bottom[i]);
            theme.Surface = Hex(surface[i]); theme.Accent = Hex(accent[i]); theme.Secondary = Hex(secondary[i]); theme.Text = Hex("F8F4FF");
            var serialized = new SerializedObject(theme);
            serialized.FindProperty("themeName").stringValue = titles[i]; serialized.FindProperty("requiredScore").intValue = scores[i];
            var colors = serialized.FindProperty("ringColors");
            for (int color = 0; color < original.RingColors.Length; color++)
            {
                var source = original.RingColors[color];
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/" + themeIds[i] + "_" + source.ColorType + ".mat");
                if (material == null) { material = new Material(source.Material); AssetDatabase.CreateAsset(material, Root + "/" + themeIds[i] + "_" + source.ColorType + ".mat"); }
                else EditorUtility.CopySerialized(source.Material, material);
                if (i > 0)
                {
                    Color tint = Color.Lerp(source.Material.GetColor("_BaseColor"), theme.Accent, 0.08f);
                    material.SetColor("_BaseColor", tint); material.SetColor("_EmissionColor", tint);
                }
                var data = Asset<RingColorData>(Root + "/" + themeIds[i] + "_" + source.ColorType + ".asset");
                var colorSerialized = new SerializedObject(data);
                colorSerialized.FindProperty("colorType").enumValueIndex = (int)source.ColorType;
                colorSerialized.FindProperty("material").objectReferenceValue = material; colorSerialized.ApplyModifiedPropertiesWithoutUndo();
                colors.GetArrayElementAtIndex(color).objectReferenceValue = data;
                EditorUtility.SetDirty(material); EditorUtility.SetDirty(data);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(theme); themes[i] = theme;
        }
        Scene current = SceneManager.GetActiveScene();
        bool alreadyOpen = current.path == GameScene;
        Scene game = alreadyOpen ? current : EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Additive);
        var board = game.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoardManager>()).First();
        var previewRoot = new GameObject("Gameplay Board Preview");
        foreach (var source in board.GetComponentsInChildren<MeshRenderer>(true))
        {
            var filter = source.GetComponent<MeshFilter>(); if (filter == null) continue;
            var clone = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
            clone.transform.SetParent(previewRoot.transform, false);
            clone.transform.localPosition = source.transform.position - board.transform.position;
            clone.transform.localRotation = source.transform.rotation; clone.transform.localScale = source.transform.lossyScale;
            clone.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            clone.GetComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
        }
        catalog.PreviewBoard = PrefabUtility.SaveAsPrefabAsset(previewRoot, Root + "/GameplayBoardPreview.prefab");
        UnityEngine.Object.DestroyImmediate(previewRoot);
        catalog.Themes = themes;
        CosmeticsArtSetup.Apply(catalog);
        themes = catalog.Themes;
        var manager = game.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ThemeManager>()).First();
        var managerSerialized = new SerializedObject(manager);
        var list = managerSerialized.FindProperty("themes"); list.arraySize = themes.Length;
        for (int i = 0; i < themes.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = themes[i];
        managerSerialized.FindProperty("currentTheme").objectReferenceValue = null;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(game); EditorSceneManager.SaveScene(game);
        if (!alreadyOpen) { EditorSceneManager.CloseScene(game, true); SceneManager.SetActiveScene(current); }
        AssetDatabase.SaveAssets();
        Debug.Log("Theme catalog installed.");
    }
    private static T Asset<T>(string path) where T : ScriptableObject
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value != null) return value;
        value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value;
    }
    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
}
