using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Game Over UI")]
    [SerializeField] private GameOverPanelUI gameOverPanelUI;

    [Header("Trash")]
    [SerializeField] private int startingTrashCount = 3;
    [SerializeField] private TMP_Text trashCountText;
    [SerializeField] private Button trashButton;

    [Header("Trash Piece Animation")]
    [SerializeField] private Transform trashFlyTarget;
    [SerializeField] private float trashAnimationDuration = 0.34f;
    [SerializeField] private float trashLiftAmount = 0.16f;
    [SerializeField] private float trashShrinkScale = 0.05f;
    [SerializeField] private Vector3 trashRotationPunch = new Vector3(0f, 210f, 25f);
    [SerializeField] private Ease trashMoveEase = Ease.InCubic;
    [SerializeField] private Ease trashScaleEase = Ease.InBack;

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
    private bool isTrashAnimating;
    private int comboCount;
    private int currentTrashCount;

    public bool IsResolving
    {
        get { return isResolving || isTrashAnimating; }
    }

    public bool IsGameOver
    {
        get { return isGameOver; }
    }

    public int ComboCount
    {
        get { return comboCount; }
    }

    public int CurrentTrashCount
    {
        get { return currentTrashCount; }
    }

    public int StartingTrashCount
    {
        get { return startingTrashCount; }
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
        if (gameOverPanelUI != null)
        {
            gameOverPanelUI.HideImmediate();
        }

        if (!startGameOnStart)
        {
            RefreshTrashUI();
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

        if (gameOverPanelUI == null)
        {
            gameOverPanelUI = FindObjectOfType<GameOverPanelUI>(true);
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
        isTrashAnimating = false;
        isGameOver = false;
        comboCount = 0;

        if (gameOverPanelUI != null)
        {
            gameOverPanelUI.HideImmediate();
        }

        ResetTrashCount();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.UpdateThemeByScore(0);
        }

        SafeClearBoard();

        if (pieceSpawner != null)
        {
            pieceSpawner.ClearCurrentPiece();
        }

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

        if (gameOverPanelUI == null)
        {
            Debug.LogWarning("GameManager: GameOverPanelUI atanmadı. Game over paneli gösterilmeyecek.");
        }

        return isValid;
    }

    public void OnPiecePlaced()
    {
        if (isGameOver)
        {
            return;
        }

        if (IsResolving)
        {
            return;
        }

        StartCoroutine(ResolveMatchesRoutine());
    }

    private IEnumerator ResolveMatchesRoutine()
    {
        isResolving = true;
        RefreshTrashUI();

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
        RefreshTrashUI();

        CheckGameOver();
    }

    private bool IsTutorialBlockingInput()
    {
        return FirstMoveTutorialManager.Instance != null &&
               FirstMoveTutorialManager.Instance.IsTutorialActive;
    }

    public void TrashCurrentPiece()
    {
        if (IsTutorialBlockingInput())
        {
            RefreshTrashUI();
            return;
        }

        if (isGameOver)
        {
            return;
        }

        if (IsResolving)
        {
            return;
        }

        if (currentTrashCount <= 0)
        {
            RefreshTrashUI();
            Debug.Log("GameManager: Trash hakkı kalmadı.");
            return;
        }

        if (pieceSpawner == null)
        {
            Debug.LogWarning("GameManager: PieceSpawner yok, trash işlemi yapılamadı.");
            return;
        }

        if (pieceSpawner.CurrentPiece == null)
        {
            Debug.LogWarning("GameManager: Silinecek current piece yok.");
            return;
        }

        StartCoroutine(TrashCurrentPieceRoutine());
    }

    private IEnumerator TrashCurrentPieceRoutine()
    {
        isTrashAnimating = true;

        currentTrashCount--;
        RefreshTrashUI();

        RingPiece pieceToTrash = pieceSpawner.CurrentPiece;

        if (pieceToTrash == null)
        {
            isTrashAnimating = false;
            RefreshTrashUI();
            yield break;
        }

        Transform pieceTransform = pieceToTrash.transform;

        Sequence trashSequence = DOTween.Sequence();
        trashSequence.SetUpdate(false);

        Vector3 startPosition = pieceTransform.position;
        Vector3 targetPosition = GetTrashAnimationTargetPosition(startPosition);

        pieceTransform.DOKill();

        trashSequence.Append(
            pieceTransform.DOMove(
                startPosition + Vector3.up * trashLiftAmount,
                trashAnimationDuration * 0.25f
            ).SetEase(Ease.OutSine)
        );

        trashSequence.Append(
            pieceTransform.DOMove(
                targetPosition,
                trashAnimationDuration * 0.75f
            ).SetEase(trashMoveEase)
        );

        trashSequence.Join(
            pieceTransform.DOScale(
                Vector3.one * trashShrinkScale,
                trashAnimationDuration * 0.75f
            ).SetEase(trashScaleEase)
        );

        trashSequence.Join(
            pieceTransform.DORotate(
                pieceTransform.eulerAngles + trashRotationPunch,
                trashAnimationDuration * 0.75f,
                RotateMode.FastBeyond360
            ).SetEase(Ease.InOutSine)
        );

        yield return trashSequence.WaitForCompletion();

        pieceSpawner.ClearCurrentPiece();
        SpawnPieceFromSpawner();

        comboCount = 0;

        isTrashAnimating = false;
        RefreshTrashUI();

        CheckGameOver();
    }

    private Vector3 GetTrashAnimationTargetPosition(Vector3 fallbackStartPosition)
    {
        if (trashFlyTarget != null)
        {
            return trashFlyTarget.position;
        }

        return fallbackStartPosition + new Vector3(0f, 0.28f, -0.35f);
    }

    public void DiscardCurrentPiece()
    {
        TrashCurrentPiece();
    }

    private void ResetTrashCount()
    {
        currentTrashCount = Mathf.Max(0, startingTrashCount);
        RefreshTrashUI();
    }

    public void RefreshTrashUI()
    {
        if (trashCountText != null)
        {
            trashCountText.text = currentTrashCount.ToString();
        }

        if (trashButton != null)
        {
            bool tutorialBlockingInput = IsTutorialBlockingInput();

            trashButton.interactable =
                !tutorialBlockingInput &&
                !isGameOver &&
                !IsResolving &&
                currentTrashCount > 0;
        }
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
        else
        {
            RefreshTrashUI();
        }
    }

    private void GameOver()
    {
        isGameOver = true;
        isResolving = false;
        isTrashAnimating = false;

        if (pieceSpawner != null)
        {
            pieceSpawner.ClearCurrentPiece();
        }

        ResetTrashCount();
        RefreshTrashUI();

        int currentScore = 0;
        int currentHighScore = 0;

        if (ScoreManager.Instance != null)
        {
            currentScore = ScoreManager.Instance.Score;
            currentHighScore = ScoreManager.Instance.HighScore;
        }

        if (gameOverPanelUI != null)
        {
            gameOverPanelUI.Show(currentScore, currentHighScore);
        }

        Debug.Log("Game Over");
    }

    public void RestartGame()
    {
        if (IsResolving)
        {
            return;
        }

        StartGame();
    }

    private void SafeClearBoard()
    {
        if (boardManager == null)
        {
            return;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        string[] possibleNames =
        {
            "ClearBoard",
            "ResetBoard",
            "ClearAllCells",
            "ResetAllCells",
            "Clear",
            "Reset"
        };

        for (int i = 0; i < possibleNames.Length; i++)
        {
            MethodInfo method = boardManager.GetType().GetMethod(possibleNames[i], flags);

            if (IsValidParameterlessMethod(method))
            {
                method.Invoke(boardManager, null);
                return;
            }
        }

        MethodInfo[] methods = boardManager.GetType().GetMethods(flags);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (!IsValidParameterlessMethod(method))
            {
                continue;
            }

            string lowerName = method.Name.ToLower();

            if ((lowerName.Contains("clear") || lowerName.Contains("reset")) &&
                (lowerName.Contains("board") || lowerName.Contains("cell")))
            {
                method.Invoke(boardManager, null);
                return;
            }
        }

        Debug.LogWarning("GameManager: BoardManager içinde board temizleme methodu bulunamadı.");
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
        RefreshTrashUI();
    }

    private MethodInfo GetSpawnMethod()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (!string.IsNullOrEmpty(spawnMethodName))
        {
            MethodInfo explicitMethod = pieceSpawner.GetType().GetMethod(spawnMethodName, flags);

            if (IsValidParameterlessMethod(explicitMethod))
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

            if (IsValidParameterlessMethod(method))
            {
                return method;
            }
        }

        MethodInfo[] methods = pieceSpawner.GetType().GetMethods(flags);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (!IsValidParameterlessMethod(method))
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

            if (!IsValidParameterlessMethod(method))
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

            if (!IsValidParameterlessMethod(method))
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

    private bool IsValidParameterlessMethod(MethodInfo method)
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