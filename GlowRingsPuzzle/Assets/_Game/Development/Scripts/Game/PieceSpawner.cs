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
    [SerializeField] private float allClearAssistChance = 0.38f;

    [SerializeField] private int allClearMaxRingCount = 6;
    [SerializeField] private int allClearCooldownSpawnCount = 5;

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

    public RingPiece CurrentPiece
    {
        get { return currentPiece; }
    }

    public void SpawnNextPiece()
    {
        ClearCurrentPiece();

        if (!CanSpawn())
        {
            return;
        }

        RingPieceData selectedData = SelectPieceData(out RingColorType selectedColor);

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
    }

    public RingPiece SpawnForcedPiece(RingPieceData pieceData, RingColorType colorType)
    {
        ClearCurrentPiece();

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
        selectedColor = GetRandomThemeColor();
        lastSpawnType = SpawnType.Random;

        BoardManager boardManager = FindObjectOfType<BoardManager>();

        if (boardManager == null)
        {
            return GetBoardAwareRandomPieceData(0f);
        }

        float fillRatio = GetBoardFillRatio(boardManager);
        int totalRingCount = GetTotalRingCount(boardManager);

        if (allClearAssistCooldown > 0)
        {
            allClearAssistCooldown--;
        }

        if (enableAllClearAssist &&
            allClearAssistCooldown <= 0 &&
            totalRingCount > 0 &&
            totalRingCount <= allClearMaxRingCount &&
            Random.value <= GetDynamicAllClearChance(totalRingCount))
        {
            if (TryGetAllClearAssistPiece(boardManager, out RingPieceData allClearData, out RingColorType allClearColor))
            {
                selectedColor = allClearColor;
                lastSpawnType = SpawnType.AllClearAssist;
                allClearAssistCooldown = Mathf.Max(0, allClearCooldownSpawnCount);
                return allClearData;
            }
        }

        float dynamicHelpfulChance = GetDynamicHelpfulChance(fillRatio);

        if (tryHelpfulSpawn && Random.value <= dynamicHelpfulChance)
        {
            if (TryGetHelpfulPiece(boardManager, out RingPieceData helpfulData, out RingColorType helpfulColor))
            {
                selectedColor = helpfulColor;
                lastSpawnType = SpawnType.Helpful;
                return helpfulData;
            }
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
        if (fillRatio < 0.30f)
        {
            return 0.18f;
        }

        if (fillRatio < 0.55f)
        {
            return helpfulChance;
        }

        if (fillRatio < 0.75f)
        {
            return 0.45f;
        }

        if (fillRatio < 0.88f)
        {
            return 0.58f;
        }

        return 0.70f;
    }

    private float GetDynamicAllClearChance(int totalRingCount)
    {
        if (totalRingCount <= 3)
        {
            return Mathf.Clamp01(allClearAssistChance + 0.22f);
        }

        if (totalRingCount <= 6)
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
        else if (boardFillRatio < 0.35f)
        {
            singleWeight = 62;
            doubleWeight = 34;
            tripleWeight = 4;
        }
        else if (boardFillRatio < 0.70f)
        {
            singleWeight = 76;
            doubleWeight = 22;
            tripleWeight = 2;
        }
        else if (boardFillRatio < 0.82f)
        {
            singleWeight = 88;
            doubleWeight = 12;
            tripleWeight = 0;
        }
        else
        {
            singleWeight = 95;
            doubleWeight = 5;
            tripleWeight = 0;
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

        if (boardManager == null || boardManager.Cells == null)
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

    private bool TryGetHelpfulPiece(BoardManager boardManager, out RingPieceData data, out RingColorType color)
    {
        data = null;
        color = GetRandomThemeColor();

        if (boardManager == null)
        {
            return false;
        }

        if (TryFindCellMatchOpportunity(boardManager, out RingLayer cellLayer, out color))
        {
            data = GetPieceBySingleLayer(cellLayer);
            return data != null;
        }

        if (boardManager.TryFindSingleMatchOpportunity(out RingLayer layer, out color))
        {
            data = GetPieceBySingleLayer(layer);
            return data != null;
        }

        if (boardManager.TryFindComboOpportunity(out RingLayer firstLayer, out RingLayer secondLayer, out color))
        {
            bool hasOuter = firstLayer == RingLayer.Outer || secondLayer == RingLayer.Outer;
            bool hasMiddle = firstLayer == RingLayer.Middle || secondLayer == RingLayer.Middle;
            bool hasInner = firstLayer == RingLayer.Inner || secondLayer == RingLayer.Inner;

            data = GetPieceByLayers(hasOuter, hasMiddle, hasInner);
            return data != null;
        }

        return false;
    }

    private bool TryFindCellMatchOpportunity(BoardManager boardManager, out RingLayer neededLayer, out RingColorType color)
    {
        neededLayer = RingLayer.Outer;
        color = GetRandomThemeColor();

        if (boardManager == null || boardManager.Cells == null)
        {
            return false;
        }

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