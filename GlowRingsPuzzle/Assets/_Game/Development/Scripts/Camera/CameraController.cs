using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;

    [Header("Framing")]
    [SerializeField] private bool frameOnStart = true;
    [SerializeField] private bool frameContinuously = true;
    [SerializeField] private bool applyInEditor = true;

    [SerializeField, Min(0.1f)]
    private float framingPadding = 1.15f;

    [Header("View")]
    [SerializeField] private Vector3 eulerRotation = new Vector3(70f, 0f, 0f);
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [SerializeField, Min(0f)]
    private float distanceOffset = 1f;

    [SerializeField, Range(10f, 85f)]
    private float perspectiveFov = 35f;

    [Header("Limits")]
    [SerializeField, Min(0.1f)] private float minDistance = 3f;
    [SerializeField, Min(0.1f)] private float maxDistance = 50f;

    [Header("Smoothing")]
    [SerializeField, Min(0f)] private float moveLerpSpeed = 12f;
    [SerializeField, Min(0f)] private float rotationLerpSpeed = 12f;
    [SerializeField, Min(0f)] private float fovLerpSpeed = 12f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color boundsColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Color centerColor = new Color(1f, 0.8f, 0f, 1f);

    private Camera cachedCamera;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetFov;

    private void Awake()
    {
        CacheReferences();

        if (cachedCamera != null)
        {
            cachedCamera.orthographic = false;
            cachedCamera.usePhysicalProperties = false;
        }
    }

    private void Start()
    {
        if (frameOnStart)
            SnapToBoard();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (frameContinuously)
            UpdateTargetsFromBoard();

        SmoothApply();
    }

    private void OnValidate()
    {
        framingPadding = Mathf.Max(0.1f, framingPadding);
        distanceOffset = Mathf.Max(0f, distanceOffset);
        minDistance = Mathf.Max(0.1f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);

        if (!Application.isPlaying && applyInEditor)
            SnapToBoard();
    }

    [ContextMenu("Snap To Board")]
    public void SnapToBoard()
    {
        CacheReferences();
        UpdateTargetsFromBoard();

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        if (cachedCamera != null)
            cachedCamera.fieldOfView = targetFov;
    }

    private void CacheReferences()
    {
        if (cachedCamera == null)
            cachedCamera = GetComponent<Camera>();

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();
    }

    private void UpdateTargetsFromBoard()
    {
        CacheReferences();

        if (boardManager == null || cachedCamera == null)
            return;

        cachedCamera.orthographic = false;
        cachedCamera.usePhysicalProperties = false;

        Bounds boardBounds = CalculateBoardBounds();

        Vector3 boardCenter = boardBounds.center;
        Vector2 boardSize = new Vector2(boardBounds.size.x, boardBounds.size.z);

        targetRotation = Quaternion.Euler(eulerRotation);
        targetFov = perspectiveFov;

        float requiredDistance = CalculateRequiredDistance(boardSize, targetFov, cachedCamera.aspect);
        float finalDistance = Mathf.Clamp(requiredDistance + distanceOffset, minDistance, maxDistance);

        Vector3 backwardOffset = -(targetRotation * Vector3.forward) * finalDistance;
        targetPosition = boardCenter + backwardOffset + worldOffset;
    }

    private Bounds CalculateBoardBounds()
    {
        Cell[] cells = boardManager.Cells;

        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

        if (cells != null)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null)
                    continue;

                Vector3 position = cells[i].transform.position;

                if (!hasBounds)
                {
                    bounds = new Bounds(position, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(position);
                }
            }
        }

        if (!hasBounds)
            bounds = new Bounds(Vector3.zero, new Vector3(4f, 0f, 4f));

        float minSize = 0.01f;

        if (bounds.size.x < minSize || bounds.size.z < minSize)
        {
            bounds.Expand(new Vector3(4f, 0f, 4f));
        }

        return bounds;
    }

    private float CalculateRequiredDistance(Vector2 boardSize, float verticalFov, float aspect)
    {
        float paddedWidth = Mathf.Max(0.01f, boardSize.x * framingPadding);
        float paddedHeight = Mathf.Max(0.01f, boardSize.y * framingPadding);

        float verticalHalfFovRad = verticalFov * 0.5f * Mathf.Deg2Rad;
        float horizontalHalfFovRad = Mathf.Atan(Mathf.Tan(verticalHalfFovRad) * Mathf.Max(0.01f, aspect));

        float distanceByHeight = (paddedHeight * 0.5f) / Mathf.Tan(verticalHalfFovRad);
        float distanceByWidth = (paddedWidth * 0.5f) / Mathf.Tan(horizontalHalfFovRad);

        return Mathf.Max(distanceByHeight, distanceByWidth);
    }

    private void SmoothApply()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * moveLerpSpeed
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationLerpSpeed
        );

        if (cachedCamera != null)
        {
            cachedCamera.fieldOfView = Mathf.Lerp(
                cachedCamera.fieldOfView,
                targetFov,
                Time.deltaTime * fovLerpSpeed
            );
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        CacheReferences();

        if (boardManager == null)
            return;

        Bounds bounds = CalculateBoardBounds();

        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube(
            bounds.center,
            new Vector3(bounds.size.x * framingPadding, 0.05f, bounds.size.z * framingPadding)
        );

        Gizmos.color = centerColor;
        Gizmos.DrawSphere(bounds.center, 0.08f);
        Gizmos.DrawLine(transform.position, bounds.center);
    }
}