using System.Collections.Generic;
using UnityEngine;

public enum SpawnType
{
    Random,
    Helpful,
    SetupCombo,
    AllClearAssist
}

public class PieceSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RingPiece piecePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Piece Data")]
    [SerializeField] private List<RingPieceData> pieceDataList = new List<RingPieceData>();

    [Header("Board Aware Balance")]
    [SerializeField] private bool useBoardAwareWeights = true;
    [SerializeField] private bool tryHelpfulSpawn = true;

    [Range(0f, 1f)]
    [SerializeField] private float helpfulChance = 0.32f;

    [Header("All Clear Assist")]
    [SerializeField] private bool enableAllClearAssist = true;

    [Range(0f, 1f)]
    [SerializeField] private float allClearAssistChance = 0.10f;

    [SerializeField] private int allClearMaxRingCount = 6;
    [SerializeField] private int allClearCooldownSpawnCount = 10;

    [Header("Fallback Weights")]
    [SerializeField] private int singleLayerWeight = 78;
    [SerializeField] private int doubleLayerWeight = 20;
    [SerializeField] private int tripleLayerWeight = 2;

    [Header("Debug")]
    [SerializeField] private SpawnType lastSpawnType = SpawnType.Random;
    [SerializeField] private RingColorType lastSpawnColor;
    [SerializeField] private RingPieceData lastSpawnData;

    private RingPiece currentPiece;
    private int allClearAssistCooldown;
    private int assistCooldown;
    private int movesWithoutMatch;
    private RingPieceData nextPieceData;
    private RingColorType nextPieceColor;
    private SpawnType nextPieceType;

    public RingPieceData NextPieceData => nextPieceData;
    public RingColorType NextPieceColor => nextPieceColor;
    public RingPiece PiecePrefab => piecePrefab;
    public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
    public Transform SpawnVisual => spawnPoint != null ? spawnPoint.Find("Visual") : null;

    [Header("Match Rhythm")]
    [Min(0)] [SerializeField] private int assistSpacing = 2;
    [Min(1)] [SerializeField] private int droughtStartMoves = 4;
    [Range(0f, 1f)] [SerializeField] private float setupChance = 0.24f;

    public void ResetBalance()
    {
        allClearAssistCooldown = 0;
        assistCooldown = 0;
        movesWithoutMatch = 0;
        nextPieceData = null;
    }

    public void ReportMoveResult(bool matched)
    {
        movesWithoutMatch = matched ? 0 : Mathf.Min(movesWithoutMatch + 1, 20);
    }

    public RingPiece CurrentPiece
    {
        get { return currentPiece; }
    }

    public SpawnType LastSpawnType
    {
        get { return lastSpawnType; }
    }

    public RingColorType LastSpawnColor
    {
        get { return lastSpawnColor; }
    }

    public RingPieceData LastSpawnData
    {
        get { return lastSpawnData; }
    }

    public void SpawnNextPiece()
    {
        ClearCurrentPiece();

        if (!CanSpawn())
        {
            return;
        }

        RingPieceData selectedData;
        RingColorType selectedColor;
        if (nextPieceData != null)
        {
            selectedData = nextPieceData;
            selectedColor = nextPieceColor;
            lastSpawnType = nextPieceType;
            nextPieceData = null;
        }
        else
        {
            selectedData = SelectPieceData(out selectedColor);
        }

        if (selectedData == null)
        {
            Debug.LogError("PieceSpawner: Uygun RingPieceData bulunamadı.");
            return;
        }

        currentPiece = Instantiate(piecePrefab, spawnPoint.position, Quaternion.identity);
        currentPiece.SetStartPosition(spawnPoint.position);

        bool initialized = currentPiece.Initialize(selectedData, ThemeManager.Instance.CurrentTheme, selectedColor);

        if (!initialized)
        {
            Debug.LogError("PieceSpawner: Current piece initialize edilemedi.");
            Destroy(currentPiece.gameObject);
            currentPiece = null;
            return;
        }

        lastSpawnData = selectedData;
        lastSpawnColor = selectedColor;
        QueueNextPiece();
    }

    private void QueueNextPiece()
    {
        // Generate once: the player can trust the visible preview, even after a bomb or discard.
        SpawnType currentType = lastSpawnType;
        nextPieceData = SelectPieceData(out nextPieceColor);
        nextPieceType = lastSpawnType;
        lastSpawnType = currentType;
    }

    public RingPiece SpawnForcedPiece(RingPieceData pieceData, RingColorType colorType)
    {
        ClearCurrentPiece();
        nextPieceData = null;

        if (pieceData == null)
        {
            Debug.LogWarning("PieceSpawner: Forced spawn için PieceData null.");
            return null;
        }

        if (!CanSpawn())
        {
            return null;
        }

        currentPiece = Instantiate(piecePrefab, spawnPoint.position, Quaternion.identity);
        currentPiece.SetStartPosition(spawnPoint.position);

        bool initialized = currentPiece.Initialize(pieceData, ThemeManager.Instance.CurrentTheme, colorType);

        if (!initialized)
        {
            Debug.LogError("PieceSpawner: Forced piece initialize edilemedi.");
            Destroy(currentPiece.gameObject);
            currentPiece = null;
            return null;
        }

        lastSpawnType = SpawnType.Random;
        lastSpawnData = pieceData;
        lastSpawnColor = colorType;

        return currentPiece;
    }

    public void ClearCurrentPiece()
    {
        if (currentPiece == null)
        {
            return;
        }

        Destroy(currentPiece.gameObject);
        currentPiece = null;
    }

    public void ForgetCurrentPiece()
    {
        currentPiece = null;
    }

    private bool CanSpawn()
    {
        if (piecePrefab == null)
        {
            Debug.LogError("PieceSpawner: Piece Prefab atanmadı.");
            return false;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("PieceSpawner: Spawn Point atanmadı.");
            return false;
        }

        if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
        {
            Debug.LogError("PieceSpawner: ThemeManager veya CurrentTheme eksik.");
            return false;
        }

        if (pieceDataList == null || pieceDataList.Count == 0)
        {
            Debug.LogError("PieceSpawner: Piece Data List boş.");
            return false;
        }

        return true;
    }

    private RingPieceData SelectPieceData(out RingColorType selectedColor)
    {
        return SelectPieceForBoard(FindObjectOfType<BoardManager>(), out selectedColor);
    }

    private RingPieceData SelectPieceForBoard(BoardManager boardManager, out RingColorType selectedColor)
    {
        selectedColor = GetRandomThemeColor();
        lastSpawnType = SpawnType.Random;

        if (boardManager == null)
        {
            return GetBoardAwareRandomPieceData(0f);
        }

        float fillRatio = GetBoardFillRatio(boardManager);
        int totalRingCount = GetTotalRingCount(boardManager);

        bool canAssistAllClear = allClearAssistCooldown <= 0;
        if (allClearAssistCooldown > 0)
        {
            allClearAssistCooldown--;
        }

        bool canAssist = assistCooldown <= 0;
        if (assistCooldown > 0)
        {
            assistCooldown--;
        }

        if (canAssist && enableAllClearAssist &&
            canAssistAllClear &&
            totalRingCount > 0 &&
            totalRingCount <= allClearMaxRingCount &&
            Random.value <= GetDynamicAllClearChance(totalRingCount))
        {
            if (TryGetAllClearAssistPiece(boardManager, out RingPieceData allClearData, out RingColorType allClearColor))
            {
                selectedColor = allClearColor;
                lastSpawnType = SpawnType.AllClearAssist;
                allClearAssistCooldown = Mathf.Max(0, allClearCooldownSpawnCount);
                assistCooldown = Mathf.Max(0, assistSpacing);
                return allClearData;
            }
        }

        float dynamicHelpfulChance = GetDynamicHelpfulChance(fillRatio);

        if (canAssist && tryHelpfulSpawn && Random.value < dynamicHelpfulChance)
        {
            if (TryGetEvaluatedPiece(boardManager, false, out RingPieceData helpfulData, out RingColorType helpfulColor))
            {
                selectedColor = helpfulColor;
                lastSpawnType = SpawnType.Helpful;
                assistCooldown = Mathf.Max(0, assistSpacing);
                return helpfulData;
            }
        }

        if (canAssist && tryHelpfulSpawn && Random.value < setupChance &&
            TryGetEvaluatedPiece(boardManager, true, out RingPieceData setupData, out RingColorType setupColor))
        {
            selectedColor = setupColor;
            lastSpawnType = SpawnType.SetupCombo;
            assistCooldown = Mathf.Max(0, assistSpacing);
            return setupData;
        }

        RingPieceData weightedData = GetBoardAwareRandomPieceData(fillRatio);

        if (weightedData != null)
        {
            selectedColor = GetRandomThemeColor();
            lastSpawnType = SpawnType.Random;
            return weightedData;
        }

        selectedColor = GetRandomThemeColor();
        lastSpawnType = SpawnType.Random;
        return GetRandomPieceData();
    }

    private float GetDynamicHelpfulChance(float fillRatio)
    {
        float pressure = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.85f, fillRatio));
        float drought = Mathf.Clamp01((movesWithoutMatch - droughtStartMoves + 1) / 4f);
        return Mathf.Clamp(Mathf.Lerp(helpfulChance * 0.65f, helpfulChance + 0.06f, pressure)
            + drought * 0.18f, 0f, 0.50f);
    }

    private float GetDynamicAllClearChance(int totalRingCount)
    {
        if (totalRingCount <= 3)
        {
            return Mathf.Clamp01(allClearAssistChance);
        }

        if (totalRingCount <= allClearMaxRingCount)
        {
            return Mathf.Clamp01(allClearAssistChance);
        }

        return 0f;
    }

    private RingPieceData GetBoardAwareRandomPieceData(float boardFillRatio)
    {
        int singleWeight;
        int doubleWeight;
        int tripleWeight;

        if (!useBoardAwareWeights)
        {
            singleWeight = singleLayerWeight;
            doubleWeight = doubleLayerWeight;
            tripleWeight = tripleLayerWeight;
        }
        else
        {
            float pressure = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(boardFillRatio));
            singleWeight = Mathf.RoundToInt(Mathf.Lerp(62f, 78f, pressure));
            doubleWeight = Mathf.RoundToInt(Mathf.Lerp(34f, 20f, pressure));
            tripleWeight = 100 - singleWeight - doubleWeight;
        }

        int layerCount = RollLayerCount(singleWeight, doubleWeight, tripleWeight);
        RingPieceData data = GetRandomPieceDataByLayerCount(layerCount);

        if (data != null)
        {
            return data;
        }

        data = GetRandomPieceDataByLayerCount(1);

        if (data != null)
        {
            return data;
        }

        return GetRandomPieceData();
    }

    private int RollLayerCount(int singleWeight, int doubleWeight, int tripleWeight)
    {
        singleWeight = Mathf.Max(0, singleWeight);
        doubleWeight = Mathf.Max(0, doubleWeight);
        tripleWeight = Mathf.Max(0, tripleWeight);

        int totalWeight = singleWeight + doubleWeight + tripleWeight;

        if (totalWeight <= 0)
        {
            return 1;
        }

        int roll = Random.Range(0, totalWeight);

        if (roll < singleWeight)
        {
            return 1;
        }

        roll -= singleWeight;

        if (roll < doubleWeight)
        {
            return 2;
        }

        return 3;
    }

    private bool TryGetAllClearAssistPiece(BoardManager boardManager, out RingPieceData data, out RingColorType color)
    {
        data = null;
        color = GetRandomThemeColor();

        if (boardManager == null || boardManager.Cells == null || boardManager.Cells.Length != 9)
        {
            return false;
        }

        if (TryFindAllClearCellOpportunity(boardManager, out RingLayer cellLayer, out color))
        {
            data = GetPieceBySingleLayer(cellLayer);
            return data != null;
        }

        if (TryFindAllClearLineOpportunity(boardManager, out RingLayer lineLayer, out color))
        {
            data = GetPieceBySingleLayer(lineLayer);
            return data != null;
        }

        return false;
    }

    private bool TryFindAllClearCellOpportunity(BoardManager boardManager, out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = GetRandomThemeColor();

        Cell[] cells = boardManager.Cells;

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

            int ringCount = 0;

            if (outer != null)
            {
                ringCount++;
            }

            if (middle != null)
            {
                ringCount++;
            }

            if (inner != null)
            {
                ringCount++;
            }

            if (ringCount != 2)
            {
                continue;
            }

            RingColorType candidateColor;

            if (outer != null && middle != null && outer.ColorType == middle.ColorType && inner == null)
            {
                candidateColor = outer.ColorType;

                if (WouldBoardBeEmptyAfterCellMatch(boardManager, cell, candidateColor))
                {
                    neededLayer = RingLayer.Inner;
                    color = candidateColor;
                    return true;
                }
            }

            if (outer != null && inner != null && outer.ColorType == inner.ColorType && middle == null)
            {
                candidateColor = outer.ColorType;

                if (WouldBoardBeEmptyAfterCellMatch(boardManager, cell, candidateColor))
                {
                    neededLayer = RingLayer.Middle;
                    color = candidateColor;
                    return true;
                }
            }

            if (middle != null && inner != null && middle.ColorType == inner.ColorType && outer == null)
            {
                candidateColor = middle.ColorType;

                if (WouldBoardBeEmptyAfterCellMatch(boardManager, cell, candidateColor))
                {
                    neededLayer = RingLayer.Outer;
                    color = candidateColor;
                    return true;
                }
            }
        }

        return false;
    }

    private bool WouldBoardBeEmptyAfterCellMatch(BoardManager boardManager, Cell targetCell, RingColorType matchColor)
    {
        Cell[] cells = boardManager.Cells;

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

                if (cell == targetCell && ring.ColorType == matchColor)
                {
                    continue;
                }

                return false;
            }
        }

        return true;
    }

    private bool TryFindAllClearLineOpportunity(BoardManager boardManager, out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = GetRandomThemeColor();

        int[,] lines =
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

        Cell[] cells = boardManager.Cells;

        for (int i = 0; i < lines.GetLength(0); i++)
        {
            Cell cellA = cells[lines[i, 0]];
            Cell cellB = cells[lines[i, 1]];
            Cell cellC = cells[lines[i, 2]];

            if (TryFindLineOpportunityInCells(boardManager, cellA, cellB, cellC, out neededLayer, out color))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindLineOpportunityInCells(BoardManager boardManager, Cell cellA, Cell cellB, Cell cellC, out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = GetRandomThemeColor();

        Cell[] lineCells = { cellA, cellB, cellC };

        int colorCount = System.Enum.GetValues(typeof(RingColorType)).Length;

        for (int colorIndex = 0; colorIndex < colorCount; colorIndex++)
        {
            RingColorType candidateColor = (RingColorType)colorIndex;

            int cellsWithColor = 0;
            int emptyCandidateCellIndex = -1;

            for (int i = 0; i < lineCells.Length; i++)
            {
                Cell cell = lineCells[i];

                if (cell == null)
                {
                    continue;
                }

                bool hasColor = CellHasColor(cell, candidateColor);

                if (hasColor)
                {
                    cellsWithColor++;
                }
                else
                {
                    emptyCandidateCellIndex = i;
                }
            }

            if (cellsWithColor != 2 || emptyCandidateCellIndex < 0)
            {
                continue;
            }

            Cell targetCell = lineCells[emptyCandidateCellIndex];

            if (targetCell == null)
            {
                continue;
            }

            if (!TryGetAvailableLayerForCell(targetCell, out RingLayer availableLayer))
            {
                continue;
            }

            if (WouldBoardBeEmptyAfterLineMatch(boardManager, cellA, cellB, cellC, candidateColor))
            {
                neededLayer = availableLayer;
                color = candidateColor;
                return true;
            }
        }

        return false;
    }

    private bool WouldBoardBeEmptyAfterLineMatch(BoardManager boardManager, Cell cellA, Cell cellB, Cell cellC, RingColorType matchColor)
    {
        Cell[] cells = boardManager.Cells;

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

                bool isLineCell = cell == cellA || cell == cellB || cell == cellC;

                if (isLineCell && ring.ColorType == matchColor)
                {
                    continue;
                }

                return false;
            }
        }

        return true;
    }

    private bool TryGetEvaluatedPiece(BoardManager boardManager, bool setupOnly, out RingPieceData data, out RingColorType color)
    {
        data = null;
        color = lastSpawnColor;
        if (boardManager.Cells == null || boardManager.Cells.Length != 9 ||
            ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
            return false;

        int[,] board = new int[9, 3];
        int occupied = 0;
        for (int cell = 0; cell < 9; cell++)
        {
            for (int layer = 0; layer < 3; layer++)
            {
                Cell source = boardManager.Cells[cell];
                Ring ring = source != null ? source.GetRing((RingLayer)layer) : null;
                board[cell, layer] = source == null ? -2 : ring != null ? (int)ring.ColorType : -1;
                if (ring != null) occupied++;
            }
        }

        // Weighted reservoir sampling keeps all shapes/colors in contention, rather
        // than always choosing the first cell, first free layer or first enum color.
        float totalWeight = 0f;
        HashSet<RingColorType> colors = new HashSet<RingColorType>();
        foreach (RingColorData colorData in ThemeManager.Instance.CurrentTheme.RingColors)
        {
            if (colorData == null || !colors.Add(colorData.ColorType)) continue;
            foreach (RingPieceData candidate in pieceDataList)
            {
                if (candidate == null || !candidate.HasAnyLayer) continue;
                int mask = (candidate.HasOuter ? 1 : 0) | (candidate.HasMiddle ? 2 : 0) | (candidate.HasInner ? 4 : 0);
                float bestWeight = 0f;
                int usefulPlacements = 0;
                for (int cell = 0; cell < 9; cell++)
                {
                    if (!SpawnBoardEvaluation.CanPlace(board, cell, mask)) continue;
                    SpawnBoardEvaluation.Evaluate(board, cell, mask, (int)colorData.ColorType,
                        out int matches, out int cleared, out int setups);
                    // Reserve deliberately supplied all-clears for their own chance/cooldown.
                    bool allClear = cleared == occupied + candidate.GetLayerCount();
                    if (allClear) continue;
                    if (matches > 0 && cleared <= candidate.GetLayerCount()) continue;
                    float weight = setupOnly
                        ? (matches == 0 && setups > 0 ? 1f + Mathf.Min(setups, 3) * 0.3f : 0f)
                        : (matches > 0 ? 1f + Mathf.Min(matches - 1, 2) * 0.6f : 0f);
                    if (weight > 0f) usefulPlacements++;
                    bestWeight = Mathf.Max(bestWeight, weight);
                }
                if (bestWeight <= 0f) continue;
                // Favor pieces that offer a choice of productive destinations.
                bestWeight *= 1f + Mathf.Min(usefulPlacements - 1, 3) * 0.4f;
                totalWeight += bestWeight;
                if (Random.value * totalWeight < bestWeight)
                {
                    data = candidate;
                    color = colorData.ColorType;
                }
            }
        }
        return data != null;
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

    private float GetBoardFillRatio(BoardManager boardManager)
    {
        if (boardManager == null || boardManager.Cells == null)
        {
            return 0f;
        }

        Cell[] cells = boardManager.Cells;

        if (cells.Length == 0)
        {
            return 0f;
        }

        int filledLayerCount = 0;
        int totalLayerCount = cells.Length * 3;

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            if (cell.HasRing(RingLayer.Outer))
            {
                filledLayerCount++;
            }

            if (cell.HasRing(RingLayer.Middle))
            {
                filledLayerCount++;
            }

            if (cell.HasRing(RingLayer.Inner))
            {
                filledLayerCount++;
            }
        }

        if (totalLayerCount <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)filledLayerCount / totalLayerCount);
    }

    private int GetTotalRingCount(BoardManager boardManager)
    {
        if (boardManager == null || boardManager.Cells == null)
        {
            return 0;
        }

        int count = 0;
        Cell[] cells = boardManager.Cells;

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            if (cell.HasRing(RingLayer.Outer))
            {
                count++;
            }

            if (cell.HasRing(RingLayer.Middle))
            {
                count++;
            }

            if (cell.HasRing(RingLayer.Inner))
            {
                count++;
            }
        }

        return count;
    }

    private RingPieceData GetRandomPieceDataByLayerCount(int layerCount)
    {
        if (pieceDataList == null || pieceDataList.Count == 0)
        {
            return null;
        }

        List<RingPieceData> validList = new List<RingPieceData>();

        for (int i = 0; i < pieceDataList.Count; i++)
        {
            RingPieceData data = pieceDataList[i];

            if (data == null)
            {
                continue;
            }

            if (data.GetLayerCount() == layerCount)
            {
                validList.Add(data);
            }
        }

        if (validList.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, validList.Count);
        return validList[randomIndex];
    }

    private RingPieceData GetRandomPieceData()
    {
        if (pieceDataList == null || pieceDataList.Count == 0)
        {
            return null;
        }

        List<RingPieceData> validList = new List<RingPieceData>();

        for (int i = 0; i < pieceDataList.Count; i++)
        {
            RingPieceData data = pieceDataList[i];

            if (data == null)
            {
                continue;
            }

            if (!data.HasAnyLayer)
            {
                continue;
            }

            validList.Add(data);
        }

        if (validList.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, validList.Count);
        return validList[randomIndex];
    }

    private RingPieceData GetPieceBySingleLayer(RingLayer layer)
    {
        if (layer == RingLayer.Outer)
        {
            return GetPieceByLayers(true, false, false);
        }

        if (layer == RingLayer.Middle)
        {
            return GetPieceByLayers(false, true, false);
        }

        if (layer == RingLayer.Inner)
        {
            return GetPieceByLayers(false, false, true);
        }

        return null;
    }

    private RingPieceData GetPieceByLayers(bool hasOuter, bool hasMiddle, bool hasInner)
    {
        if (pieceDataList == null || pieceDataList.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < pieceDataList.Count; i++)
        {
            RingPieceData data = pieceDataList[i];

            if (data == null)
            {
                continue;
            }

            if (data.HasOuter == hasOuter &&
                data.HasMiddle == hasMiddle &&
                data.HasInner == hasInner)
            {
                return data;
            }
        }

        return null;
    }

    private RingColorType GetRandomThemeColor()
    {
        if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
        {
            RingColorData randomColorData = ThemeManager.Instance.CurrentTheme.GetRandomColor();

            if (randomColorData != null)
            {
                return randomColorData.ColorType;
            }
        }

        int colorCount = System.Enum.GetValues(typeof(RingColorType)).Length;
        int randomIndex = Random.Range(0, colorCount);

        return (RingColorType)randomIndex;
    }
}
