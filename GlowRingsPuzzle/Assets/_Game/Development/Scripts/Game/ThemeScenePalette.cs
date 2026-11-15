using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>Color adjustments on original shaders. Original mode restores the captured scene appearance.</summary>
public static class ThemeScenePalette
{
    private sealed class RendererAppearance
    {
        public readonly MaterialPropertyBlock[] Blocks;
        public readonly Material[] Materials;
        public RendererAppearance(Renderer renderer)
        {
            Materials = renderer.sharedMaterials;
            Blocks = new MaterialPropertyBlock[renderer.sharedMaterials.Length];
            for (int i = 0; i < Blocks.Length; i++)
            { Blocks[i] = new MaterialPropertyBlock(); renderer.GetPropertyBlock(Blocks[i], i); }
        }
    }
    private sealed class GraphicAppearance
    {
        public readonly Color Color;
        public readonly Color Border;
        public readonly float Radius;
        public GraphicAppearance(Graphic graphic)
        {
            Color = graphic.color;
            if (graphic is RoundedPanelGraphic panel) { Border = panel.BorderColor; Radius = panel.CornerRadius; }
        }
    }
    private static ConditionalWeakTable<Renderer, RendererAppearance> renderers = new ConditionalWeakTable<Renderer, RendererAppearance>();
    private static ConditionalWeakTable<Graphic, GraphicAppearance> graphics = new ConditionalWeakTable<Graphic, GraphicAppearance>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        renderers = new ConditionalWeakTable<Renderer, RendererAppearance>();
        graphics = new ConditionalWeakTable<Graphic, GraphicAppearance>();
    }
    public static void Apply(Scene scene, ThemeData theme)
    {
        if (theme == null) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponentInParent<Ring>() != null || renderer.gameObject.layer == 30) continue;
                ApplyRenderer(renderer, theme);
            }
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.GetComponentInParent<CosmeticsStoreUI>() != null || graphic.GetComponentInParent<DailySpinUI>() != null || graphic is RawImage) continue;
                var original = graphics.GetValue(graphic, item => new GraphicAppearance(item));
                if (theme.IsOriginal)
                {
                    if (graphic is RoundedPanelGraphic originalPanel) originalPanel.SetStyle(original.Color, original.Border, original.Radius);
                    else graphic.color = original.Color;
                    continue;
                }
                Color color = original.Color;
                if (color.a < 0.05f) continue;
                Color.RGBToHSV(color, out _, out float saturation, out float brightness);
                Color next;
                if (graphic is TMP_Text) next = saturation > 0.25f ? theme.Accent : theme.Text;
                else if (graphic is RoundedPanelGraphic panel) { panel.ApplyPalette(theme.Surface, theme.Accent); continue; }
                else if (brightness < 0.45f) next = theme.Surface;
                else if (saturation > 0.2f) next = theme.Secondary;
                else continue;
                next.a = color.a; graphic.color = next;
            }
        }
    }
    public static void ApplyRenderer(Renderer renderer, ThemeData theme)
    {
        if (renderer == null || theme == null) return;
        var original = renderers.GetValue(renderer, item => new RendererAppearance(item));
        bool boardRenderer = original.Materials.AnyBoardMaterial();
        if (boardRenderer)
            renderer.sharedMaterials = !theme.IsOriginal && theme.BoardSurfaceMaterials != null && theme.BoardSurfaceMaterials.Length == original.Materials.Length
                ? theme.BoardSurfaceMaterials : original.Materials;
        Material[] materials = renderer.sharedMaterials;
        for (int index = 0; index < materials.Length; index++)
        {
            if (theme.IsOriginal)
            { renderer.SetPropertyBlock(index < original.Blocks.Length ? original.Blocks[index] : null, index); continue; }
            Material material = materials[index]; if (material == null) continue;
            string shader = material.shader.name;
            bool background = material.HasProperty("_TopColor");
            bool trim = materials.Length > 1 && shader.Contains("Ring Lit Neon");
            bool board = material.name.Contains("BoardGrid") || material.name.Contains("BoardOuterFrame");
            if (boardRenderer && theme.BoardSurfaceMaterials != null && theme.BoardSurfaceMaterials.Length == materials.Length)
            { renderer.SetPropertyBlock(index < original.Blocks.Length ? original.Blocks[index] : null, index); continue; }
            bool surface = board || trim || shader.Contains("Metallic Surface") || shader.Contains("Board Neon") || shader.Contains("Universal Render Pipeline/Lit");
            if (!background && !surface) continue;
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, index);
            // Keep source textures, normal maps, metallic response and the original shaders.
            Color tint = trim ? theme.Accent : board
                ? Color.Lerp(Color.white, material.name.Contains("GridFace") ? theme.BoardFace : theme.BoardFrame, 0.22f)
                : Color.Lerp(Color.white, theme.Surface, 0.25f);
            Set(material, block, "_BaseColor", tint);
            Set(material, block, "_RimColor", theme.Secondary);
            Set(material, block, "_TopTint", Color.Lerp(theme.Surface, theme.Accent, 0.3f));
            Set(material, block, "_BottomTint", theme.BackgroundBottom);
            Set(material, block, "_FresnelColor", theme.Accent);
            Set(material, block, "_EdgeGlowColor", theme.Accent);
            Set(material, block, "_EmissionColor", theme.Accent);
            Set(material, block, "_ReflectionColor", theme.Secondary);
            Set(material, block, "_MetallicTint", theme.Accent);
            Set(material, block, "_TopColor", theme.BackgroundTop);
            Set(material, block, "_MiddleColor", Color.Lerp(theme.BackgroundTop, theme.BackgroundBottom, 0.5f));
            Set(material, block, "_BottomColor", theme.BackgroundBottom);
            Set(material, block, "_CenterGlowColor", theme.Secondary);
            Set(material, block, "_VignetteColor", theme.BackgroundBottom);
            Set(material, block, "_BlobColorA", theme.Surface);
            Set(material, block, "_BlobColorB", theme.Secondary);
            Set(material, block, "_FogColor", Color.Lerp(theme.BackgroundBottom, theme.Secondary, 0.15f));
            Set(material, block, "_StarColor", theme.Accent);
            renderer.SetPropertyBlock(block, index);
        }
    }
    private static void Set(Material material, MaterialPropertyBlock block, string property, Color color)
    { if (material.HasProperty(property)) block.SetColor(property, color); }
    private static bool AnyBoardMaterial(this Material[] materials)
    {
        foreach (var material in materials)
            if (material != null && (material.name.Contains("BoardGrid") || material.name.Contains("BoardOuterFrame"))) return true;
        return false;
    }
}
