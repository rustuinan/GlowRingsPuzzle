using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RoundedPanelGraphic : MaskableGraphic
{
    [SerializeField] private Color borderColor = new Color32(128, 113, 255, 255);
    [SerializeField] private float cornerRadius = 48f;
    public void SetStyle(Color fill, Color border, float radius = 32f)
    { color = fill; borderColor = border; cornerRadius = radius; SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
        const int steps = 8;
        const int count = (steps + 1) * 4;
        Vector2[] outer = new Vector2[count];
        Vector2[] inner = new Vector2[count];
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMin + radius : rect.xMax - radius,
                corner < 2 ? rect.yMin + radius : rect.yMax - radius);
            for (int step = 0; step <= steps; step++)
            {
                float angle = (180f + corner * 90f + step * 90f / steps) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                int index = corner * (steps + 1) + step;
                outer[index] = center + direction * radius;
                inner[index] = center + direction * (radius - 3f);
            }
        }
        Color edge = borderColor;
        mesh.AddVert(rect.center, color, Vector2.zero);
        for (int i = 0; i < count; i++) mesh.AddVert(inner[i], color, Vector2.zero);
        for (int i = 0; i < count; i++) mesh.AddTriangle(0, 1 + i, 1 + (i + 1) % count);
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int start = mesh.currentVertCount;
            mesh.AddVert(inner[i], edge, Vector2.zero);
            mesh.AddVert(outer[i], edge, Vector2.zero);
            mesh.AddVert(outer[next], edge, Vector2.zero);
            mesh.AddVert(inner[next], edge, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
