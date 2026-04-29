using System;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    public static event Action<ThemeData> ThemeChanged;

    [SerializeField] private ThemeData[] themes;
    [SerializeField] private Renderer boardRenderer;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Renderer backgroundRenderer;

    private ThemeData currentTheme;

    public ThemeData CurrentTheme => currentTheme;

    private void Awake()
    {
        Instance = this;
        UpdateThemeByScore(0);
    }

    public void UpdateThemeByScore(int score)
    {
        ThemeData bestTheme = GetBestThemeForScore(score);

        if (bestTheme == null)
        {
            Debug.LogError("ThemeManager: En az bir ThemeData RequiredScore = 0 olmalı.");
            return;
        }

        if (currentTheme == bestTheme)
            return;

        currentTheme = bestTheme;
        ApplyTheme();

        ThemeChanged?.Invoke(currentTheme);
    }

    public void RefreshThemeByScore(int score)
    {
        UpdateThemeByScore(score);
    }

    private ThemeData GetBestThemeForScore(int score)
    {
        ThemeData bestTheme = null;

        for (int i = 0; i < themes.Length; i++)
        {
            ThemeData theme = themes[i];

            if (theme == null)
                continue;

            if (score >= theme.RequiredScore)
            {
                if (bestTheme == null || theme.RequiredScore > bestTheme.RequiredScore)
                    bestTheme = theme;
            }
        }

        return bestTheme;
    }

    private void ApplyTheme()
    {
        if (currentTheme == null)
            return;

        if (boardRenderer != null && currentTheme.BoardMaterial != null)
            boardRenderer.material = currentTheme.BoardMaterial;

        if (boardManager != null && currentTheme.CellMaterial != null)
            boardManager.ApplyCellMaterial(currentTheme.CellMaterial);

        if (backgroundRenderer != null && currentTheme.BackgroundMaterial != null)
            backgroundRenderer.material = currentTheme.BackgroundMaterial;
    }
}