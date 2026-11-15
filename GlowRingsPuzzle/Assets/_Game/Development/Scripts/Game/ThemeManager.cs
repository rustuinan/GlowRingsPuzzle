using System;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    public event Action<ThemeData> ThemeChanged;

    [Header("Themes")]
    [SerializeField] private ThemeData[] themes;

    [Header("Scene References")]
    [SerializeField] private BoardVisualAutoFit boardVisualAutoFit;
    [SerializeField] private Renderer boardRendererFallback;
    [SerializeField] private Renderer backgroundRenderer;

    [Header("Runtime")]
    [SerializeField] private ThemeData currentTheme;
    public ThemeData[] Themes => themes;
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void Start() { if (currentTheme != null) ApplyTheme(currentTheme); }

    public ThemeData CurrentTheme
    {
        get { return currentTheme; }
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

        var original = CosmeticsSave.MenuTheme;
        if (original == null || !original.IsValid())
        {
            Debug.LogError("ThemeManager: Original theme is missing or invalid.", this);
            enabled = false;
            return;
        }
        ApplyTheme(original);
    }

    private void FindMissingReferences()
    {
        if (boardVisualAutoFit == null)
        {
            boardVisualAutoFit = FindObjectOfType<BoardVisualAutoFit>();
        }
    }

    private void ApplyTheme(ThemeData theme)
    {
        if (theme == null)
        {
            return;
        }

        currentTheme = theme;

        ApplyBoardMaterial(theme.BoardMaterial);
        ApplyCellMaterial(theme.CellMaterial);
        ApplyBackgroundMaterial(theme.BackgroundMaterial);
        ThemeScenePalette.Apply(gameObject.scene, theme);

        if (ThemeChanged != null)
        {
            ThemeChanged.Invoke(currentTheme);
        }
    }

    private void ApplyBoardMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (boardVisualAutoFit != null)
        {
            boardVisualAutoFit.ApplyBoardMaterial(material);
            return;
        }

        if (boardRendererFallback != null)
        {
            boardRendererFallback.sharedMaterial = material;
        }
    }

    private void ApplyCellMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        Cell[] cells = FindObjectsOfType<Cell>();

        if (cells == null || cells.Length == 0)
        {
            return;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell == null)
            {
                continue;
            }

            cell.ApplyCellMaterial(material);
        }
    }

    private void ApplyBackgroundMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.sharedMaterial = material;
        }
    }

    public RingColorData GetColorData(RingColorType colorType)
    {
        if (currentTheme == null)
        {
            return null;
        }

        return currentTheme.GetColorData(colorType);
    }

    public RingColorData GetRandomColor()
    {
        if (currentTheme == null)
        {
            return null;
        }

        return currentTheme.GetRandomColor();
    }

    public RingModelData GetCurrentRingModelData()
    {
        if (currentTheme == null)
        {
            return null;
        }

        return currentTheme.RingModelData;
    }
}
