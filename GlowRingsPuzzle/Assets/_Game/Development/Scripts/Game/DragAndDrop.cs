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

    [Header("Drag Position")]
    [SerializeField] private Vector3 dragWorldOffset = new Vector3(0f, 0f, 0.65f);
    [SerializeField] private float dragFollowSpeed = 24f;

    [Header("Pick Up Animation")]
    [SerializeField] private float holdLiftHeight = 0.18f;
    [SerializeField] private float pickUpDuration = 0.16f;
    [SerializeField] private float pickedScale = 1.06f;
    [SerializeField] private Ease pickUpEase = Ease.OutCubic;

    [Header("Drag Polish")]
    [SerializeField] private bool useHoverMotion = true;
    [SerializeField] private float hoverBobAmount = 0.025f;
    [SerializeField] private float hoverBobSpeed = 7.5f;

    [SerializeField] private bool useDragTilt = true;
    [SerializeField] private float maxTiltAngle = 8f;
    [SerializeField] private float tiltSmoothSpeed = 16f;

    [Header("Placement")]
    [SerializeField] private float placeDistance = 0.78f;
    [SerializeField] private float placementDuration = 0.18f;
    [SerializeField] private Ease placementEase = Ease.OutSine;

    [Header("Invalid Return")]
    [SerializeField] private bool returnToStartIfInvalid = true;
    [SerializeField] private float invalidReturnDuration = 0.20f;
    [SerializeField] private Ease invalidReturnEase = Ease.InOutSine;

    private RingPiece selectedPiece;
    private Transform selectedTransform;

    private Vector3 selectedStartPosition;
    private Quaternion selectedStartRotation;
    private Vector3 selectedStartScale;

    private bool isDragging;
    private bool isDropAnimating;

    private float currentLiftHeight;
    private float currentScaleMultiplier = 1f;
    private float dragStartTime;

    private Tween liftTween;
    private Tween scaleTween;
    private Sequence dropSequence;

    private Vector3 lastDragTargetPosition;

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnDisable()
    {
        KillTweens();
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

        if (isDropAnimating)
        {
            return;
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
        selectedTransform = selectedPiece.transform;

        selectedStartPosition = selectedTransform.position;
        selectedStartRotation = selectedTransform.rotation;
        selectedStartScale = selectedTransform.localScale;

        isDragging = true;
        isDropAnimating = false;

        dragStartTime = Time.time;
        currentLiftHeight = 0f;
        currentScaleMultiplier = 1f;

        lastDragTargetPosition = selectedTransform.position;

        selectedTransform.DOKill();
        KillLiftTween();
        KillScaleTween();

        liftTween = DOTween.To(
            () => currentLiftHeight,
            value => currentLiftHeight = value,
            holdLiftHeight,
            pickUpDuration
        ).SetEase(pickUpEase);

        scaleTween = DOTween.To(
            () => currentScaleMultiplier,
            value =>
            {
                currentScaleMultiplier = value;

                if (selectedTransform != null)
                {
                    selectedTransform.localScale = selectedStartScale * currentScaleMultiplier;
                }
            },
            pickedScale,
            pickUpDuration
        ).SetEase(pickUpEase);

        if (FirstMoveTutorialManager.Instance != null)
        {
            FirstMoveTutorialManager.Instance.NotifyPieceGrabbed();
        }
    }

    private void DragSelectedPiece()
    {
        if (!isDragging || selectedPiece == null || selectedTransform == null)
        {
            return;
        }

        Vector3 planePosition;
        if (!TryGetDragPlanePosition(out planePosition))
        {
            return;
        }

        float hoverOffset = 0f;

        if (useHoverMotion)
        {
            float elapsed = Time.time - dragStartTime;
            hoverOffset = Mathf.Sin(elapsed * hoverBobSpeed) * hoverBobAmount;
        }

        Vector3 targetPosition = planePosition + dragWorldOffset;
        targetPosition.y = dragPlaneY + currentLiftHeight + hoverOffset;

        selectedTransform.position = Vector3.Lerp(
            selectedTransform.position,
            targetPosition,
            Time.deltaTime * dragFollowSpeed
        );

        ApplyDragTilt(targetPosition);

        lastDragTargetPosition = targetPosition;
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

    private void ApplyDragTilt(Vector3 targetPosition)
    {
        if (!useDragTilt || selectedTransform == null)
        {
            return;
        }

        Vector3 movement = targetPosition - lastDragTargetPosition;

        float tiltX = Mathf.Clamp(-movement.z * maxTiltAngle * 10f, -maxTiltAngle, maxTiltAngle);
        float tiltZ = Mathf.Clamp(movement.x * maxTiltAngle * 10f, -maxTiltAngle, maxTiltAngle);

        Quaternion targetRotation = selectedStartRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

        selectedTransform.rotation = Quaternion.Slerp(
            selectedTransform.rotation,
            targetRotation,
            Time.deltaTime * tiltSmoothSpeed
        );
    }

    private void DropSelectedPiece()
    {
        if (!isDragging || selectedPiece == null || selectedTransform == null)
        {
            ClearSelection();
            return;
        }

        Cell closestCell = FindClosestCell(selectedTransform.position);

        bool canPlace = false;

        if (closestCell != null)
        {
            float distance = Vector3.Distance(
                new Vector3(selectedTransform.position.x, 0f, selectedTransform.position.z),
                new Vector3(closestCell.transform.position.x, 0f, closestCell.transform.position.z)
            );

            canPlace = distance <= placeDistance && closestCell.CanPlace(selectedPiece);

            if (canPlace && FirstMoveTutorialManager.Instance != null)
            {
                canPlace = FirstMoveTutorialManager.Instance.CanPlaceToCell(closestCell);
            }
        }

        if (canPlace)
        {
            AnimateValidPlacement(closestCell);
        }
        else
        {
            AnimateInvalidReturn();
        }
    }

    private void AnimateValidPlacement(Cell targetCell)
    {
        if (selectedPiece == null || selectedTransform == null || targetCell == null)
        {
            ClearSelection();
            return;
        }

        isDragging = false;
        isDropAnimating = true;

        KillTweens();

        RingPiece pieceToPlace = selectedPiece;
        Transform pieceTransform = selectedTransform;

        Vector3 finalPosition = new Vector3(
            targetCell.transform.position.x,
            dragPlaneY,
            targetCell.transform.position.z
        );

        dropSequence = DOTween.Sequence();
        dropSequence.SetUpdate(false);

        dropSequence.Append(
            pieceTransform.DOMove(finalPosition, placementDuration)
                .SetEase(placementEase)
        );

        dropSequence.Join(
            pieceTransform.DORotateQuaternion(selectedStartRotation, placementDuration)
                .SetEase(placementEase)
        );

        dropSequence.Join(
            pieceTransform.DOScale(selectedStartScale, placementDuration)
                .SetEase(Ease.OutSine)
        );

        dropSequence.OnComplete(() =>
        {
            currentLiftHeight = 0f;
            currentScaleMultiplier = 1f;

            if (pieceToPlace != null && targetCell != null)
            {
                pieceTransform.position = finalPosition;
                pieceTransform.rotation = selectedStartRotation;
                pieceTransform.localScale = selectedStartScale;

                targetCell.PlacePiece(pieceToPlace);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPlace();
                }

                if (FirstMoveTutorialManager.Instance != null)
                {
                    FirstMoveTutorialManager.Instance.NotifyPiecePlaced(targetCell);
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPiecePlaced();
                }
            }

            isDropAnimating = false;
            ClearSelection();
        });
    }

    private void AnimateInvalidReturn()
    {
        if (selectedTransform == null)
        {
            ClearSelection();
            return;
        }

        isDragging = false;
        isDropAnimating = true;

        KillTweens();

        Transform pieceTransform = selectedTransform;

        Vector3 returnPosition = selectedStartPosition;

        if (!returnToStartIfInvalid)
        {
            returnPosition = new Vector3(
                pieceTransform.position.x,
                dragPlaneY,
                pieceTransform.position.z
            );
        }

        dropSequence = DOTween.Sequence();
        dropSequence.SetUpdate(false);

        dropSequence.Append(
            pieceTransform.DOMove(returnPosition, invalidReturnDuration)
                .SetEase(invalidReturnEase)
        );

        dropSequence.Join(
            pieceTransform.DORotateQuaternion(selectedStartRotation, invalidReturnDuration)
                .SetEase(invalidReturnEase)
        );

        dropSequence.Join(
            pieceTransform.DOScale(selectedStartScale, invalidReturnDuration)
                .SetEase(Ease.InOutSine)
        );

        dropSequence.Join(
            DOTween.To(
                () => currentLiftHeight,
                value => currentLiftHeight = value,
                0f,
                invalidReturnDuration
            ).SetEase(invalidReturnEase)
        );

        dropSequence.Join(
            DOTween.To(
                () => currentScaleMultiplier,
                value => currentScaleMultiplier = value,
                1f,
                invalidReturnDuration
            ).SetEase(invalidReturnEase)
        );

        dropSequence.OnComplete(() =>
        {
            currentLiftHeight = 0f;
            currentScaleMultiplier = 1f;

            if (pieceTransform != null)
            {
                pieceTransform.position = returnPosition;
                pieceTransform.rotation = selectedStartRotation;
                pieceTransform.localScale = selectedStartScale;
            }

            isDropAnimating = false;
            ClearSelection();

            if (FirstMoveTutorialManager.Instance != null)
            {
                FirstMoveTutorialManager.Instance.NotifyInvalidPlacement();
            }
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

    private void KillTweens()
    {
        KillLiftTween();
        KillScaleTween();

        if (dropSequence != null)
        {
            dropSequence.Kill();
            dropSequence = null;
        }

        if (selectedTransform != null)
        {
            selectedTransform.DOKill();
        }
    }

    private void KillLiftTween()
    {
        if (liftTween != null)
        {
            liftTween.Kill();
            liftTween = null;
        }
    }

    private void KillScaleTween()
    {
        if (scaleTween != null)
        {
            scaleTween.Kill();
            scaleTween = null;
        }
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        selectedTransform = null;
        isDragging = false;
    }
}