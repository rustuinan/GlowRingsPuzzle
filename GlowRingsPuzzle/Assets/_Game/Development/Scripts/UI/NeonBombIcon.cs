using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class NeonBombIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 100f;
        Color glyph = color;
        Disc(mesh, new Vector2(-8, -10) * scale, 34 * scale, glyph);
        Line(mesh, new Vector2(6, 16) * scale, new Vector2(16, 32) * scale, 14 * scale, glyph);
        Line(mesh, new Vector2(16, 32) * scale, new Vector2(28, 34) * scale, 8 * scale, glyph);
        Vector2 spark = new Vector2(39, 38) * scale;
        Line(mesh, spark - Vector2.right * 7 * scale, spark + Vector2.right * 7 * scale, 4 * scale, glyph);
        Line(mesh, spark - Vector2.up * 7 * scale, spark + Vector2.up * 7 * scale, 4 * scale, glyph);
    }

    private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color tint)
    {
        const int sides = 48;
        int start = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(center + direction * radius, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % sides);
        }
    }

    private static void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color color)
    {
        Vector2 delta = (to - from).normalized;
        Vector2 edge = new Vector2(-delta.y, delta.x) * (width * 0.5f);
        int start = mesh.currentVertCount;
        mesh.AddVert(from + edge, color, Vector2.zero);
        mesh.AddVert(to + edge, color, Vector2.zero);
        mesh.AddVert(to - edge, color, Vector2.zero);
        mesh.AddVert(from - edge, color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start, start + 2, start + 3);
    }
}
