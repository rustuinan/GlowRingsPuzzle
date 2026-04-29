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

    public RingPiece CurrentPiece => currentPiece;

    public void SpawnNextPiece()
    {
        ClearCurrentPiece();

        if (piecePrefab == null)
        {
            Debug.LogError("PieceSpawner: Piece Prefab atanmadı.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("PieceSpawner: Spawn Point atanmadı.");
            return;
        }

        if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
        {
            Debug.LogError("PieceSpawner: ThemeManager veya CurrentTheme eksik.");
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

    public void ClearCurrentPiece()
    {
        if (currentPiece == null)
            return;

        Destroy(currentPiece.gameObject);
        currentPiece = null;
    }

    public void ForgetCurrentPiece()
    {
        currentPiece = null;
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
            return weightedData;

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
            return data;

        data = GetRandomPieceDataByLayerCount(1);

        if (data != null)
            return data;

        return GetRandomPieceData();
    }

    private int RollLayerCount(int singleWeight, int doubleWeight, int tripleWeight)
    {
        singleWeight = Mathf.Max(0, singleWeight);
        doubleWeight = Mathf.Max(0, doubleWeight);
        tripleWeight = Mathf.Max(0, tripleWeight);

        int totalWeight = singleWeight + doubleWeight + tripleWeight;

        if (totalWeight <= 0)
            return 1;

        int roll = Random.Range(0, totalWeight);

        if (roll < singleWeight)
            return 1;

        roll -= singleWeight;

        if (roll < doubleWeight)
            return 2;

        return 3;
    }

    private bool TryGetHelpfulPiece(BoardManager boardManager, out RingPieceData data, out RingColorType color)
    {
        data = null;
        color = GetRandomThemeColor();

        if (boardManager == null)
            return false;

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
            return 0f;

        int filled = 0;
        int total = 0;

        Cell[] cells = boardManager.Cells;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
                continue;

            total += 3;

            if (cells[i].GetRing(RingLayer.Outer) != null) filled++;
            if (cells[i].GetRing(RingLayer.Middle) != null) filled++;
            if (cells[i].GetRing(RingLayer.Inner) != null) filled++;
        }

        if (total <= 0)
            return 0f;

        return (float)filled / total;
    }

    private RingPieceData GetRandomPieceDataByLayerCount(int layerCount)
    {
        List<RingPieceData> candidates = new List<RingPieceData>();

        for (int i = 0; i < pieceDataList.Count; i++)
        {
            RingPieceData data = pieceDataList[i];

            if (data == null || !data.HasAnyLayer)
                continue;

            if (GetLayerCount(data) == layerCount)
                candidates.Add(data);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private RingPieceData GetRandomPieceData()
    {
        List<RingPieceData> validPieces = new List<RingPieceData>();

        for (int i = 0; i < pieceDataList.Count; i++)
        {
            if (pieceDataList[i] != null && pieceDataList[i].HasAnyLayer)
                validPieces.Add(pieceDataList[i]);
        }

        if (validPieces.Count == 0)
            return null;

        return validPieces[Random.Range(0, validPieces.Count)];
    }

    private RingPieceData GetPieceByLayers(bool hasOuter, bool hasMiddle, bool hasInner)
    {
        for (int i = 0; i < pieceDataList.Count; i++)
        {
            RingPieceData data = pieceDataList[i];

            if (data == null)
                continue;

            if (data.HasOuter == hasOuter &&
                data.HasMiddle == hasMiddle &&
                data.HasInner == hasInner)
            {
                return data;
            }
        }

        return null;
    }

    private int GetLayerCount(RingPieceData data)
    {
        int count = 0;

        if (data.HasOuter) count++;
        if (data.HasMiddle) count++;
        if (data.HasInner) count++;

        return count;
    }

    private RingColorType GetRandomThemeColor()
    {
        if (ThemeManager.Instance == null)
            return RingColorType.Red;

        ThemeData theme = ThemeManager.Instance.CurrentTheme;

        if (theme == null)
            return RingColorType.Red;

        RingColorData colorData = theme.GetRandomColor();

        if (colorData == null)
            return RingColorType.Red;

        return colorData.ColorType;
    }
}