using UnityEngine;

[ExecuteAlways]
public class BoardModelCellLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boardVisualRoot;
    [SerializeField] private Cell[] cells = new Cell[9];

    [Header("Auto Collect")]
    [SerializeField] private bool autoFindBoardVisual = true;
    [SerializeField] private bool autoCollectCells = true;

    [Header("Apply Settings")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyInEditorOnValidate = false;

    [Header("Board Inner Area")]
    [Tooltip("Board modelinin kenar kalınlığını içeri almak için X padding.")]
    [SerializeField] private float innerPaddingX = 0.38f;

    [Tooltip("Board modelinin kenar kalınlığını içeri almak için Z padding.")]
    [SerializeField] private float innerPaddingZ = 0.38f;

    [Header("Cell Placement")]
    [SerializeField] private float cellYOffsetFromBoardTop = 0.035f;
    [SerializeField] private Vector3 extraWorldOffset = Vector3.zero;

    [Header("Index Direction")]
    [Tooltip("Açıkken Cell_0 üst sol, Cell_8 alt sağ olur.")]
    [SerializeField] private bool indexZeroIsTopLeft = true;

    [Header("Cell Design")]
    [SerializeField] private bool applyCellEditorDesign = true;
    [SerializeField] private Vector3 cellVisualScale = Vector3.one;
    [SerializeField] private Vector3 ringParentLocalPosition = new Vector3(0f, 0.13f, 0f);

    private void Start()
    {
        if (Application.isPlaying && applyOnStart)
        {
            ApplyLayout();
        }
    }

    private void OnValidate()
    {
        if (!applyInEditorOnValidate)
        {
            return;
        }

        ApplyLayout();
    }

    [ContextMenu("Apply Layout")]
    public void ApplyLayout()
    {
        FindReferencesIfNeeded();

        if (boardVisualRoot == null)
        {
            Debug.LogWarning("BoardModelCellLayout: BoardVisualRoot atanmadı.");
            return;
        }

        if (cells == null || cells.Length != 9)
        {
            Debug.LogWarning("BoardModelCellLayout: Cells array 9 eleman olmalı.");
            return;
        }

        Bounds boardBounds;
        if (!TryGetBoardBounds(out boardBounds))
        {
            Debug.LogWarning("BoardModelCellLayout: BoardVisual altında Renderer bulunamadı.");
            return;
        }

        float minX = boardBounds.min.x + innerPaddingX;
        float maxX = boardBounds.max.x - innerPaddingX;

        float minZ = boardBounds.min.z + innerPaddingZ;
        float maxZ = boardBounds.max.z - innerPaddingZ;

        float targetY = boardBounds.max.y + cellYOffsetFromBoardTop;

        for (int i = 0; i < 9; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                Debug.LogWarning("BoardModelCellLayout: Cell_" + i + " atanmadı.");
                continue;
            }

            int column = i % 3;
            int row = i / 3;

            float xT = column / 2f;
            float zT = row / 2f;

            float x = Mathf.Lerp(minX, maxX, xT);

            float z;
            if (indexZeroIsTopLeft)
            {
                z = Mathf.Lerp(maxZ, minZ, zT);
            }
            else
            {
                z = Mathf.Lerp(minZ, maxZ, zT);
            }

            Vector3 targetPosition = new Vector3(x, targetY, z) + extraWorldOffset;
            cell.transform.position = targetPosition;

            if (applyCellEditorDesign)
            {
                cell.ApplyEditorDesign(cellVisualScale, ringParentLocalPosition);
            }
        }
    }

    [ContextMenu("Collect References")]
    public void CollectReferences()
    {
        AutoFindBoardVisual();
        AutoCollectCells();
    }

    private void FindReferencesIfNeeded()
    {
        if (autoFindBoardVisual && boardVisualRoot == null)
        {
            AutoFindBoardVisual();
        }

        if (autoCollectCells)
        {
            AutoCollectCells();
        }
    }

    private void AutoFindBoardVisual()
    {
        Transform found = transform.Find("BoardVisual");

        if (found != null)
        {
            boardVisualRoot = found;
        }
    }

    private void AutoCollectCells()
    {
        Cell[] newCells = new Cell[9];

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child == null)
            {
                continue;
            }

            Cell cell = child.GetComponent<Cell>();

            if (cell == null)
            {
                continue;
            }

            int index = ExtractIndex(child.name);

            if (index >= 0 && index < 9)
            {
                newCells[index] = cell;
            }
        }

        cells = newCells;
    }

    private bool TryGetBoardBounds(out Bounds bounds)
    {
        bounds = new Bounds();

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

    private int ExtractIndex(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return -1;
        }

        for (int i = 0; i <= 8; i++)
        {
            if (objectName.EndsWith("_" + i))
            {
                return i;
            }

            if (objectName.EndsWith(i.ToString()))
            {
                return i;
            }
        }

        return -1;
    }
}