using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PieceSpawner pieceSpawner;

    [Header("Effect References")]
    [SerializeField] private MatchEffectManager matchEffectManager;

    [Header("Feedback")]
    [SerializeField] private GameplayFeedbackManager gameplayFeedbackManager;

    [Header("Piece Spawner Method Settings")]
    [Tooltip("PieceSpawner içindeki spawn method adı. Boş bırakırsan otomatik bulmaya çalışır.")]
    [SerializeField] private string spawnMethodName = "";

    [Header("Score Settings")]
    [SerializeField] private int pointsPerClearedRing = 10;
    [SerializeField] private int multiMatchBonus = 100;
    [SerializeField] private int allClearBonus = 500;

    [Header("Game State")]
    [SerializeField] private bool startGameOnStart = true;

    private bool isResolving;
    private bool isGameOver;
    private int comboCount;

    public bool IsResolving
    {
        get { return isResolving; }
    }

    public bool IsGameOver
    {
        get { return isGameOver; }
    }

    public int ComboCount
    {
        get { return comboCount; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindMissingReferences();
    }

    private void Start()
    {
        if (!startGameOnStart)
        {
            return;
        }

        StartGame();
    }

    private void FindMissingReferences()
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }

        if (pieceSpawner == null)
        {
            pieceSpawner = FindObjectOfType<PieceSpawner>();
        }

        if (matchEffectManager == null)
        {
            matchEffectManager = FindObjectOfType<MatchEffectManager>();
        }

        if (gameplayFeedbackManager == null)
        {
            gameplayFeedbackManager = FindObjectOfType<GameplayFeedbackManager>();
        }
    }

    public void StartGame()
    {
        FindMissingReferences();

        if (!ValidateReferences())
        {
            return;
        }

        isResolving = false;
        isGameOver = false;
        comboCount = 0;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.UpdateThemeByScore(0);
        }

        pieceSpawner.ClearCurrentPiece();
        SpawnPieceFromSpawner();

        CheckGameOver();
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (boardManager == null)
        {
            Debug.LogError("GameManager: BoardManager referansı bulunamadı.");
            isValid = false;
        }

        if (pieceSpawner == null)
        {
            Debug.LogError("GameManager: PieceSpawner referansı bulunamadı.");
            isValid = false;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("GameManager: ScoreManager.Instance bulunamadı. Skor sistemi çalışmayabilir.");
        }

        if (ThemeManager.Instance == null)
        {
            Debug.LogWarning("GameManager: ThemeManager.Instance bulunamadı. Tema sistemi çalışmayabilir.");
        }

        if (matchEffectManager == null)
        {
            Debug.LogWarning("GameManager: MatchEffectManager atanmadı. Match effect oynatılmayacak.");
        }

        if (gameplayFeedbackManager == null)
        {
            Debug.LogWarning("GameManager: GameplayFeedbackManager atanmadı. UI feedback gösterilmeyecek.");
        }

        return isValid;
    }

    public void OnPiecePlaced()
    {
        if (isGameOver)
        {
            return;
        }

        if (isResolving)
        {
            return;
        }

        StartCoroutine(ResolveMatchesRoutine());
    }

    private IEnumerator ResolveMatchesRoutine()
    {
        isResolving = true;

        List<MatchData> matches = boardManager.FindMatches();

        if (matches != null && matches.Count > 0)
        {
            comboCount++;

            if (matchEffectManager != null)
            {
                yield return matchEffectManager.PlayMatches(matches);
            }

            int clearedRingCount = boardManager.ClearMatches(matches);

            int baseScore = clearedRingCount * pointsPerClearedRing;
            int comboScore = baseScore * comboCount;
            int totalScore = comboScore;

            if (matches.Count >= 2)
            {
                totalScore += multiMatchBonus * matches.Count;
            }

            bool isAllClear = boardManager.IsBoardEmpty();

            if (isAllClear)
            {
                totalScore += allClearBonus;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(totalScore);
            }

            if (gameplayFeedbackManager != null)
            {
                gameplayFeedbackManager.PlayMatchFeedback(
                    comboCount,
                    matches.Count,
                    clearedRingCount,
                    isAllClear,
                    totalScore
                );
            }
        }
        else
        {
            comboCount = 0;
        }

        if (ThemeManager.Instance != null && ScoreManager.Instance != null)
        {
            ThemeManager.Instance.UpdateThemeByScore(ScoreManager.Instance.Score);
        }

        if (!isGameOver)
        {
            SpawnPieceFromSpawner();
        }

        isResolving = false;

        CheckGameOver();
    }

    public void TrashCurrentPiece()
    {
        if (isGameOver)
        {
            return;
        }

        if (isResolving)
        {
            return;
        }

        if (pieceSpawner == null)
        {
            Debug.LogWarning("GameManager: PieceSpawner yok, trash işlemi yapılamadı.");
            return;
        }

        pieceSpawner.ClearCurrentPiece();
        SpawnPieceFromSpawner();

        comboCount = 0;

        CheckGameOver();
    }

    public void DiscardCurrentPiece()
    {
        TrashCurrentPiece();
    }

    private void CheckGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        if (boardManager == null || pieceSpawner == null)
        {
            return;
        }

        RingPiece currentPiece = pieceSpawner.CurrentPiece;

        if (currentPiece == null)
        {
            return;
        }

        bool hasMove = boardManager.HasMove(currentPiece);

        if (!hasMove)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        isGameOver = true;
        isResolving = false;

        Debug.Log("Game Over");

        if (pieceSpawner != null)
        {
            pieceSpawner.ClearCurrentPiece();
        }
    }

    public void RestartGame()
    {
        if (isResolving)
        {
            return;
        }

        StartGame();
    }

    private void SpawnPieceFromSpawner()
    {
        if (pieceSpawner == null)
        {
            Debug.LogError("GameManager: PieceSpawner yok, yeni piece spawn edilemedi.");
            return;
        }

        MethodInfo spawnMethod = GetSpawnMethod();

        if (spawnMethod == null)
        {
            Debug.LogError("GameManager: PieceSpawner içinde uygun spawn methodu bulunamadı. Inspector'daki Spawn Method Name alanına PieceSpawner içindeki gerçek spawn method adını yaz.");
            LogPieceSpawnerMethods();
            return;
        }

        spawnMethod.Invoke(pieceSpawner, null);
    }

    private MethodInfo GetSpawnMethod()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (!string.IsNullOrEmpty(spawnMethodName))
        {
            MethodInfo explicitMethod = pieceSpawner.GetType().GetMethod(spawnMethodName, flags);

            if (IsValidSpawnMethod(explicitMethod))
            {
                return explicitMethod;
            }

            Debug.LogWarning("GameManager: Inspector'da verilen spawn method adı bulunamadı veya parametre alıyor: " + spawnMethodName);
        }

        string[] possibleNames =
        {
            "SpawnCurrentPiece",
            "SpawnNextPiece",
            "SpawnRandomPiece",
            "SpawnWeightedPiece",
            "SpawnNewRandomPiece",
            "SpawnNewCurrentPiece",
            "CreateCurrentPiece",
            "CreateNewPiece",
            "GeneratePiece",
            "GenerateNewPiece",
            "SpawnPiece",
            "SpawnNewPiece",
            "Spawn",
            "CreatePiece"
        };

        for (int i = 0; i < possibleNames.Length; i++)
        {
            MethodInfo method = pieceSpawner.GetType().GetMethod(possibleNames[i], flags);

            if (IsValidSpawnMethod(method))
            {
                return method;
            }
        }

        MethodInfo[] methods = pieceSpawner.GetType().GetMethods(flags);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (!IsValidSpawnMethod(method))
            {
                continue;
            }

            string lowerName = method.Name.ToLower();

            if (lowerName.Contains("spawn") && lowerName.Contains("piece"))
            {
                return method;
            }
        }

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (!IsValidSpawnMethod(method))
            {
                continue;
            }

            string lowerName = method.Name.ToLower();

            if (lowerName.Contains("create") && lowerName.Contains("piece"))
            {
                return method;
            }
        }

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (!IsValidSpawnMethod(method))
            {
                continue;
            }

            string lowerName = method.Name.ToLower();

            if (lowerName.Contains("generate") && lowerName.Contains("piece"))
            {
                return method;
            }
        }

        return null;
    }

    private bool IsValidSpawnMethod(MethodInfo method)
    {
        if (method == null)
        {
            return false;
        }

        ParameterInfo[] parameters = method.GetParameters();

        if (parameters != null && parameters.Length > 0)
        {
            return false;
        }

        return true;
    }

    private void LogPieceSpawnerMethods()
    {
        if (pieceSpawner == null)
        {
            return;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo[] methods = pieceSpawner.GetType().GetMethods(flags);

        string log = "PieceSpawner içindeki parametresiz methodlar:\n";

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (method == null)
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters != null && parameters.Length > 0)
            {
                continue;
            }

            log += "- " + method.Name + "\n";
        }

        Debug.Log(log);
    }
}