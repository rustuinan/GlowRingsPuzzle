using UnityEngine;
using UnityEngine.UI;

/// <summary>Glossy prize sectors plus a separate stationary marquee frame and jewel pointer.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DailySpinWheelGraphic : MaskableGraphic
{
    private int segments = 8;
    private int selected = -1;
    private int mode;
    private bool spinning;
    private float celebrationUntil;
    private float nextRefresh;
    private static readonly Color Gold = new Color32(255, 204, 92, 255);
    private static readonly Color[] Colors = {
        new Color32(102, 67, 225, 255), new Color32(17, 170, 196, 255),
        new Color32(158, 76, 228, 255), new Color32(227, 81, 147, 255),
        new Color32(54, 125, 224, 255), new Color32(22, 177, 146, 255),
        new Color32(184, 76, 202, 255), new Color32(231, 162, 39, 255) };
    public void Configure(int count) { segments = count; mode = 0; raycastTarget = false; SetVerticesDirty(); }
    public void ConfigureFrame() { mode = 1; raycastTarget = false; SetVerticesDirty(); }
    public void ConfigurePointer() { mode = 2; raycastTarget = false; SetVerticesDirty(); }
    public void SetSpinning(bool value) { spinning = value; SetVerticesDirty(); }
    public void Celebrate() { celebrationUntil = Time.unscaledTime + 3.2f; SetVerticesDirty(); }
    public void ResetCelebration() { celebrationUntil = 0; SetVerticesDirty(); }
    public void Highlight(int index) { selected = index; SetVerticesDirty(); }
    private void Update()
    {
        if (mode != 1 && selected < 0) return;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.065f; SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (mode == 2) { Pointer(mesh); return; }
        if (mode == 1) { Frame(mesh); return; }
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f - 40;
        float step = 360f / Mathf.Max(3, segments);
        for (int segment = 0; segment < segments; segment++)
        {
            Color fill = Colors[segment % Colors.Length];
            bool winning = segment == selected;
            if (winning) fill = Color.Lerp(fill, new Color32(255, 225, 127, 255), 0.28f + Mathf.Sin(Time.unscaledTime * 3) * 0.06f);
            float first = 90 + step * 0.5f - segment * step;
            float last = first - step;
            Sector(mesh, Vector2.zero, 0, radius * 0.43f, first, last, Color.Lerp(fill, Color.black, 0.28f), fill);
            Sector(mesh, Vector2.zero, radius * 0.43f, radius * 0.82f, first, last, fill, Color.Lerp(fill, Color.white, 0.12f));
            Sector(mesh, Vector2.zero, radius * 0.82f, radius, first, last, Color.Lerp(fill, Color.white, 0.12f), Color.Lerp(fill, Color.black, 0.13f));
            Sector(mesh, Vector2.zero, radius - 27, radius - 23, first - 2, last + 2, new Color(1, 1, 1, 0.14f), new Color(1, 1, 1, 0));
            float edge = first * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(edge), Mathf.Sin(edge));
            Vector2 normal = new Vector2(-direction.y, direction.x) * 1.6f;
            Quad(mesh, direction * 76 - normal, direction * radius - normal, direction * radius + normal, direction * 76 + normal,
                new Color32(233, 225, 255, 190));
            if (winning)
                Sector(mesh, Vector2.zero, radius - 9, radius, first - 0.5f, last + 0.5f, new Color32(255, 241, 177, 255), Gold);
        }
    }
    private void Frame(VertexHelper mesh)
    {
        float outer = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f - 16;
        float inner = outer - 44;
        bool celebrate = Time.unscaledTime < celebrationUntil;
        float glow = celebrate ? 0.30f + Mathf.Sin(Time.unscaledTime * 4) * 0.07f : 0.14f;
        Sector(mesh, Vector2.zero, outer - 2, outer + 30, 0, 360, new Color(1, 0.69f, 0.18f, glow), new Color(1, 0.5f, 0.1f, 0));
        Disc(mesh, new Vector2(0, -14), outer + 3, new Color32(9, 6, 28, 255), new Color32(9, 6, 28, 255));
        Sector(mesh, Vector2.zero, inner, inner + 7, 0, 360, new Color32(162, 236, 255, 255), new Color32(37, 28, 80, 255));
        Sector(mesh, Vector2.zero, inner + 7, outer - 9, 0, 360, new Color32(255, 222, 139, 255), new Color32(203, 128, 29, 255));
        Sector(mesh, Vector2.zero, outer - 9, outer - 4, 0, 360, new Color32(255, 244, 188, 255), new Color32(250, 194, 79, 255));
        Sector(mesh, Vector2.zero, outer - 4, outer, 0, 360, new Color32(180, 106, 29, 255), new Color32(90, 49, 28, 255));
        for (int i = 0; i < 40; i++)
        {
            float angle = i * Mathf.PI * 2 / 40;
            Vector2 center = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (outer - 23);
            float phase = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (spinning ? 10 : 2) - i * 0.6f);
            Color bulb = Color.Lerp(new Color32(233, 159, 52, 255), new Color32(255, 251, 221, 255), celebrate ? 0.85f : 0.38f + phase * 0.62f);
            Disc(mesh, center, 11, new Color(1, 0.92f, 0.6f, 0.24f + phase * 0.13f), new Color(1, 0.8f, 0.3f, 0));
            Disc(mesh, center, 5, bulb, Color.Lerp(bulb, Gold, 0.5f));
        }
    }
    private static void Pointer(VertexHelper mesh)
    {
        Quad(mesh, new Vector2(-35, 34), new Vector2(35, 34), new Vector2(29, 5), new Vector2(-29, 5), new Color32(120, 63, 17, 255));
        int start = mesh.currentVertCount;
        mesh.AddVert(new Vector2(-29, 28), new Color32(255, 245, 194, 255), Vector2.zero);
        mesh.AddVert(new Vector2(29, 28), Gold, Vector2.zero);
        mesh.AddVert(new Vector2(0, -38), new Color32(243, 163, 35, 255), Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        start = mesh.currentVertCount;
        mesh.AddVert(new Vector2(-17, 23), Color.white, Vector2.zero);
        mesh.AddVert(new Vector2(0, -23), new Color32(255, 220, 132, 255), Vector2.zero);
        mesh.AddVert(new Vector2(0, 23), new Color32(255, 243, 192, 255), Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
    }
    private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color inside, Color outside)
    { Sector(mesh, center, 0, radius, 0, 360, inside, outside); }
    private static void Sector(VertexHelper mesh, Vector2 center, float inner, float outer, float from, float to, Color inside, Color outside)
    {
        int steps = outer < 20 ? 12 : Mathf.Max(3, Mathf.CeilToInt(Mathf.Abs(to - from) / 3));
        for (int i = 0; i < steps; i++)
        {
            float a = Mathf.Lerp(from, to, (float)i / steps) * Mathf.Deg2Rad;
            float b = Mathf.Lerp(from, to, (float)(i + 1) / steps) * Mathf.Deg2Rad;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            int start = mesh.currentVertCount;
            mesh.AddVert(center + p * inner, inside, Vector2.zero); mesh.AddVert(center + q * inner, inside, Vector2.zero);
            mesh.AddVert(center + q * outer, outside, Vector2.zero); mesh.AddVert(center + p * outer, outside, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
    private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero);
        mesh.AddVert(c, color, Vector2.zero); mesh.AddVert(d, color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
    }
}
