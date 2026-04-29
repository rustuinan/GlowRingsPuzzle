using UnityEngine;

[CreateAssetMenu(fileName = "RingModelData", menuName = "Glow Rings/Ring Model Data")]
public class RingModelData : ScriptableObject
{
    [Header("Model Prefabs")]
    [SerializeField] private GameObject outerModelPrefab;
    [SerializeField] private GameObject middleModelPrefab;
    [SerializeField] private GameObject innerModelPrefab;

    public GameObject OuterModelPrefab => outerModelPrefab;
    public GameObject MiddleModelPrefab => middleModelPrefab;
    public GameObject InnerModelPrefab => innerModelPrefab;

    public bool IsValid()
    {
        return outerModelPrefab != null &&
               middleModelPrefab != null &&
               innerModelPrefab != null;
    }

    public GameObject GetModel(RingLayer layer)
    {
        switch (layer)
        {
            case RingLayer.Outer:
                return outerModelPrefab;

            case RingLayer.Middle:
                return middleModelPrefab;

            case RingLayer.Inner:
                return innerModelPrefab;

            default:
                return null;
        }
    }
}