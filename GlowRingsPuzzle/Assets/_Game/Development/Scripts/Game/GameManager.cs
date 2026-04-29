using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PieceSpawner pieceSpawner;

    [SerializeField] private int pointsPerClearedRing = 10;
    [SerializeField] private int multiMatchBonus = 100;
    [SerializeField] private int allClearBonus = 500;

    private int comboCount;
    private bool gameOver;

    public bool IsGameOver => gameOver;
    public int ComboCount => comboCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();

        if (pieceSpawner == null)
            pieceSpawner = FindObjectOfType<PieceSpawner>();

        gameOver = false;
        comboCount = 0;

        ScoreManager.Instance.ResetScore();
        ThemeManager.Instance.UpdateThemeByScore(ScoreManager.Instance.Score);

        pieceSpawner.SpawnNextPiece();
        CheckGameOver();
    }

    public void OnPiecePlaced()
    {
        if (gameOver)
            return;

        ResolveMatches();

        ThemeManager.Instance.UpdateThemeByScore(ScoreManager.Instance.Score);

        pieceSpawner.SpawnNextPiece();
        CheckGameOver();
    }

    public void TrashCurrentPiece()
    {
        if (gameOver)
            return;

        pieceSpawner.ClearCurrentPiece();
        pieceSpawner.SpawnNextPiece();

        CheckGameOver();
    }

    public void DiscardCurrentPiece()
    {
        TrashCurrentPiece();
    }

    private void ResolveMatches()
    {
        List<MatchData> matches = boardManager.FindMatches();

        if (matches.Count == 0)
        {
            comboCount = 0;
            return;
        }

        comboCount++;

        int clearedRingCount = boardManager.ClearMatches(matches);

        int baseScore = clearedRingCount * pointsPerClearedRing;
        int comboScore = baseScore * comboCount;
        int bonusScore = 0;

        if (matches.Count >= 2)
            bonusScore += multiMatchBonus * matches.Count;

        if (boardManager.IsBoardEmpty())
            bonusScore += allClearBonus;

        ScoreManager.Instance.AddScore(comboScore + bonusScore);
    }

    private void CheckGameOver()
    {
        if (pieceSpawner == null || boardManager == null)
            return;

        RingPiece currentPiece = pieceSpawner.CurrentPiece;

        if (currentPiece == null)
            return;

        if (!boardManager.HasMove(currentPiece))
        {
            gameOver = true;
            Debug.Log("Game Over");
        }
    }
}