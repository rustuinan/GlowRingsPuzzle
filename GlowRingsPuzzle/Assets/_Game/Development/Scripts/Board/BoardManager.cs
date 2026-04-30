using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    [Header("Board Cells")]
    [SerializeField] private Cell[] cells = new Cell[9];

    [Header("Editor Layout")]
    [SerializeField] private bool autoLayoutInEditor = false;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float cellSpacing = 1.08f;
    [SerializeField] private Vector3 cellScale = Vector3.one;
    [SerializeField] private Vector3 cellVisualScale = Vector3.one;
    [SerializeField] private Vector3 ringParentLocalPosition = new Vector3(0f, 0.05f, 0f);

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoCellRadius = 0.45f;
    [SerializeField] private Color gizmoCellColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Color gizmoLineColor = Color.white;
    [SerializeField] private Color gizmoCenterColor = Color.yellow;

    private readonly int[,] matchLines =
    {
        {0, 1, 2},
        {3, 4, 5},
        {6, 7, 8},
        {0, 3, 6},
        {1, 4, 7},
        {2, 5, 8},
        {0, 4, 8},
        {2, 4, 6}
    };

    public Cell[] Cells
    {
        get { return cells; }
    }

    private void OnValidate()
    {
        EnsureCellsArray();

        if (autoLayoutInEditor)
        {
            ApplyBoardLayout();
        }
    }

    private void EnsureCellsArray()
    {
        if (cells == null || cells.Length != 9)
        {
            System.Array.Resize(ref cells, 9);
        }
    }

    [ContextMenu("Apply Board Layout")]
    public void ApplyBoardLayout()
    {
        EnsureCellsArray();

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            Vector3 localPosition = GetLocalPositionForIndex(i);

            cell.transform.localPosition = boardCenter + localPosition;
            cell.transform.localRotation = Quaternion.identity;
            cell.transform.localScale = cellScale;

            cell.ApplyEditorDesign(cellVisualScale, ringParentLocalPosition);
        }
    }

    [ContextMenu("Collect Cells From Children")]
    public void CollectCellsFromChildren()
    {
        EnsureCellsArray();

        Cell[] foundCells = GetComponentsInChildren<Cell>(true);

        for (int i = 0; i < foundCells.Length; i++)
        {
            Cell cell = foundCells[i];

            if (cell == null)
            {
                continue;
            }

            int index = ExtractIndex(cell.name);

            if (index >= 0 && index < 9)
            {
                cells[index] = cell;
            }
        }
    }

    public Cell GetCell(int index)
    {
        if (cells == null)
        {
            return null;
        }

        if (index < 0 || index >= cells.Length)
        {
            return null;
        }

        return cells[index];
    }

    public bool HasMove(RingPiece piece)
    {
        if (piece == null)
        {
            return false;
        }

        if (cells == null || cells.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            if (cell.CanPlace(piece))
            {
                return true;
            }
        }

        return false;
    }

    public List<MatchData> FindMatches()
    {
        List<MatchData> matches = new List<MatchData>();

        FindCellMatches(matches);
        FindLineMatches(matches);

        return matches;
    }

    private void FindCellMatches(List<MatchData> matches)
    {
        if (cells == null)
        {
            return;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            Ring outer = cell.GetRing(RingLayer.Outer);
            Ring middle = cell.GetRing(RingLayer.Middle);
            Ring inner = cell.GetRing(RingLayer.Inner);

            if (outer == null || middle == null || inner == null)
            {
                continue;
            }

            if (outer.ColorType == middle.ColorType && middle.ColorType == inner.ColorType)
            {
                matches.Add(new MatchData(outer, middle, inner, outer.ColorType));
            }
        }
    }

    private void FindLineMatches(List<MatchData> matches)
    {
        if (cells == null || cells.Length < 9)
        {
            return;
        }

        for (int i = 0; i < matchLines.GetLength(0); i++)
        {
            Cell cellA = cells[matchLines[i, 0]];
            Cell cellB = cells[matchLines[i, 1]];
            Cell cellC = cells[matchLines[i, 2]];

            TryFindLineMatchesForCells(cellA, cellB, cellC, matches);
        }
    }

    private void TryFindLineMatchesForCells(Cell cellA, Cell cellB, Cell cellC, List<MatchData> matches)
    {
        if (cellA == null || cellB == null || cellC == null || matches == null)
        {
            return;
        }

        int colorCount = System.Enum.GetValues(typeof(RingColorType)).Length;

        for (int colorIndex = 0; colorIndex < colorCount; colorIndex++)
        {
            RingColorType targetColor = (RingColorType)colorIndex;

            Ring ringA = GetFirstRingOfColor(cellA, targetColor);
            Ring ringB = GetFirstRingOfColor(cellB, targetColor);
            Ring ringC = GetFirstRingOfColor(cellC, targetColor);

            if (ringA != null && ringB != null && ringC != null)
            {
                matches.Add(new MatchData(ringA, ringB, ringC, targetColor));
            }
        }
    }

    private Ring GetFirstRingOfColor(Cell cell, RingColorType colorType)
    {
        if (cell == null)
        {
            return null;
        }

        Ring[] rings = cell.GetAllRings();

        for (int i = 0; i < rings.Length; i++)
        {
            Ring ring = rings[i];

            if (ring == null)
            {
                continue;
            }

            if (ring.ColorType == colorType)
            {
                return ring;
            }
        }

        return null;
    }

    public int ClearMatches(List<MatchData> matches)
    {
        if (matches == null || matches.Count == 0)
        {
            return 0;
        }

        HashSet<Ring> ringsToClear = new HashSet<Ring>();

        for (int i = 0; i < matches.Count; i++)
        {
            MatchData match = matches[i];

            if (match == null)
            {
                continue;
            }

            AddMatchedRingAndSameColorSiblings(match.RingA, match.ColorType, ringsToClear);
            AddMatchedRingAndSameColorSiblings(match.RingB, match.ColorType, ringsToClear);
            AddMatchedRingAndSameColorSiblings(match.RingC, match.ColorType, ringsToClear);
        }

        int clearedCount = 0;

        foreach (Ring ring in ringsToClear)
        {
            if (ring == null)
            {
                continue;
            }

            Cell ownerCell = FindCellContainingRing(ring);

            if (ownerCell != null)
            {
                ownerCell.RemoveRing(ring.Layer);
            }

            Destroy(ring.gameObject);
            clearedCount++;
        }

        return clearedCount;
    }

    private void AddMatchedRingAndSameColorSiblings(Ring sourceRing, RingColorType matchColor, HashSet<Ring> ringsToClear)
    {
        if (sourceRing == null || ringsToClear == null)
        {
            return;
        }

        Cell ownerCell = FindCellContainingRing(sourceRing);

        if (ownerCell == null)
        {
            ringsToClear.Add(sourceRing);
            return;
        }

        Ring[] ringsInCell = ownerCell.GetAllRings();

        for (int i = 0; i < ringsInCell.Length; i++)
        {
            Ring ring = ringsInCell[i];

            if (ring == null)
            {
                continue;
            }

            if (ring.ColorType == matchColor)
            {
                ringsToClear.Add(ring);
            }
        }
    }
    public void ClearBoard()
    {
        if (cells == null || cells.Length == 0)
        {
            return;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            cell.ClearAllRings();
        }
    }

    public bool IsBoardEmpty()
    {
        if (cells == null || cells.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            if (cell.HasRing(RingLayer.Outer))
            {
                return false;
            }

            if (cell.HasRing(RingLayer.Middle))
            {
                return false;
            }

            if (cell.HasRing(RingLayer.Inner))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryFindSingleMatchOpportunity(out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = RingColorType.Yellow;

        if (TryFindLineSingleOpportunity(out neededLayer, out color))
        {
            return true;
        }

        if (TryFindCellSingleOpportunity(out neededLayer, out color))
        {
            return true;
        }

        return false;
    }

    public bool TryFindComboOpportunity(out RingLayer firstLayer, out RingLayer secondLayer, out RingColorType color)
    {
        firstLayer = RingLayer.Outer;
        secondLayer = RingLayer.Middle;
        color = RingColorType.Yellow;

        if (cells == null || cells.Length < 9)
        {
            return false;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            if (!cell.HasRing(RingLayer.Outer) && !cell.HasRing(RingLayer.Middle))
            {
                firstLayer = RingLayer.Outer;
                secondLayer = RingLayer.Middle;
                color = GetMostCommonColorOnBoard();
                return true;
            }

            if (!cell.HasRing(RingLayer.Outer) && !cell.HasRing(RingLayer.Inner))
            {
                firstLayer = RingLayer.Outer;
                secondLayer = RingLayer.Inner;
                color = GetMostCommonColorOnBoard();
                return true;
            }

            if (!cell.HasRing(RingLayer.Middle) && !cell.HasRing(RingLayer.Inner))
            {
                firstLayer = RingLayer.Middle;
                secondLayer = RingLayer.Inner;
                color = GetMostCommonColorOnBoard();
                return true;
            }
        }

        return false;
    }

    private bool TryFindLineSingleOpportunity(out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = RingColorType.Yellow;

        if (cells == null || cells.Length < 9)
        {
            return false;
        }

        for (int line = 0; line < matchLines.GetLength(0); line++)
        {
            Cell cellA = cells[matchLines[line, 0]];
            Cell cellB = cells[matchLines[line, 1]];
            Cell cellC = cells[matchLines[line, 2]];

            if (TryFindLineOpportunity(cellA, cellB, cellC, out neededLayer, out color))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindLineOpportunity(Cell cellA, Cell cellB, Cell cellC, out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = RingColorType.Yellow;

        Cell[] lineCells = { cellA, cellB, cellC };

        int colorCount = System.Enum.GetValues(typeof(RingColorType)).Length;

        for (int colorIndex = 0; colorIndex < colorCount; colorIndex++)
        {
            RingColorType candidateColor = (RingColorType)colorIndex;

            int cellsWithColor = 0;
            Cell emptyCell = null;

            for (int i = 0; i < lineCells.Length; i++)
            {
                Cell cell = lineCells[i];

                if (cell == null)
                {
                    continue;
                }

                if (CellHasColor(cell, candidateColor))
                {
                    cellsWithColor++;
                }
                else
                {
                    emptyCell = cell;
                }
            }

            if (cellsWithColor != 2 || emptyCell == null)
            {
                continue;
            }

            if (TryGetAvailableLayerForCell(emptyCell, out neededLayer))
            {
                color = candidateColor;
                return true;
            }
        }

        return false;
    }

    private bool TryFindCellSingleOpportunity(out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = RingColorType.Yellow;

        if (cells == null)
        {
            return false;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            Ring outer = cell.GetRing(RingLayer.Outer);
            Ring middle = cell.GetRing(RingLayer.Middle);
            Ring inner = cell.GetRing(RingLayer.Inner);

            if (outer != null && middle != null && inner == null && outer.ColorType == middle.ColorType)
            {
                neededLayer = RingLayer.Inner;
                color = outer.ColorType;
                return true;
            }

            if (outer != null && inner != null && middle == null && outer.ColorType == inner.ColorType)
            {
                neededLayer = RingLayer.Middle;
                color = outer.ColorType;
                return true;
            }

            if (middle != null && inner != null && outer == null && middle.ColorType == inner.ColorType)
            {
                neededLayer = RingLayer.Outer;
                color = middle.ColorType;
                return true;
            }
        }

        return false;
    }

    private bool CellHasColor(Cell cell, RingColorType color)
    {
        if (cell == null)
        {
            return false;
        }

        Ring[] rings = cell.GetAllRings();

        for (int i = 0; i < rings.Length; i++)
        {
            Ring ring = rings[i];

            if (ring == null)
            {
                continue;
            }

            if (ring.ColorType == color)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetAvailableLayerForCell(Cell cell, out RingLayer layer)
    {
        layer = RingLayer.Outer;

        if (cell == null)
        {
            return false;
        }

        if (!cell.HasRing(RingLayer.Outer))
        {
            layer = RingLayer.Outer;
            return true;
        }

        if (!cell.HasRing(RingLayer.Middle))
        {
            layer = RingLayer.Middle;
            return true;
        }

        if (!cell.HasRing(RingLayer.Inner))
        {
            layer = RingLayer.Inner;
            return true;
        }

        return false;
    }

    private RingColorType GetMostCommonColorOnBoard()
    {
        Dictionary<RingColorType, int> colorCounts = new Dictionary<RingColorType, int>();

        if (cells != null)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                Cell cell = cells[i];

                if (cell == null)
                {
                    continue;
                }

                Ring[] rings = cell.GetAllRings();

                for (int r = 0; r < rings.Length; r++)
                {
                    Ring ring = rings[r];

                    if (ring == null)
                    {
                        continue;
                    }

                    if (!colorCounts.ContainsKey(ring.ColorType))
                    {
                        colorCounts.Add(ring.ColorType, 0);
                    }

                    colorCounts[ring.ColorType]++;
                }
            }
        }

        RingColorType bestColor = RingColorType.Yellow;
        int bestCount = -1;

        foreach (KeyValuePair<RingColorType, int> pair in colorCounts)
        {
            if (pair.Value > bestCount)
            {
                bestColor = pair.Key;
                bestCount = pair.Value;
            }
        }

        return bestColor;
    }

    private Cell FindCellContainingRing(Ring ring)
    {
        if (ring == null || cells == null)
        {
            return null;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            if (cell.GetRing(RingLayer.Outer) == ring)
            {
                return cell;
            }

            if (cell.GetRing(RingLayer.Middle) == ring)
            {
                return cell;
            }

            if (cell.GetRing(RingLayer.Inner) == ring)
            {
                return cell;
            }
        }

        return null;
    }

    private Vector3 GetLocalPositionForIndex(int index)
    {
        int row = index / 3;
        int column = index % 3;

        float x = (column - 1) * cellSpacing;
        float z = (1 - row) * cellSpacing;

        return new Vector3(x, 0f, z);
    }

    private int ExtractIndex(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return -1;
        }

        for (int i = 0; i <= 8; i++)
        {
            if (objectName.EndsWith("_" + i))
            {
                return i;
            }

            if (objectName.EndsWith(i.ToString()))
            {
                return i;
            }
        }

        return -1;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        EnsureCellsArray();

        Gizmos.color = gizmoCenterColor;
        Gizmos.DrawSphere(transform.TransformPoint(boardCenter), 0.07f);

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            Vector3 position;

            if (cell != null)
            {
                position = cell.transform.position;
            }
            else
            {
                position = transform.TransformPoint(boardCenter + GetLocalPositionForIndex(i));
            }

            Gizmos.color = gizmoCellColor;
            Gizmos.DrawSphere(position, gizmoCellRadius * 0.15f);
            Gizmos.DrawWireCube(position, new Vector3(gizmoCellRadius * 2f, 0.02f, gizmoCellRadius * 2f));
        }

        if (cells != null && cells.Length >= 9)
        {
            Gizmos.color = gizmoLineColor;

            for (int i = 0; i < matchLines.GetLength(0); i++)
            {
                Cell a = cells[matchLines[i, 0]];
                Cell c = cells[matchLines[i, 2]];

                if (a != null && c != null)
                {
                    Gizmos.DrawLine(a.transform.position, c.transform.position);
                }
            }
        }
    }
}