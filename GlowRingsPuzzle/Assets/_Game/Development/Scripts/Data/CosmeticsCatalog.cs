using UnityEngine;

[CreateAssetMenu(menuName = "Glow Rings/Cosmetics Catalog")]
public sealed class CosmeticsCatalog : ScriptableObject
{
    public ThemeData[] Themes;
    public GameObject PreviewBoard;
    private static CosmeticsCatalog loaded;
    public static CosmeticsCatalog Load()
    {
        if (loaded == null) loaded = Resources.Load<CosmeticsCatalog>("CosmeticsCatalog");
        return loaded;
    }
    public ThemeData FindTheme(string id)
    {
        if (Themes != null) foreach (var theme in Themes) if (theme != null && theme.Id == id) return theme;
        return null;
    }
}
