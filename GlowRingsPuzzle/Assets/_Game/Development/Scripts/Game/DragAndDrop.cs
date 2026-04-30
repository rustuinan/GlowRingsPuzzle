using DG.Tweening;
using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager boardManager;

    [Header("Input")]
    [SerializeField] private LayerMask draggableLayerMask;

    [Header("Drag Plane")]
    [SerializeField] private float dragPlaneY = 0f;

    [Header("Drag Settings")]
    [SerializeField] private Vector3 dragWorldOffset = new Vector3(0f, 0f, 0.65f);
    [SerializeField] private float dragFollowSpeed = 20f;

    [Header("Hold Lift")]
    [SerializeField] private float holdLiftHeight = 0.22f;
    [SerializeField] private float liftDuration = 0.14f;
    [SerializeField] private float dropDuration = 0.12f;
    [SerializeField] private Ease liftEase = Ease.OutCubic;
    [SerializeField] private Ease dropEase = Ease.InOutSine;

    [Header("Drop Settings")]
    [SerializeField] private float placeDistance = 1.25f;
    [SerializeField] private bool returnToStartIfInvalid = true;

    private RingPiece selectedPiece;

    private Vector3 selectedStartPosition;
    private Quaternion selectedStartRotation;

    private bool isDragging;
    private float currentLiftHeight;
    private Tween liftTween;

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnDisable()
    {
        KillLiftTween();
    }

    private void FindMissingReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectPiece();
        }

        if (Input.GetMouseButton(0))
        {
            DragSelectedPiece();
        }

        if (Input.GetMouseButtonUp(0))
        {
            DropSelectedPiece();
        }
    }

    private void TrySelectPiece()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.IsGameOver || GameManager.Instance.IsResolving)
            {
                return;
            }
        }

        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f, draggableLayerMask))
        {
            return;
        }

        RingPiece piece = hit.collider.GetComponentInParent<RingPiece>();

        if (piece == null)
        {
            return;
        }

        selectedPiece = piece;
        selectedStartPosition = selectedPiece.transform.position;
        selectedStartRotation = selectedPiece.transform.rotation;

        isDragging = true;

        StartLiftTween(holdLiftHeight, liftDuration, liftEase);
    }

    private void DragSelectedPiece()
    {
        if (!isDragging || selectedPiece == null)
        {
            return;
        }

        Vector3 planePosition;
        if (!TryGetDragPlanePosition(out planePosition))
        {
            return;
        }

        Vector3 targetPosition = planePosition + dragWorldOffset;
        targetPosition.y = dragPlaneY + currentLiftHeight;

        selectedPiece.transform.position = Vector3.Lerp(
            selectedPiece.transform.position,
            targetPosition,
            Time.deltaTime * dragFollowSpeed
        );
    }

    private bool TryGetDragPlanePosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (mainCamera == null)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));

        float enter;
        if (!dragPlane.Raycast(ray, out enter))
        {
            return false;
        }

        worldPosition = ray.GetPoint(enter);
        return true;
    }

    private void DropSelectedPiece()
    {
        if (!isDragging || selectedPiece == null)
        {
            ClearSelection();
            return;
        }

        Cell closestCell = FindClosestCell(selectedPiece.transform.position);

        bool placed = false;

        if (closestCell != null)
        {
            float distance = Vector3.Distance(
                new Vector3(selectedPiece.transform.position.x, 0f, selectedPiece.transform.position.z),
                new Vector3(closestCell.transform.position.x, 0f, closestCell.transform.position.z)
            );

            if (distance <= placeDistance && closestCell.CanPlace(selectedPiece))
            {
                KillLiftTween();
                currentLiftHeight = 0f;

                closestCell.PlacePiece(selectedPiece);
                placed = true;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPiecePlaced();
                }
            }
        }

        if (!placed)
        {
            if (returnToStartIfInvalid)
            {
                ReturnSelectedPieceToStart();
            }
            else
            {
                StartLiftTween(0f, dropDuration, dropEase);
            }
        }

        ClearSelection();
    }

    private void ReturnSelectedPieceToStart()
    {
        if (selectedPiece == null)
        {
            return;
        }

        KillLiftTween();

        Transform pieceTransform = selectedPiece.transform;

        Sequence returnSequence = DOTween.Sequence();
        returnSequence.SetUpdate(false);

        Vector3 startPosition = selectedStartPosition;
        startPosition.y = selectedStartPosition.y;

        returnSequence.Join(
            DOTween.To(
                () => currentLiftHeight,
                value =>
                {
                    currentLiftHeight = value;
                },
                0f,
                dropDuration
            ).SetEase(dropEase)
        );

        returnSequence.Join(
            pieceTransform.DOMove(startPosition, dropDuration)
                .SetEase(dropEase)
        );

        returnSequence.Join(
            pieceTransform.DORotateQuaternion(selectedStartRotation, dropDuration)
                .SetEase(dropEase)
        );

        returnSequence.OnComplete(() =>
        {
            currentLiftHeight = 0f;
        });
    }

    private Cell FindClosestCell(Vector3 worldPosition)
    {
        Cell[] cells = FindObjectsOfType<Cell>();

        if (cells == null || cells.Length == 0)
        {
            return null;
        }

        Cell closestCell = null;
        float closestDistance = float.MaxValue;

        Vector3 flatWorldPosition = new Vector3(worldPosition.x, 0f, worldPosition.z);

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            Vector3 flatCellPosition = new Vector3(cell.transform.position.x, 0f, cell.transform.position.z);
            float distance = Vector3.Distance(flatWorldPosition, flatCellPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCell = cell;
            }
        }

        return closestCell;
    }

    private void StartLiftTween(float targetHeight, float duration, Ease ease)
    {
        KillLiftTween();

        liftTween = DOTween.To(
            () => currentLiftHeight,
            value =>
            {
                currentLiftHeight = value;
            },
            targetHeight,
            duration
        ).SetEase(ease);
    }

    private void KillLiftTween()
    {
        if (liftTween != null)
        {
            liftTween.Kill();
            liftTween = null;
        }
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        isDragging = false;
    }
}