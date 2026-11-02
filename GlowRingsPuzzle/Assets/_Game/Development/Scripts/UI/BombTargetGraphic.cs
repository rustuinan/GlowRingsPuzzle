using UnityEngine;
using UnityEngine.UI;

/// <summary>Non-interactive outlines projected onto the nine selectable board cells.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class BombTargetGraphic : MaskableGraphic
{
    [SerializeField] private BoardManager board;
    public void Configure(BoardManager value) { board = value; raycastTarget = false; }

    private void LateUpdate() { SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Camera camera = Camera.main;
        if (board == null || camera == null) return;
        Canvas rootCanvas = canvas.rootCanvas;
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        float halfSize = 0.44f;
        if (board.Cells.Length > 1 && board.Cells[0] != null && board.Cells[1] != null)
            halfSize *= Vector3.Distance(board.Cells[0].transform.position, board.Cells[1].transform.position);
        foreach (Cell cell in board.Cells)
        {
            if (cell == null) continue;
            bool occupied = cell.GetAllRings().Length > 0;
            Color tint = occupied ? new Color32(255, 215, 95, 240) : new Color32(164, 169, 208, 70);
            Vector2[] points = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = board.transform.right * ((i == 0 || i == 3) ? -halfSize : halfSize)
                    + board.transform.forward * (i < 2 ? -halfSize : halfSize);
                Vector3 screen = camera.WorldToScreenPoint(cell.transform.position + offset + board.transform.up * 0.1f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, uiCamera, out points[i]);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % 4];
                Vector2 delta = (b - a).normalized;
                Vector2 normal = new Vector2(-delta.y, delta.x) * 2.5f;
                int start = mesh.currentVertCount;
                mesh.AddVert(a + normal, tint, Vector2.zero);
                mesh.AddVert(b + normal, tint, Vector2.zero);
                mesh.AddVert(b - normal, tint, Vector2.zero);
                mesh.AddVert(a - normal, tint, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
