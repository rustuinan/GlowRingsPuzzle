using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    [SerializeField] private int multiMatchBonus = 120;
    [SerializeField] private int allClearBonus = 750;

    [Header("Cell Bomb")]
    [Min(0)] [SerializeField] private int startingBombCount = 2;
    private int currentBombCount;
    private bool bombTargeting;
    private DragAndDrop dragAndDrop;
    public int CurrentBombCount => currentBombCount;
    public bool IsBombTargeting => bombTargeting;
    public bool HasBlockedPiece { get; private set; }
    public bool IsBoosterOfferOpen { get; private set; }
    private bool boosterRescueSelected;
    public BoardManager Board => boardManager;
    public bool IsTutorialActive => IsTutorialBlockingInput();
    public bool IsPieceInteracting => dragAndDrop != null && dragAndDrop.IsInteracting;
    public bool CanUseBomb => !isGameOver && !IsResolving && !IsBoosterOfferOpen && !IsTutorialBlockingInput() &&
        !IsPieceInteracting && currentBombCount > 0 && boardManager != null && !boardManager.IsBoardEmpty();
    public int ComboPreparationMovesRemaining => comboChain.PreparationMovesRemaining;

    [Header("Game State")]
    [SerializeField] private bool startGameOnStart = true;

    private bool isResolving;
    private bool isGameOver;
    private bool isTrashAnimating;
    private bool hasUsedExtraLife;
    private bool reviveRequestPending;
    private readonly ComboChain comboChain = new ComboChain();
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
        get { return comboChain.Multiplier; }
    }

    public int CurrentTrashCount
    {
        get { return currentTrashCount; }
    }

    public int StartingTrashCount
    {
        get { return startingTrashCount; }
    }

    public bool CanOfferExtraLife
    {
        get { return isGameOver && !hasUsedExtraLife && !reviveRequestPending; }
    }

    public bool IsExtraLifeRequestPending
    {
        get { return reviveRequestPending; }
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

    private void LateUpdate()
    {
        RefreshTrashUI();
        if (!bombTargeting) return;
        if (IsTutorialBlockingInput() || isGameOver || IsResolving)
        {
            CancelBombTargeting();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) { CancelBombTargeting(); return; }
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null &&
            (EventSystem.current.IsPointerOverGameObject() ||
            (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(boardManager.transform.up, boardManager.transform.position);
        if (!plane.Raycast(ray, out float distance)) { CancelBombTargeting(); return; }
        Vector3 point = ray.GetPoint(distance);
        Cell closest = null;
        float closestDistance = float.MaxValue;
        float spacing = 1f;
        Cell[] cells = boardManager.Cells;
        if (cells.Length > 1 && cells[0] != null && cells[1] != null)
            spacing = Vector3.Distance(cells[0].transform.position, cells[1].transform.position);
        foreach (Cell cell in cells)
        {
            if (cell == null) continue;
            float candidateDistance = Vector3.ProjectOnPlane(point - cell.transform.position, plane.normal).magnitude;
            if (candidateDistance >= closestDistance) continue;
            closestDistance = candidateDistance;
            closest = cell;
        }
        if (closest == null || closestDistance > spacing * 0.65f) { CancelBombTargeting(); return; }
        TryBombCell(closest);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && bombTargeting) CancelBombTargeting();
    }

    private void FindMissingReferences()
    {
        if (dragAndDrop == null) dragAndDrop = FindObjectOfType<DragAndDrop>();
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
        hasUsedExtraLife = false;
        reviveRequestPending = false;
        comboChain.Reset();
        currentBombCount = Mathf.Max(0, startingBombCount);
        bombTargeting = false;
        HasBlockedPiece = false;
        IsBoosterOfferOpen = false;
        boosterRescueSelected = false;

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
            pieceSpawner.ResetBalance();
        }

        if (!IsTutorialBlockingInput())
        {
            SpawnPieceFromSpawner();
            CheckGameOver();
        }
        else
        {
            RefreshTrashUI();
        }

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.ShowInterstitialAfterLevelStart();
        }
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

    private bool IsTutorialBlockingInput()
    {
        return FirstMoveTutorialManager.Instance != null &&
               FirstMoveTutorialManager.Instance.IsTutorialActive;
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
        comboChain.RegisterMove(matches != null && matches.Count > 0);

        if (pieceSpawner != null)
        {
            pieceSpawner.ReportMoveResult(matches != null && matches.Count > 0);
        }

        if (matches != null && matches.Count > 0)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMatch();
            }

            if (matchEffectManager != null)
            {
                yield return matchEffectManager.PlayMatches(matches);
            }

            int clearedRingCount = boardManager.ClearMatches(matches);

            int baseScore = clearedRingCount * pointsPerClearedRing;
            int comboScore = baseScore * ComboCount;
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
                    ComboCount,
                    matches.Count,
                    clearedRingCount,
                    isAllClear,
                    totalScore
                );
            }
        }
        if (ThemeManager.Instance != null && ScoreManager.Instance != null)
        {
            ThemeManager.Instance.UpdateThemeByScore(ScoreManager.Instance.Score);
        }

        if (!isGameOver && !IsTutorialBlockingInput())
        {
            SpawnPieceFromSpawner();
        }

        isResolving = false;
        RefreshTrashUI();

        if (!IsTutorialBlockingInput())
        {
            CheckGameOver();
        }
    }

    public void TrashCurrentPiece()
    {
        if (IsBoosterOfferOpen) return;
        if (bombTargeting || IsPieceInteracting) return;
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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTrash();
        }

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

        if (!IsTutorialBlockingInput())
        {
            SpawnPieceFromSpawner();
        }

        comboChain.Reset();

        isTrashAnimating = false;
        RefreshTrashUI();

        if (!IsTutorialBlockingInput())
        {
            CheckGameOver();
        }
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
                !IsBoosterOfferOpen &&
                !bombTargeting &&
                !IsPieceInteracting &&
                currentTrashCount > 0;
        }
    }

    private void CheckGameOver()
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
        HasBlockedPiece = !hasMove;

        bool hasBombRescue = currentBombCount > 0 && !boardManager.IsBoardEmpty();
        if (hasMove)
        {
            IsBoosterOfferOpen = false;
            boosterRescueSelected = false;
            RefreshTrashUI();
        }
        else if (currentTrashCount <= 0 && !hasBombRescue)
        {
            GameOver();
        }
        else if (!boosterRescueSelected)
        {
            IsBoosterOfferOpen = true;
            RefreshTrashUI();
        }
    }

    private void GameOver()
    {
        IsBoosterOfferOpen = false;
        boosterRescueSelected = false;
        bombTargeting = false;
        isGameOver = true;
        isResolving = false;
        isTrashAnimating = false;

        RefreshTrashUI();

        int currentScore = 0;
        int currentHighScore = 0;

        if (ScoreManager.Instance != null)
        {
            currentScore = ScoreManager.Instance.Score;
            currentHighScore = ScoreManager.Instance.HighScore;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }

        if (gameOverPanelUI != null)
        {
            gameOverPanelUI.Show(currentScore, currentHighScore);
        }

        Debug.Log("Game Over");
    }

    public void ToggleBombTargeting()
    {
        if (bombTargeting) { CancelBombTargeting(); return; }
        if (!CanUseBomb) return;
        bombTargeting = true;
        RefreshTrashUI();
    }

    public void CancelBombTargeting()
    {
        bombTargeting = false;
        if (boosterRescueSelected && HasBlockedPiece)
        {
            boosterRescueSelected = false;
            CheckGameOver();
        }
        RefreshTrashUI();
    }

    public void AcceptBombRescue()
    {
        if (!IsBoosterOfferOpen || currentBombCount <= 0 || IsResolving || isGameOver) return;
        IsBoosterOfferOpen = false;
        boosterRescueSelected = true;
        ToggleBombTargeting();
        if (!bombTargeting)
        {
            boosterRescueSelected = false;
            CheckGameOver();
        }
    }

    public void AcceptDiscardRescue()
    {
        if (!IsBoosterOfferOpen || currentTrashCount <= 0 || IsResolving || isGameOver) return;
        IsBoosterOfferOpen = false;
        // A replacement can also be blocked, so check it and offer a fresh choice afterwards.
        boosterRescueSelected = false;
        TrashCurrentPiece();
    }

    public void DeclineBoosterRescue()
    {
        if (!IsBoosterOfferOpen) return;
        GameOver();
    }

    public bool TryBombCell(Cell cell)
    {
        if (!bombTargeting || !CanUseBomb || cell == null ||
            System.Array.IndexOf(boardManager.Cells, cell) < 0 || cell.GetAllRings().Length == 0)
            return false;

        bombTargeting = false;
        currentBombCount--;
        comboChain.Reset();
        StartCoroutine(BombCellRoutine(cell));
        return true;
    }

    private IEnumerator BombCellRoutine(Cell cell)
    {
        isResolving = true;
        RefreshTrashUI();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayTrash();
        foreach (Ring ring in cell.GetAllRings())
        {
            if (ring == null) continue;
            ring.transform.DOKill();
            ring.transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack);
        }
        yield return new WaitForSeconds(0.24f);
        if (cell != null) cell.ClearAllRings();
        // A booster grants space, never match points, an all-clear bonus or a new piece.
        isResolving = false;
        boosterRescueSelected = false;
        RefreshTrashUI();
        CheckGameOver();
    }

    public void RestartGame()
    {
        if (IsResolving)
        {
            return;
        }

        StartGame();
    }

    public bool TryContinueWithRewardedAd()
    {
        if (!CanOfferExtraLife || AdMobManager.Instance == null || !AdMobManager.Instance.IsRewardedReady)
        {
            return false;
        }

        reviveRequestPending = true;

        bool started = AdMobManager.Instance.ShowRewardedExtraLife(
            ContinueAfterRewardedAd,
            CancelRewardedContinue);

        if (!started)
        {
            reviveRequestPending = false;
        }

        return started;
    }

    private void ContinueAfterRewardedAd()
    {
        if (!isGameOver || hasUsedExtraLife)
        {
            reviveRequestPending = false;
            return;
        }

        hasUsedExtraLife = true;
        reviveRequestPending = false;

        FindMissingReferences();

        RingPiece blockedPiece = pieceSpawner != null ? pieceSpawner.CurrentPiece : null;
        int clearedRingCount = boardManager != null
            ? boardManager.CreateReviveSpace(blockedPiece)
            : 0;

        isGameOver = false;
        isResolving = false;
        isTrashAnimating = false;

        if (pieceSpawner != null && pieceSpawner.CurrentPiece == null)
        {
            SpawnPieceFromSpawner();
        }

        System.Action finishContinue = () =>
        {
            RefreshTrashUI();
            CheckGameOver();
        };

        if (gameOverPanelUI != null)
        {
            gameOverPanelUI.HideAnimated(finishContinue);
        }
        else
        {
            finishContinue.Invoke();
        }

        Debug.Log("Ekstra can kullanıldı. Açılan halka sayısı: " + clearedRingCount);
    }

    private void CancelRewardedContinue()
    {
        reviveRequestPending = false;

        if (gameOverPanelUI != null)
        {
            gameOverPanelUI.RefreshExtraLifeButton();
        }
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
        if (IsTutorialBlockingInput())
        {
            RefreshTrashUI();
            return;
        }

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

    public void ContinueAfterTutorial()
    {
        if (isGameOver)
        {
            return;
        }

        if (IsResolving)
        {
            StartCoroutine(ContinueAfterTutorialRoutine());
            return;
        }

        ContinueAfterTutorialInternal();
    }

    private IEnumerator ContinueAfterTutorialRoutine()
    {
        while (IsResolving)
        {
            yield return null;
        }

        ContinueAfterTutorialInternal();
    }

    private void ContinueAfterTutorialInternal()
    {
        if (isGameOver)
        {
            return;
        }

        if (IsTutorialBlockingInput())
        {
            return;
        }

        if (pieceSpawner == null)
        {
            FindMissingReferences();
        }

        if (pieceSpawner == null)
        {
            Debug.LogError("GameManager: PieceSpawner bulunamadı, tutorial sonrası oyun devam edemedi.");
            return;
        }

        if (pieceSpawner.CurrentPiece == null)
        {
            SpawnPieceFromSpawner();
        }

        RefreshTrashUI();
        CheckGameOver();
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
