using UnityEngine;

[CreateAssetMenu(fileName = "ThemeData", menuName = "Glow Rings/Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("Collection / Palette")]
    public string Id = "midnight";
    [Min(0)] public int CoinPrice;
    public bool IsOriginal;
    public Material[] BoardSurfaceMaterials;
    public Color BackgroundTop = new Color(0.08f, 0.03f, 0.22f);
    public Color BackgroundBottom = new Color(0.025f, 0.02f, 0.10f);
    public Color Surface = new Color(0.10f, 0.08f, 0.25f);
    public Color Accent = new Color(0.45f, 0.85f, 1f);
    public Color Secondary = new Color(0.65f, 0.4f, 1f);
    public Color Text = new Color(0.94f, 0.96f, 1f);
    public Color BoardFace = new Color(0.32f, 0.40f, 0.53f);
    public Color BoardFrame = new Color(0.14f, 0.16f, 0.27f);
    public Color BoardGap = new Color(0.08f, 0.10f, 0.19f);
    [Range(0, 5)] public int Atmosphere;
    [Header("Theme")]
    [SerializeField] private string themeName = "Default";
    [SerializeField] private int requiredScore;

    [Header("Ring")]
    [SerializeField] private RingModelData ringModelData;
    [SerializeField] private RingColorData[] ringColors;

    [Header("Board")]
    [SerializeField] private Material boardMaterial;
    [SerializeField] private Material cellMaterial;
    [SerializeField] private Material backgroundMaterial;

    public string ThemeName => themeName;
    public int RequiredScore => requiredScore;
    public RingModelData RingModelData => ringModelData;
    public RingColorData[] RingColors => ringColors;
    public Material BoardMaterial => boardMaterial;
    public Material CellMaterial => cellMaterial;
    public Material BackgroundMaterial => backgroundMaterial;

    public bool IsValid()
    {
        if (ringModelData == null || !ringModelData.IsValid())
            return false;

        if (ringColors == null || ringColors.Length == 0)
            return false;

        for (int i = 0; i < ringColors.Length; i++)
        {
            if (ringColors[i] == null || !ringColors[i].IsValid())
                return false;
        }

        return true;
    }

    public RingColorData GetRandomColor()
    {
        if (ringColors == null || ringColors.Length == 0)
            return null;

        return ringColors[Random.Range(0, ringColors.Length)];
    }

    public RingColorData GetColorData(RingColorType colorType)
    {
        if (ringColors == null)
            return null;

        for (int i = 0; i < ringColors.Length; i++)
        {
            if (ringColors[i] != null && ringColors[i].ColorType == colorType)
                return ringColors[i];
        }

        return null;
    }
}
