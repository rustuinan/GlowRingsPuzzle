using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PieceSpawner pieceSpawner;
    [SerializeField] private float placeDistance = 1.2f;
    [SerializeField] private LayerMask draggableLayer;

    [Header("Mobile Drag Offset")]
    [SerializeField] private bool useDragOffset = true;
    [SerializeField] private Vector3 dragWorldOffset = new Vector3(0f, 0f, 0.65f);

    private RingPiece selectedPiece;
    private Plane dragPlane;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();

        if (pieceSpawner == null)
            pieceSpawner = FindObjectOfType<PieceSpawner>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (Input.GetMouseButtonDown(0))
            TrySelectPiece();

        if (Input.GetMouseButton(0))
            DragSelectedPiece();

        if (Input.GetMouseButtonUp(0))
            DropSelectedPiece();
    }

    private void TrySelectPiece()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, draggableLayer))
            return;

        RingPiece piece = hit.collider.GetComponentInParent<RingPiece>();

        if (piece == null || piece.IsPlaced)
            return;

        selectedPiece = piece;
        dragPlane = new Plane(Vector3.up, selectedPiece.transform.position);
    }

    private void DragSelectedPiece()
    {
        if (selectedPiece == null || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPosition = ray.GetPoint(enter);

            if (useDragOffset)
                worldPosition += dragWorldOffset;

            selectedPiece.transform.position = worldPosition;
        }
    }

    private void DropSelectedPiece()
    {
        if (selectedPiece == null)
            return;

        Cell targetCell = boardManager.GetClosestCell(selectedPiece.transform.position, placeDistance);

        if (targetCell != null && targetCell.CanPlace(selectedPiece))
        {
            targetCell.PlacePiece(selectedPiece);
            pieceSpawner.ForgetCurrentPiece();
            GameManager.Instance.OnPiecePlaced();
        }
        else
        {
            selectedPiece.ReturnToStart();
        }

        selectedPiece = null;
    }
}