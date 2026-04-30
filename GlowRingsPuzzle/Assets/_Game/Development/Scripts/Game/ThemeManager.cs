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

        if (!HasRequiredStartTheme())
        {
            Debug.LogError("ThemeManager: RequiredScore = 0 olan bir ThemeData yok. Oyun başlatılmamalı.");
            enabled = false;
            return;
        }

        UpdateThemeByScore(0);
    }

    private void FindMissingReferences()
    {
        if (boardVisualAutoFit == null)
        {
            boardVisualAutoFit = FindObjectOfType<BoardVisualAutoFit>();
        }
    }

    public void UpdateThemeByScore(int score)
    {
        ThemeData bestTheme = GetThemeForScore(score);

        if (bestTheme == null)
        {
            Debug.LogError("ThemeManager: Score için uygun theme bulunamadı. Score: " + score);
            return;
        }

        if (currentTheme == bestTheme)
        {
            return;
        }

        ApplyTheme(bestTheme);
    }

    private ThemeData GetThemeForScore(int score)
    {
        if (themes == null || themes.Length == 0)
        {
            return null;
        }

        ThemeData bestTheme = null;
        int bestRequiredScore = int.MinValue;

        for (int i = 0; i < themes.Length; i++)
        {
            ThemeData theme = themes[i];

            if (theme == null)
            {
                continue;
            }

            if (!theme.IsValid())
            {
                Debug.LogWarning("ThemeManager: Geçersiz ThemeData bulundu: " + theme.name);
                continue;
            }

            if (theme.RequiredScore <= score && theme.RequiredScore >= bestRequiredScore)
            {
                bestTheme = theme;
                bestRequiredScore = theme.RequiredScore;
            }
        }

        return bestTheme;
    }

    private bool HasRequiredStartTheme()
    {
        if (themes == null || themes.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < themes.Length; i++)
        {
            ThemeData theme = themes[i];

            if (theme == null)
            {
                continue;
            }

            if (theme.RequiredScore == 0)
            {
                return true;
            }
        }

        return false;
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