using UnityEngine;

public class Ring : MonoBehaviour
{
    [SerializeField] private RingLayer layer;
    [SerializeField] private Transform modelParent;

    private RingColorType colorType;
    private bool initialized;
    private GameObject currentModel;

    public RingLayer Layer => layer;
    public RingColorType ColorType => colorType;

    private void OnEnable()
    {
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    private void OnDisable()
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
    }

    public void Initialize(RingLayer newLayer, RingColorType newColorType, ThemeData themeData)
    {
        layer = newLayer;
        colorType = newColorType;
        initialized = true;
        RefreshVisual(themeData);
    }

    public void RefreshVisual(ThemeData themeData)
    {
        if (!initialized || themeData == null)
            return;

        ClearModel();

        GameObject modelPrefab = themeData.RingModelData != null ? themeData.RingModelData.GetModel(layer) : null;
        RingColorData colorData = themeData.GetColorData(colorType);

        CreateModel(modelPrefab);
        ApplyMaterial(colorData);
        transform.localScale = Vector3.one;
    }

    private void OnThemeChanged(ThemeData themeData)
    {
        RefreshVisual(themeData);
    }

    private void CreateModel(GameObject modelPrefab)
    {
        if (modelPrefab == null)
        {
            Debug.LogError("Ring model prefab eksik. Layer: " + layer);
            return;
        }

        Transform parent = modelParent != null ? modelParent : transform;
        currentModel = Instantiate(modelPrefab, parent);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one;
    }

    private void ApplyMaterial(RingColorData colorData)
    {
        if (colorData == null || colorData.Material == null)
        {
            Debug.LogError("Ring color/material eksik. Color: " + colorType);
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterial = colorData.Material;
    }

    private void ClearModel()
    {
        if (currentModel == null)
            return;

        Destroy(currentModel);
        currentModel = null;
    }
}
