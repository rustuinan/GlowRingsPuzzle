using UnityEngine;

[CreateAssetMenu(fileName = "RingModelData", menuName = "Glow Rings/Ring Model Data")]
public class RingModelData : ScriptableObject
{
    [SerializeField] private GameObject outerRingModel;
    [SerializeField] private GameObject middleRingModel;
    [SerializeField] private GameObject innerRingModel;

    public GameObject GetModel(RingLayer layer)
    {
        switch (layer)
        {
            case RingLayer.Outer:
                return outerRingModel;
            case RingLayer.Middle:
                return middleRingModel;
            case RingLayer.Inner:
                return innerRingModel;
            default:
                return null;
        }
    }

    public bool IsValid()
    {
        return outerRingModel != null && middleRingModel != null && innerRingModel != null;
    }
}
