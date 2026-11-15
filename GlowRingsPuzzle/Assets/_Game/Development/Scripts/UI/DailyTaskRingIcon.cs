using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class DailyTaskRingIcon : MaskableGraphic
{
    private static readonly Color32 BlueEdge = new Color32(23, 94, 183, 255);
    private static readonly Color32 BlueFace = new Color32(29, 167, 243, 255);
    private static readonly Color32 BlueHighlight = new Color32(137, 220, 255, 255);

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        const int segments = 48;
        const int verticesPerSegment = 3;
        Rect rect = rectTransform.rect;
        float unit = Mathf.Min(rect.width, rect.height) / 72f;

        for (int ring = 0; ring < 3; ring++)
        {
            float radius = (35f - 11f * ring) * unit;
            int start = mesh.currentVertCount;
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment * (2f * Mathf.PI / segments);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Color face = Color.Lerp(BlueFace, BlueHighlight, Mathf.Clamp01((direction.y + 0.2f) * 0.65f));
                mesh.AddVert(rect.center + direction * radius, BlueEdge, Vector2.zero);
                mesh.AddVert(rect.center + direction * (radius - 2f * unit), face, Vector2.zero);
                mesh.AddVert(rect.center + direction * (radius - 6f * unit), BlueFace, Vector2.zero);
            }
            for (int segment = 0; segment < segments; segment++)
            {
                int current = start + segment * verticesPerSegment;
                int next = start + (segment + 1) % segments * verticesPerSegment;
                mesh.AddTriangle(current, current + 1, next);
                mesh.AddTriangle(next, current + 1, next + 1);
                mesh.AddTriangle(current + 1, current + 2, next + 1);
                mesh.AddTriangle(next + 1, current + 2, next + 2);
            }
        }
    }
}
