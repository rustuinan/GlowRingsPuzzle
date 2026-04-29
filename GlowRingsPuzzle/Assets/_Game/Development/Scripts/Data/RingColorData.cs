using UnityEngine;

[CreateAssetMenu(fileName = "RingColorData", menuName = "Glow Rings/Ring Color Data")]
public class RingColorData : ScriptableObject
{
    [Header("Color")]
    [SerializeField] private RingColorType colorType;
    [SerializeField] private Material material;

    public RingColorType ColorType => colorType;
    public Material Material => material;

    public bool IsValid()
    {
        return material != null;
    }
}