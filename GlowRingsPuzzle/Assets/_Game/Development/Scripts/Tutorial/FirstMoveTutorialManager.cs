using System.Collections;
using UnityEngine;

[System.Serializable]
public class TutorialPreparedRing
{
    public int cellIndex;
    public RingLayer layer = RingLayer.Outer;
    public RingColorType color = RingColorType.Yellow;
}

[System.Serializable]
public class FirstMoveTutorialStep
{
    public string stepName = "Tutorial Step";

    [Header("Prepared Rings")]
    public TutorialPreparedRing[] preparedRings;

    [Header("Player Target")]
    public int targetCellIndex;
    public RingLayer targetLayer = RingLayer.Outer;
    public RingColorType targetColor = RingColorType.Yellow;

    [Header("Optional Piece Override")]
    public RingPieceData overridePieceData;
}

public class FirstMoveTutorialManager : MonoBehaviour
{
    public static FirstMoveTutorialManager Instance { get; private set; }

    private const string TutorialCompletedKey = "FirstMoveTutorialCompleted";

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PieceSpawner pieceSpawner;
    [SerializeField] private TutorialHandHintUI handHintUI;

    [Header("Tutorial Ring Setup")]
    [SerializeField] private Ring tutorialRingPrefab;

    [Header("Single Layer Piece Data")]
    [SerializeField] private RingPieceData outerOnlyPieceData;
    [SerializeField] private RingPieceData middleOnlyPieceData;
    [SerializeField] private RingPieceData innerOnlyPieceData;

    [Header("Tutorial Steps")]
    [SerializeField]
    private FirstMoveTutorialStep[] tutorialSteps =
    {
        new FirstMoveTutorialStep
        {
            stepName = "Horizontal Mixed Layer Match",
            preparedRings = new TutorialPreparedRing[]
            {
                new TutorialPreparedRing
                {
                    cellIndex = 3,
                    layer = RingLayer.Outer,
                    color = RingColorType.Yellow
                },
                new TutorialPreparedRing
                {
                    cellIndex = 4,
                    layer = RingLayer.Middle,
                    color = RingColorType.Yellow
                }
            },
            targetCellIndex = 5,
            targetLayer = RingLayer.Inner,
            targetColor = RingColorType.Yellow
        },

        new FirstMoveTutorialStep
        {
            stepName = "Vertical Mixed Layer Match",
            preparedRings = new TutorialPreparedRing[]
            {
                new TutorialPreparedRing
                {
                    cellIndex = 1,
                    layer = RingLayer.Inner,
                    color = RingColorType.Blue
                },
                new TutorialPreparedRing
                {
                    cellIndex = 4,
                    layer = RingLayer.Outer,
                    color = RingColorType.Blue
                }
            },
            targetCellIndex = 7,
            targetLayer = RingLayer.Middle,
            targetColor = RingColorType.Blue
        },

        new FirstMoveTutorialStep
        {
            stepName = "Diagonal Mixed Layer Match",
            preparedRings = new TutorialPreparedRing[]
            {
                new TutorialPreparedRing
                {
                    cellIndex = 0,
                    layer = RingLayer.Middle,
                    color = RingColorType.Pink
                },
                new TutorialPreparedRing
                {
                    cellIndex = 4,
                    layer = RingLayer.Inner,
                    color = RingColorType.Pink
                }
            },
            targetCellIndex = 8,
            targetLayer = RingLayer.Outer,
            targetColor = RingColorType.Pink
        },

        new FirstMoveTutorialStep
        {
            stepName = "Same Cell Triple Layer Match",
            preparedRings = new TutorialPreparedRing[]
            {
                new TutorialPreparedRing
                {
                    cellIndex = 4,
                    layer = RingLayer.Outer,
                    color = RingColorType.Green
                },
                new TutorialPreparedRing
                {
                    cellIndex = 4,
                    layer = RingLayer.Middle,
                    color = RingColorType.Green
                }
            },
            targetCellIndex = 4,
            targetLayer = RingLayer.Inner,
            targetColor = RingColorType.Green
        }
    };

    [Header("Settings")]
    [SerializeField] private bool forceTutorialInEditor = false;
    [SerializeField] private bool restrictPlacementToTargetCell = true;
    [SerializeField] private float setupDelay = 0.45f;
    [SerializeField] private float nextStepDelayAfterMatch = 0.95f;

    private bool isTutorialActive;
    private bool isTutorialCompletedThisSession;
    private int currentStepIndex;
    private Cell targetCell;
    private RingPiece currentTutorialPiece;

    public bool IsTutorialActive
    {
        get { return isTutorialActive; }
    }

    public bool RestrictPlacementToTargetCell
    {
        get { return restrictPlacementToTargetCell; }
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
        if (ShouldPlayTutorial())
        {
            isTutorialActive = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RefreshTrashUI();
            }

            StartCoroutine(StartTutorialRoutine());
        }
        else
        {
            if (handHintUI != null)
            {
                handHintUI.HideInstant();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RefreshTrashUI();
            }
        }
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

        if (handHintUI == null)
        {
            handHintUI = FindObjectOfType<TutorialHandHintUI>(true);
        }
    }

    private bool ShouldPlayTutorial()
    {
        if (forceTutorialInEditor)
        {
            return true;
        }

        return PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 0;
    }

    private IEnumerator StartTutorialRoutine()
    {
        yield return new WaitForSeconds(setupDelay);
        StartTutorial();
    }

    public void StartTutorial()
    {
        FindMissingReferences();

        if (!ValidateBaseSetup())
        {
            return;
        }

        isTutorialActive = true;
        isTutorialCompletedThisSession = false;
        currentStepIndex = 0;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshTrashUI();
        }

        SetupCurrentStep();
    }

    private void SetupCurrentStep()
    {
        if (!isTutorialActive)
        {
            return;
        }

        if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Length)
        {
            CompleteTutorial();
            return;
        }

        FirstMoveTutorialStep step = tutorialSteps[currentStepIndex];

        if (!ValidateStep(step))
        {
            CompleteTutorial();
            return;
        }

        if (handHintUI != null)
        {
            handHintUI.HideInstant();
        }

        boardManager.ClearBoard();

        if (pieceSpawner.CurrentPiece != null)
        {
            pieceSpawner.ClearCurrentPiece();
        }

        for (int i = 0; i < step.preparedRings.Length; i++)
        {
            TutorialPreparedRing preparedRing = step.preparedRings[i];

            if (preparedRing == null)
            {
                continue;
            }

            Cell cell = boardManager.GetCell(preparedRing.cellIndex);

            if (cell == null)
            {
                continue;
            }

            cell.ForcePlaceRing(
                tutorialRingPrefab,
                preparedRing.layer,
                preparedRing.color
            );
        }

        targetCell = boardManager.GetCell(step.targetCellIndex);

        RingPieceData pieceData = GetPieceDataForStep(step);

        currentTutorialPiece = pieceSpawner.SpawnForcedPiece(pieceData, step.targetColor);

        if (handHintUI != null && currentTutorialPiece != null && targetCell != null)
        {
            handHintUI.Play(currentTutorialPiece.transform, targetCell.transform);
        }
    }

    private RingPieceData GetPieceDataForStep(FirstMoveTutorialStep step)
    {
        if (step == null)
        {
            return null;
        }

        if (step.overridePieceData != null)
        {
            return step.overridePieceData;
        }

        if (step.targetLayer == RingLayer.Outer)
        {
            return outerOnlyPieceData;
        }

        if (step.targetLayer == RingLayer.Middle)
        {
            return middleOnlyPieceData;
        }

        if (step.targetLayer == RingLayer.Inner)
        {
            return innerOnlyPieceData;
        }

        return null;
    }

    private bool ValidateBaseSetup()
    {
        if (boardManager == null)
        {
            Debug.LogError("FirstMoveTutorialManager: BoardManager atanmadı.");
            return false;
        }

        if (pieceSpawner == null)
        {
            Debug.LogError("FirstMoveTutorialManager: PieceSpawner atanmadı.");
            return false;
        }

        if (tutorialRingPrefab == null)
        {
            Debug.LogError("FirstMoveTutorialManager: Tutorial Ring Prefab atanmadı.");
            return false;
        }

        if (outerOnlyPieceData == null)
        {
            Debug.LogError("FirstMoveTutorialManager: OuterOnly PieceData atanmadı.");
            return false;
        }

        if (middleOnlyPieceData == null)
        {
            Debug.LogError("FirstMoveTutorialManager: MiddleOnly PieceData atanmadı.");
            return false;
        }

        if (innerOnlyPieceData == null)
        {
            Debug.LogError("FirstMoveTutorialManager: InnerOnly PieceData atanmadı.");
            return false;
        }

        if (tutorialSteps == null || tutorialSteps.Length == 0)
        {
            Debug.LogError("FirstMoveTutorialManager: Tutorial Steps boş.");
            return false;
        }

        return true;
    }

    private bool ValidateStep(FirstMoveTutorialStep step)
    {
        if (step == null)
        {
            Debug.LogError("FirstMoveTutorialManager: Step null.");
            return false;
        }

        if (step.preparedRings == null || step.preparedRings.Length == 0)
        {
            Debug.LogError("FirstMoveTutorialManager: Step preparedRings boş. Step: " + step.stepName);
            return false;
        }

        for (int i = 0; i < step.preparedRings.Length; i++)
        {
            TutorialPreparedRing preparedRing = step.preparedRings[i];

            if (preparedRing == null)
            {
                Debug.LogError("FirstMoveTutorialManager: Prepared ring null. Step: " + step.stepName);
                return false;
            }

            if (boardManager.GetCell(preparedRing.cellIndex) == null)
            {
                Debug.LogError("FirstMoveTutorialManager: Prepared cell bulunamadı. Step: " + step.stepName + " Index: " + preparedRing.cellIndex);
                return false;
            }
        }

        if (boardManager.GetCell(step.targetCellIndex) == null)
        {
            Debug.LogError("FirstMoveTutorialManager: Target cell bulunamadı. Step: " + step.stepName + " Index: " + step.targetCellIndex);
            return false;
        }

        RingPieceData pieceData = GetPieceDataForStep(step);

        if (pieceData == null)
        {
            Debug.LogError("FirstMoveTutorialManager: Step için PieceData bulunamadı. Step: " + step.stepName);
            return false;
        }

        if (!pieceData.HasLayer(step.targetLayer))
        {
            Debug.LogWarning("FirstMoveTutorialManager: PieceData target layer'ı içermiyor. Step: " + step.stepName);
        }

        return true;
    }

    public bool CanPlaceToCell(Cell cell)
    {
        if (!isTutorialActive)
        {
            return true;
        }

        if (!restrictPlacementToTargetCell)
        {
            return true;
        }

        return cell == targetCell;
    }

    public void NotifyPieceGrabbed()
    {
        if (!isTutorialActive)
        {
            return;
        }

        if (handHintUI != null)
        {
            handHintUI.HideInstant();
        }
    }

    public void NotifyInvalidPlacement()
    {
        if (!isTutorialActive)
        {
            return;
        }

        if (currentTutorialPiece != null && targetCell != null && handHintUI != null)
        {
            handHintUI.Play(currentTutorialPiece.transform, targetCell.transform);
        }
    }

    public void NotifyPiecePlaced(Cell placedCell)
    {
        if (!isTutorialActive)
        {
            return;
        }

        if (placedCell != targetCell)
        {
            return;
        }

        if (handHintUI != null)
        {
            handHintUI.HideInstant();
        }

        StartCoroutine(AdvanceAfterMatchRoutine());
    }

    private IEnumerator AdvanceAfterMatchRoutine()
    {
        currentStepIndex++;

        if (currentStepIndex >= tutorialSteps.Length)
        {
            CompleteTutorial();
            yield break;
        }

        yield return new WaitForSeconds(nextStepDelayAfterMatch);

        if (GameManager.Instance != null)
        {
            while (GameManager.Instance.IsResolving)
            {
                yield return null;
            }
        }

        SetupCurrentStep();
    }

    private void CompleteTutorial()
    {
        if (isTutorialCompletedThisSession)
        {
            return;
        }

        isTutorialCompletedThisSession = true;
        isTutorialActive = false;
        targetCell = null;
        currentTutorialPiece = null;

        if (handHintUI != null)
        {
            handHintUI.HideInstant();
        }

        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshTrashUI();
        }
    }

    [ContextMenu("Reset Tutorial Save")]
    public void ResetTutorialSave()
    {
        PlayerPrefs.DeleteKey(TutorialCompletedKey);
        PlayerPrefs.Save();
    }
}