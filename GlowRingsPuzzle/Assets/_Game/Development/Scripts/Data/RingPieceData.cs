using UnityEngine;

[CreateAssetMenu(fileName = "RingPieceData", menuName = "Glow Rings/Ring Piece Data")]
public class RingPieceData : ScriptableObject
{
    [SerializeField] private bool hasOuter;
    [SerializeField] private bool hasMiddle;
    [SerializeField] private bool hasInner;

    public bool HasOuter => hasOuter;
    public bool HasMiddle => hasMiddle;
    public bool HasInner => hasInner;

    public bool HasAnyLayer => hasOuter || hasMiddle || hasInner;

    public bool HasLayer(RingLayer layer)
    {
        switch (layer)
        {
            case RingLayer.Outer:
                return hasOuter;
            case RingLayer.Middle:
                return hasMiddle;
            case RingLayer.Inner:
                return hasInner;
            default:
                return false;
        }
    }
}
