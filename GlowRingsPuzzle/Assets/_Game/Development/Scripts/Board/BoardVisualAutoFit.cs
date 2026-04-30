using UnityEngine;

[ExecuteAlways]
public class BoardVisualAutoFit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boardVisualRoot;
    [SerializeField] private Cell[] cells;

    [Header("Auto Collect")]
    [SerializeField] private bool collectCellsFromChildren = true;
    [SerializeField] private Transform cellParent;

    [Header("Fit Settings")]
    [SerializeField] private bool autoFitInEditor = true;
    [SerializeField] private bool autoFitOnStart = true;

    [Min(0f)]
    [SerializeField] private float paddingX = 0.55f;

    [Min(0f)]
    [SerializeField] private float paddingZ = 0.55f;

    [SerializeField] private float boardYOffset = -0.035f;

    [Header("Scale Settings")]
    [SerializeField] private bool scaleBoardToCells = true;
    [SerializeField] private bool preserveYScale = true;

    [Min(0.01f)]
    [SerializeField] private float fallbackBoardWidth = 1f;

    [Min(0.01f)]
    [SerializeField] private float fallbackBoardDepth = 1f;

    [Header("Material")]
    [SerializeField] private Material boardMaterial;
    [SerializeField] private bool applyMaterialInEditor = true;

    private void Start()
    {
        if (Application.isPlaying && autoFitOnStart)
        {
            ApplyBoardVisualLayout();
        }
    }

    private void OnValidate()
    {
        if (!autoFitInEditor)
        {
            return;
        }

        ApplyBoardVisualLayout();
    }

    [ContextMenu("Apply Board Visual Layout")]
    public void ApplyBoardVisualLayout()
    {
        if (boardVisualRoot == null)
        {
            return;
        }

        if (collectCellsFromChildren)
        {
            CollectCells();
        }

        if (cells == null || cells.Length == 0)
        {
            return;
        }

        Bounds cellBounds;
        if (!TryGetCellBounds(out cellBounds))
        {
            return;
        }

        Vector3 center = cellBounds.center;
        Vector3 targetPosition = new Vector3(center.x, center.y + boardYOffset, center.z);
        boardVisualRoot.position = targetPosition;

        if (scaleBoardToCells)
        {
            FitBoardScale(cellBounds);
        }

        if (boardMaterial != null && (Application.isPlaying || applyMaterialInEditor))
        {
            ApplyBoardMaterial(boardMaterial);
        }
    }

    private void CollectCells()
    {
        Transform targetParent = cellParent;

        if (targetParent == null)
        {
            targetParent = transform;
        }

        cells = targetParent.GetComponentsInChildren<Cell>(true);
    }

    private bool TryGetCellBounds(out Bounds bounds)
    {
        bounds = new Bounds();

        bool hasAnyCell = false;

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            Vector3 position = cell.transform.position;

            if (!hasAnyCell)
            {
                bounds = new Bounds(position, Vector3.zero);
                hasAnyCell = true;
            }
            else
            {
                bounds.Encapsulate(position);
            }
        }

        return hasAnyCell;
    }

    private void FitBoardScale(Bounds cellBounds)
    {
        Bounds visualBounds;
        if (!TryGetBoardVisualBounds(out visualBounds))
        {
            return;
        }

        float targetWidth = Mathf.Max(0.01f, cellBounds.size.x + paddingX * 2f);
        float targetDepth = Mathf.Max(0.01f, cellBounds.size.z + paddingZ * 2f);

        float currentWidth = Mathf.Max(0.01f, visualBounds.size.x);
        float currentDepth = Mathf.Max(0.01f, visualBounds.size.z);

        if (currentWidth <= 0.01f)
        {
            currentWidth = fallbackBoardWidth;
        }

        if (currentDepth <= 0.01f)
        {
            currentDepth = fallbackBoardDepth;
        }

        Vector3 currentScale = boardVisualRoot.localScale;

        float scaleXMultiplier = targetWidth / currentWidth;
        float scaleZMultiplier = targetDepth / currentDepth;

        Vector3 newScale = currentScale;
        newScale.x = currentScale.x * scaleXMultiplier;
        newScale.z = currentScale.z * scaleZMultiplier;

        if (!preserveYScale)
        {
            float average = (scaleXMultiplier + scaleZMultiplier) * 0.5f;
            newScale.y = currentScale.y * average;
        }

        boardVisualRoot.localScale = newScale;
    }

    private bool TryGetBoardVisualBounds(out Bounds bounds)
    {
        bounds = new Bounds();

        if (boardVisualRoot == null)
        {
            return false;
        }

        Renderer[] renderers = boardVisualRoot.GetComponentsInChildren<Renderer>(true);

        bool hasRenderer = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererTarget = renderers[i];

            if (rendererTarget == null)
            {
                continue;
            }

            if (!hasRenderer)
            {
                bounds = rendererTarget.bounds;
                hasRenderer = true;
            }
            else
            {
                bounds.Encapsulate(rendererTarget.bounds);
            }
        }

        return hasRenderer;
    }

    public void ApplyBoardMaterial(Material material)
    {
        if (boardVisualRoot == null || material == null)
        {
            return;
        }

        Renderer[] renderers = boardVisualRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererTarget = renderers[i];

            if (rendererTarget == null)
            {
                continue;
            }

            rendererTarget.sharedMaterial = material;
        }
    }
}