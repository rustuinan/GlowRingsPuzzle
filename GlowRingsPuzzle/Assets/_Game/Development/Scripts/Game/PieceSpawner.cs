using System.Collections.Generic;
using UnityEngine;

public enum SpawnType
{
    Random,
    Helpful,
    SetupCombo,
    Punish
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
    [SerializeField] private float helpfulChance = 0.35f;

    [Header("Fallback Weights")]
    [SerializeField] private int singleLayerWeight = 75;
    [SerializeField] private int doubleLayerWeight = 22;
    [SerializeField] private int tripleLayerWeight = 3;

    private RingPiece currentPiece;

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
        currentPiece.Initialize(selectedData, ThemeManager.Instance.CurrentTheme, selectedColor);
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
        currentPiece.Initialize(pieceData, ThemeManager.Instance.CurrentTheme, colorType);

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

        BoardManager boardManager = FindObjectOfType<BoardManager>();
        float fillRatio = boardManager != null ? GetBoardFillRatio(boardManager) : 0f;

        float dynamicHelpfulChance = helpfulChance;

        if (fillRatio < 0.30f)
        {
            dynamicHelpfulChance = 0.15f;
        }
        else if (fillRatio < 0.70f)
        {
            dynamicHelpfulChance = helpfulChance;
        }
        else if (fillRatio < 0.80f)
        {
            dynamicHelpfulChance = 0.50f;
        }
        else
        {
            dynamicHelpfulChance = 0.65f;
        }

        if (tryHelpfulSpawn && Random.value <= dynamicHelpfulChance)
        {
            if (TryGetHelpfulPiece(boardManager, out RingPieceData helpfulData, out RingColorType helpfulColor))
            {
                selectedColor = helpfulColor;
                return helpfulData;
            }
        }

        RingPieceData weightedData = GetBoardAwareRandomPieceData(fillRatio);

        if (weightedData != null)
        {
            return weightedData;
        }

        return GetRandomPieceData();
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
            singleWeight = 55;
            doubleWeight = 35;
            tripleWeight = 10;
        }
        else if (boardFillRatio < 0.70f)
        {
            singleWeight = 70;
            doubleWeight = 25;
            tripleWeight = 5;
        }
        else if (boardFillRatio < 0.80f)
        {
            singleWeight = 85;
            doubleWeight = 15;
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

    private bool TryGetHelpfulPiece(BoardManager boardManager, out RingPieceData data, out RingColorType color)
    {
        data = null;
        color = GetRandomThemeColor();

        if (boardManager == null)
        {
            return false;
        }

        if (boardManager.TryFindSingleMatchOpportunity(out RingLayer layer, out color))
        {
            data = GetPieceByLayers(
                layer == RingLayer.Outer,
                layer == RingLayer.Middle,
                layer == RingLayer.Inner
            );

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