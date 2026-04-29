using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [Header("Board Cells")]
    [SerializeField] private Cell[] cells = new Cell[9];

    [Header("Editor Layout")]
    [SerializeField] private bool autoLayoutInEditor = true;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float cellSpacing = 1.6f;
    [SerializeField] private Vector3 cellScale = Vector3.one;
    [SerializeField] private Vector3 cellVisualScale = Vector3.one;
    [SerializeField] private Vector3 ringParentLocalPosition = new Vector3(0f, 0.08f, 0f);

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoCellRadius = 0.45f;
    [SerializeField] private Color gizmoCellColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Color gizmoLineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color gizmoCenterColor = new Color(1f, 0.8f, 0f, 1f);

    public Cell[] Cells => cells;
    public float CellSpacing => cellSpacing;

    private static readonly int[,] Lines =
    {
        { 0, 1, 2 },
        { 3, 4, 5 },
        { 6, 7, 8 },
        { 0, 3, 6 },
        { 1, 4, 7 },
        { 2, 5, 8 },
        { 0, 4, 8 },
        { 2, 4, 6 }
    };

    private void Awake()
    {
        ValidateCells();
    }

    private void OnValidate()
    {
        cellSpacing = Mathf.Max(0.2f, cellSpacing);
        gizmoCellRadius = Mathf.Max(0.05f, gizmoCellRadius);

        if (!Application.isPlaying && autoLayoutInEditor)
            ApplyEditorLayout();
    }

    [ContextMenu("Apply Board Layout")]
    public void ApplyEditorLayout()
    {
        if (cells == null || cells.Length != 9)
            return;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
                continue;

            Vector3 position = GetCellWorldPosition(i);
            cells[i].transform.position = position;
            cells[i].transform.localScale = cellScale;
            cells[i].ApplyEditorDesign(cellVisualScale, ringParentLocalPosition);
        }
    }

    public Vector3 GetCellWorldPosition(int index)
    {
        int row = index / 3;
        int column = index % 3;

        float x = (column - 1) * cellSpacing;
        float z = (1 - row) * cellSpacing;

        return transform.position + boardCenter + new Vector3(x, 0f, z);
    }

    public Cell GetClosestCell(Vector3 worldPosition, float maxDistance)
    {
        if (!ValidateCells())
            return null;

        Cell closestCell = null;
        float closestDistance = maxDistance;

        for (int i = 0; i < cells.Length; i++)
        {
            float distance = Vector3.Distance(worldPosition, cells[i].transform.position);

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestCell = cells[i];
            }
        }

        return closestCell;
    }

    public bool HasMove(RingPiece piece)
    {
        if (piece == null || !ValidateCells())
            return false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].CanPlace(piece))
                return true;
        }

        return false;
    }

    public List<MatchData> FindMatches()
    {
        List<MatchData> matches = new List<MatchData>();

        if (!ValidateCells())
            return matches;

        for (int i = 0; i < Lines.GetLength(0); i++)
        {
            CheckLineAnyLayer(matches, Lines[i, 0], Lines[i, 1], Lines[i, 2]);
        }

        return matches;
    }

    public int ClearMatches(List<MatchData> matches)
    {
        if (matches == null || matches.Count == 0)
            return 0;

        HashSet<int> clearedIds = new HashSet<int>();
        int clearedCount = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            clearedCount += ClearRingOnce(matches[i].RingA, clearedIds);
            clearedCount += ClearRingOnce(matches[i].RingB, clearedIds);
            clearedCount += ClearRingOnce(matches[i].RingC, clearedIds);
        }

        return clearedCount;
    }

    public bool IsBoardEmpty()
    {
        if (!ValidateCells())
            return false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].GetRing(RingLayer.Outer) != null) return false;
            if (cells[i].GetRing(RingLayer.Middle) != null) return false;
            if (cells[i].GetRing(RingLayer.Inner) != null) return false;
        }

        return true;
    }

    public bool TryFindSingleMatchOpportunity(out RingLayer layer, out RingColorType colorType)
    {
        layer = RingLayer.Outer;
        colorType = RingColorType.Red;

        if (!ValidateCells())
            return false;

        RingLayer[] layers =
        {
            RingLayer.Outer,
            RingLayer.Middle,
            RingLayer.Inner
        };

        for (int l = 0; l < layers.Length; l++)
        {
            for (int i = 0; i < Lines.GetLength(0); i++)
            {
                Cell a = cells[Lines[i, 0]];
                Cell b = cells[Lines[i, 1]];
                Cell c = cells[Lines[i, 2]];

                if (TryGetTwoSameOneEmpty(a, b, c, layers[l], out colorType))
                {
                    layer = layers[l];
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryFindComboOpportunity(out RingLayer firstLayer, out RingLayer secondLayer, out RingColorType colorType)
    {
        firstLayer = RingLayer.Outer;
        secondLayer = RingLayer.Middle;
        colorType = RingColorType.Red;

        if (!ValidateCells())
            return false;

        bool foundFirst = false;

        RingLayer[] layers =
        {
            RingLayer.Outer,
            RingLayer.Middle,
            RingLayer.Inner
        };

        for (int l = 0; l < layers.Length; l++)
        {
            for (int i = 0; i < Lines.GetLength(0); i++)
            {
                Cell a = cells[Lines[i, 0]];
                Cell b = cells[Lines[i, 1]];
                Cell c = cells[Lines[i, 2]];

                if (!TryGetTwoSameOneEmpty(a, b, c, layers[l], out RingColorType foundColor))
                    continue;

                if (!foundFirst)
                {
                    foundFirst = true;
                    firstLayer = layers[l];
                    colorType = foundColor;
                }
                else if (foundColor == colorType && layers[l] != firstLayer)
                {
                    secondLayer = layers[l];
                    return true;
                }
            }
        }

        return false;
    }

    public void ApplyCellMaterial(Material material)
    {
        if (cells == null || material == null)
            return;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                cells[i].ApplyCellMaterial(material);
        }
    }

    private void CheckLineAnyLayer(List<MatchData> matches, int indexA, int indexB, int indexC)
    {
        Ring[] ringsA = cells[indexA].GetAllRings();
        Ring[] ringsB = cells[indexB].GetAllRings();
        Ring[] ringsC = cells[indexC].GetAllRings();

        for (int a = 0; a < ringsA.Length; a++)
        {
            if (ringsA[a] == null) continue;

            for (int b = 0; b < ringsB.Length; b++)
            {
                if (ringsB[b] == null) continue;
                if (ringsA[a].ColorType != ringsB[b].ColorType) continue;

                for (int c = 0; c < ringsC.Length; c++)
                {
                    if (ringsC[c] == null) continue;
                    if (ringsA[a].ColorType != ringsC[c].ColorType) continue;

                    matches.Add(new MatchData(
                        ringsA[a],
                        ringsB[b],
                        ringsC[c],
                        ringsA[a].ColorType
                    ));
                }
            }
        }
    }

    private int ClearRingOnce(Ring ring, HashSet<int> clearedIds)
    {
        if (ring == null)
            return 0;

        int id = ring.GetInstanceID();

        if (clearedIds.Contains(id))
            return 0;

        clearedIds.Add(id);

        Cell parentCell = ring.GetComponentInParent<Cell>();

        if (parentCell != null)
            parentCell.RemoveRing(ring.Layer);
        else
            Destroy(ring.gameObject);

        return 1;
    }

    private bool TryGetTwoSameOneEmpty(Cell a, Cell b, Cell c, RingLayer layer, out RingColorType colorType)
    {
        colorType = RingColorType.Red;

        Ring ringA = a.GetRing(layer);
        Ring ringB = b.GetRing(layer);
        Ring ringC = c.GetRing(layer);

        int filledCount = 0;

        if (ringA != null) filledCount++;
        if (ringB != null) filledCount++;
        if (ringC != null) filledCount++;

        if (filledCount != 2)
            return false;

        if (ringA != null && ringB != null && ringA.ColorType == ringB.ColorType)
        {
            colorType = ringA.ColorType;
            return true;
        }

        if (ringA != null && ringC != null && ringA.ColorType == ringC.ColorType)
        {
            colorType = ringA.ColorType;
            return true;
        }

        if (ringB != null && ringC != null && ringB.ColorType == ringC.ColorType)
        {
            colorType = ringB.ColorType;
            return true;
        }

        return false;
    }

    private bool ValidateCells()
    {
        if (cells == null || cells.Length != 9)
        {
            if (Application.isPlaying)
                Debug.LogError("BoardManager: Cells listesi 9 eleman olmalı.");

            return false;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
            {
                if (Application.isPlaying)
                    Debug.LogError("BoardManager: Cells[" + i + "] boş.");

                return false;
            }
        }

        return true;
    }

    public float GetBoardFillRatio()
    {
        if (!ValidateCells())
            return 0f;

        int filled = 0;
        int total = cells.Length * 3;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].GetRing(RingLayer.Outer) != null) filled++;
            if (cells[i].GetRing(RingLayer.Middle) != null) filled++;
            if (cells[i].GetRing(RingLayer.Inner) != null) filled++;
        }

        return total <= 0 ? 0f : (float)filled / total;
    }

    public bool HasMoveForPieces(IReadOnlyList<RingPiece> pieces)
    {
        if (pieces == null || !ValidateCells())
            return false;

        for (int i = 0; i < pieces.Count; i++)
        {
            RingPiece piece = pieces[i];

            if (piece == null)
                continue;

            if (HasMove(piece))
                return true;
        }

        return false;
    }

    public bool HasMoveForData(RingPieceData data)
    {
        if (data == null || !ValidateCells())
            return false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (CanPlaceDataOnCell(data, cells[i]))
                return true;
        }

        return false;
    }

    public bool TryFindAnyMatchOpportunity(out RingLayer layer, out RingColorType colorType)
    {
        layer = RingLayer.Outer;
        colorType = RingColorType.Red;

        if (!ValidateCells())
            return false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (TryFindCellMatchOpportunity(cells[i], out layer, out colorType))
                return true;
        }

        for (int i = 0; i < Lines.GetLength(0); i++)
        {
            Cell a = cells[Lines[i, 0]];
            Cell b = cells[Lines[i, 1]];
            Cell c = cells[Lines[i, 2]];

            if (TryFindLineMatchOpportunity(a, b, c, out layer, out colorType))
                return true;
        }

        return false;
    }

    private bool CanPlaceDataOnCell(RingPieceData data, Cell cell)
    {
        if (cell == null)
            return false;

        if (data.HasOuter && cell.GetRing(RingLayer.Outer) != null)
            return false;

        if (data.HasMiddle && cell.GetRing(RingLayer.Middle) != null)
            return false;

        if (data.HasInner && cell.GetRing(RingLayer.Inner) != null)
            return false;

        return true;
    }

    private bool TryFindCellMatchOpportunity(Cell cell, out RingLayer emptyLayer, out RingColorType colorType)
    {
        emptyLayer = RingLayer.Outer;
        colorType = RingColorType.Red;

        Ring outer = cell.GetRing(RingLayer.Outer);
        Ring middle = cell.GetRing(RingLayer.Middle);
        Ring inner = cell.GetRing(RingLayer.Inner);

        if (outer != null && middle != null && inner == null && outer.ColorType == middle.ColorType)
        {
            emptyLayer = RingLayer.Inner;
            colorType = outer.ColorType;
            return true;
        }

        if (outer != null && inner != null && middle == null && outer.ColorType == inner.ColorType)
        {
            emptyLayer = RingLayer.Middle;
            colorType = outer.ColorType;
            return true;
        }

        if (middle != null && inner != null && outer == null && middle.ColorType == inner.ColorType)
        {
            emptyLayer = RingLayer.Outer;
            colorType = middle.ColorType;
            return true;
        }

        return false;
    }

    private bool TryFindLineMatchOpportunity(Cell a, Cell b, Cell c, out RingLayer emptyLayer, out RingColorType colorType)
    {
        emptyLayer = RingLayer.Outer;
        colorType = RingColorType.Red;

        RingColorType[] colors =
        {
        RingColorType.Red,
        RingColorType.Blue,
        RingColorType.Green,
        RingColorType.Yellow,
        RingColorType.Purple,
        RingColorType.Cyan
    };

        for (int i = 0; i < colors.Length; i++)
        {
            bool aHas = CellHasColor(a, colors[i]);
            bool bHas = CellHasColor(b, colors[i]);
            bool cHas = CellHasColor(c, colors[i]);

            int count = 0;
            if (aHas) count++;
            if (bHas) count++;
            if (cHas) count++;

            if (count != 2)
                continue;

            Cell missingCell = null;

            if (!aHas) missingCell = a;
            else if (!bHas) missingCell = b;
            else if (!cHas) missingCell = c;

            if (missingCell != null && TryGetEmptyLayer(missingCell, out emptyLayer))
            {
                colorType = colors[i];
                return true;
            }
        }

        return false;
    }

    private bool CellHasColor(Cell cell, RingColorType colorType)
    {
        Ring[] rings = cell.GetAllRings();

        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] != null && rings[i].ColorType == colorType)
                return true;
        }

        return false;
    }

    private bool TryGetEmptyLayer(Cell cell, out RingLayer layer)
    {
        layer = RingLayer.Outer;

        if (cell.GetRing(RingLayer.Outer) == null)
        {
            layer = RingLayer.Outer;
            return true;
        }

        if (cell.GetRing(RingLayer.Middle) == null)
        {
            layer = RingLayer.Middle;
            return true;
        }

        if (cell.GetRing(RingLayer.Inner) == null)
        {
            layer = RingLayer.Inner;
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        DrawBoardGizmos();
    }

    private void DrawBoardGizmos()
    {
        Gizmos.color = gizmoCenterColor;
        Gizmos.DrawSphere(transform.position + boardCenter, 0.08f);

        for (int i = 0; i < 9; i++)
        {
            Vector3 position = GetCellWorldPosition(i);

            Gizmos.color = gizmoCellColor;
            Gizmos.DrawCube(position + Vector3.up * 0.02f, new Vector3(gizmoCellRadius * 2f, 0.04f, gizmoCellRadius * 2f));

            Gizmos.color = gizmoCenterColor;
            Gizmos.DrawSphere(position, 0.05f);
        }

        Gizmos.color = gizmoLineColor;

        for (int i = 0; i < Lines.GetLength(0); i++)
        {
            Vector3 a = GetCellWorldPosition(Lines[i, 0]) + Vector3.up * 0.05f;
            Vector3 b = GetCellWorldPosition(Lines[i, 1]) + Vector3.up * 0.05f;
            Vector3 c = GetCellWorldPosition(Lines[i, 2]) + Vector3.up * 0.05f;

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
        }
    }
}