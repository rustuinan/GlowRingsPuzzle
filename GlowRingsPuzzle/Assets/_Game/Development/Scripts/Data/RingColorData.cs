using UnityEngine;

[CreateAssetMenu(fileName = "RingColorData", menuName = "Glow Rings/Ring Color Data")]
public class RingColorData : ScriptableObject
{
    [SerializeField] private RingColorType colorType;
    [SerializeField] private Material material;

    public RingColorType ColorType => colorType;
    public Material Material => material;
}
