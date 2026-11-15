using UnityEngine;
using UnityEngine.UI;

/// <summary>Lightweight palette-driven gradients, without downloaded textures or screenshots.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StoreBackdropGraphic : MaskableGraphic
{
    private Color top;
    private Color bottom;
    private Color glow;
    private float radius;
    private Material themeMaterial;
    public void Configure(Color upper, Color lower, Color accent, float cornerRadius = 0)
    { ReleaseMaterial(); top = upper; bottom = lower; glow = accent; radius = cornerRadius; raycastTarget = false; SetVerticesDirty(); }
    public void ConfigureTheme(ThemeData theme, float cornerRadius = 0)
    {
        Configure(theme.BackgroundTop, theme.BackgroundBottom, theme.Accent, cornerRadius);
        var template = Resources.Load<Material>("ThemeAtmosphereUI");
        if (template == null) return;
        themeMaterial = new Material(template) { name = theme.ThemeName + " Atmosphere", hideFlags = HideFlags.DontSave };
        themeMaterial.SetColor("_TopColor", theme.BackgroundTop); themeMaterial.SetColor("_BottomColor", theme.BackgroundBottom);
        themeMaterial.SetColor("_Accent", theme.Accent); themeMaterial.SetColor("_Secondary", theme.Secondary);
        themeMaterial.SetFloat("_ThemeStyle", theme.Atmosphere); material = themeMaterial;
    }
    protected override void OnDisable() { ReleaseMaterial(); base.OnDisable(); }
    protected override void OnDestroy() { ReleaseMaterial(); base.OnDestroy(); }
    private void ReleaseMaterial()
    {
        if (themeMaterial == null) return;
        material = null;
        if (Application.isPlaying) Destroy(themeMaterial); else DestroyImmediate(themeMaterial);
        themeMaterial = null;
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (radius > 0)
        {
            float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
            mesh.AddVert(rect.center, Sample(0.5f, 0.5f), Vector2.one * 0.5f);
            const int steps = 12;
            const int count = (steps + 1) * 4;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMin + r : rect.xMax - r,
                    corner < 2 ? rect.yMin + r : rect.yMax - r);
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (180 + corner * 90 + step * 90f / steps) * Mathf.Deg2Rad;
                    Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                    Vector2 uv = new Vector2((point.x - rect.xMin) / rect.width, (point.y - rect.yMin) / rect.height);
                    mesh.AddVert(point, Sample(uv.x, uv.y), uv);
                }
            }
            for (int i = 0; i < count; i++) mesh.AddTriangle(0, 1 + i, 1 + (i + 1) % count);
            return;
        }
        const int columns = 16, rows = 24;
        for (int y = 0; y <= rows; y++) for (int x = 0; x <= columns; x++)
        {
            float u = (float)x / columns, v = (float)y / rows;
            mesh.AddVert(new Vector2(rect.xMin + rect.width * u, rect.yMin + rect.height * v), Sample(u, v), new Vector2(u, v));
        }
        for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
        {
            int a = y * (columns + 1) + x;
            mesh.AddTriangle(a, a + columns + 1, a + 1);
            mesh.AddTriangle(a + 1, a + columns + 1, a + columns + 2);
        }
    }
    private Color Sample(float u, float v)
    {
        float haze = Mathf.Exp(-((u - 0.22f) * (u - 0.22f) * 7f + (v - 0.72f) * (v - 0.72f) * 11f));
        return Color.Lerp(Color.Lerp(bottom, top, v), glow, haze * 0.14f);
    }
}
