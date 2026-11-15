using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;

/// <summary>Builds the original and legacy theme palettes.</summary>
public static class CosmeticsArtSetup
{
    [MenuItem("Tools/Glow Rings/Refine Cosmetics Art")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Run in Edit Mode.");
        Apply(CosmeticsCatalog.Load()); ConnectGame(CosmeticsCatalog.Load()); AssetDatabase.SaveAssets();
        Debug.Log("Original-shader palettes rebuilt.");
    }
    public static void Apply(CosmeticsCatalog catalog)
    {
        if (catalog == null) throw new System.InvalidOperationException("Install the catalog first.");
        var source = AssetDatabase.LoadAssetAtPath<ThemeData>("Assets/_Game/Development/Data/Theme/ThemeData.asset");
        string path = "Assets/_Game/Development/Data/Cosmetics/Theme_original.asset";
        var original = AssetDatabase.LoadAssetAtPath<ThemeData>(path);
        if (original == null) { original = ScriptableObject.CreateInstance<ThemeData>(); AssetDatabase.CreateAsset(original, path); }
        EditorUtility.CopySerialized(source, original);
        original.Id = "original"; original.IsOriginal = true; original.CoinPrice = 0;
        var serialized = new SerializedObject(original);
        serialized.FindProperty("themeName").stringValue = "Original";
        serialized.FindProperty("requiredScore").intValue = 0; serialized.ApplyModifiedPropertiesWithoutUndo();
        catalog.Themes = new[] { original }.Concat(catalog.Themes.Where(x => x != null && x.Id != "original")).ToArray();
        foreach (var theme in catalog.Themes)
        {
            if (theme.IsOriginal) { EditorUtility.SetDirty(theme); continue; }
            theme.IsOriginal = false; theme.Atmosphere = 0;
            switch (theme.Id)
            { case "midnight": theme.CoinPrice = 500; break; case "ocean": theme.CoinPrice = 900; break;
              case "sunset": theme.CoinPrice = 1200; break; case "forest": theme.CoinPrice = 1600; break; default: theme.CoinPrice = 2000; break; }
            foreach (var color in theme.RingColors)
            {
                var reference = source.GetColorData(color.ColorType).Material;
                EditorUtility.CopySerialized(reference, color.Material);
                Color tint = reference.GetColor("_BaseColor");
                color.Material.SetColor("_BaseColor", tint); color.Material.SetColor("_EmissionColor", tint);
                EditorUtility.SetDirty(color.Material);
            }
            EditorUtility.SetDirty(theme);
        }
        PremiumCosmeticsArt.Apply(catalog);
        EditorUtility.SetDirty(catalog);
    }
    private static void BuildBoardMaterials(CosmeticsCatalog catalog, ThemeData theme)
    {
        const string folder = "Assets/_Game/Development/Data/Cosmetics/OriginalShaderThemes";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Game/Development/Data/Cosmetics", "OriginalShaderThemes");
        var references = catalog.PreviewBoard.GetComponentInChildren<Renderer>().sharedMaterials;
        theme.BoardSurfaceMaterials = new Material[references.Length];
        for (int i = 0; i < references.Length; i++)
        {
            var reference = references[i]; string path = folder + "/" + theme.Id + "_" + i;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path + ".mat");
            if (material == null) { material = new Material(reference); AssetDatabase.CreateAsset(material, path + ".mat"); }
            else EditorUtility.CopySerialized(reference, material);
            material.name = theme.Id + "_" + i;
            if (reference.HasProperty("_BaseMap") && reference.GetTexture("_BaseMap") != null)
            {
                Color tint = reference.name.Contains("GridFace") ? theme.BoardFace : reference.name.Contains("GridGaps") ? theme.BoardGap : theme.BoardFrame;
                var texture = GradeTexture(reference.GetTexture("_BaseMap"), tint, path + ".png");
                material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", Color.white);
                // The original URP shader provides the lighting, normals and reflections.
                // A low textured emission floor prevents unlit previews losing surface detail.
                float emission = reference.name.Contains("GridFace") ? 0.55f : 0.18f;
                material.SetTexture("_EmissionMap", texture); material.SetColor("_EmissionColor", new Color(emission, emission, emission, 1));
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", theme.Accent);
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", theme.Accent);
            }
            theme.BoardSurfaceMaterials[i] = material; EditorUtility.SetDirty(material);
        }
    }
    private static Texture2D GradeTexture(Texture source, Color tint, string path)
    {
        int width = Mathf.Min(512, source.width), height = Mathf.Max(1, Mathf.RoundToInt((float)width * source.height / source.width));
        var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;
        Texture2D readable = null;
        try
        {
            Graphics.Blit(source, target); RenderTexture.active = target;
            readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, width, height), 0, 0); readable.Apply();
            Color[] pixels = readable.GetPixels();
            float maximum = Mathf.Max(0.001f, pixels.Max(x => Mathf.Max(x.r, Mathf.Max(x.g, x.b))));
            for (int i = 0; i < pixels.Length; i++)
            {
                float detail = Mathf.Clamp01(Mathf.Max(pixels[i].r, Mathf.Max(pixels[i].g, pixels[i].b)) / maximum);
                Color color = tint * Mathf.Lerp(0.76f, 1.16f, Mathf.Sqrt(detail)); color.a = pixels[i].a; pixels[i] = color;
            }
            readable.SetPixels(pixels); readable.Apply(); File.WriteAllBytes(path, readable.EncodeToPNG());
        }
        finally
        { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); if (readable != null) Object.DestroyImmediate(readable); }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static void ConnectGame(CosmeticsCatalog catalog)
    {
        var current = SceneManager.GetActiveScene();
        const string gamePath = "Assets/_Game/Development/Scene/Game.unity";
        var loaded = SceneManager.GetSceneByPath(gamePath);
        bool open = loaded.IsValid() && loaded.isLoaded;
        var game = open ? loaded : EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Additive);
        var manager = game.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ThemeManager>()).First();
        var data = new SerializedObject(manager); var themes = data.FindProperty("themes"); themes.arraySize = catalog.Themes.Length;
        for (int i = 0; i < themes.arraySize; i++) themes.GetArrayElementAtIndex(i).objectReferenceValue = catalog.Themes[i];
        data.FindProperty("currentTheme").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(game); EditorSceneManager.SaveScene(game);
        if (!open) { EditorSceneManager.CloseScene(game, true); SceneManager.SetActiveScene(current); }
    }
}
