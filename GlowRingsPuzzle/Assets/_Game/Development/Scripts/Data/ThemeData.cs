using UnityEngine;

[CreateAssetMenu(fileName = "ThemeData", menuName = "Glow Rings/Theme Data")]
public class ThemeData : ScriptableObject
{
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