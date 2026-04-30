using UnityEngine;

public class Ring : MonoBehaviour
{
    [Header("Ring Data")]
    [SerializeField] private RingLayer layer;
    [SerializeField] private RingColorType colorType;

    [Header("Runtime Model")]
    [SerializeField] private Transform modelParent;
    [SerializeField] private GameObject currentModel;

    private Renderer[] currentRenderers;

    public RingLayer Layer
    {
        get { return layer; }
    }

    public RingColorType ColorType
    {
        get { return colorType; }
    }

    private void Awake()
    {
        if (modelParent == null)
        {
            modelParent = transform;
        }
    }

    private void OnEnable()
    {
        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
        }

        ApplyCurrentTheme();
    }

    private void OnDisable()
    {
        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
        }
    }

    public void Initialize(RingLayer newLayer, RingColorType newColorType)
    {
        layer = newLayer;
        colorType = newColorType;

        gameObject.SetActive(true);

        ApplyCurrentTheme();
    }

    public void Initialize(RingLayer newLayer, RingColorType newColorType, bool isActive)
    {
        layer = newLayer;
        colorType = newColorType;

        gameObject.SetActive(isActive);

        if (!isActive)
        {
            return;
        }

        ApplyCurrentTheme();
    }

    public void SetLayer(RingLayer newLayer)
    {
        layer = newLayer;

        ApplyCurrentTheme();
    }

    public void SetColor(RingColorType newColorType)
    {
        colorType = newColorType;

        ApplyCurrentTheme();
    }

    public void SetActiveState(bool isActive)
    {
        gameObject.SetActive(isActive);

        if (isActive)
        {
            ApplyCurrentTheme();
        }
    }

    private void OnThemeChanged(ThemeData themeData)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        ApplyTheme(themeData);
    }

    private void ApplyCurrentTheme()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (ThemeManager.Instance == null)
        {
            return;
        }

        ThemeData currentTheme = ThemeManager.Instance.CurrentTheme;

        if (currentTheme == null)
        {
            return;
        }

        ApplyTheme(currentTheme);
    }

    private void ApplyTheme(ThemeData themeData)
    {
        if (themeData == null)
        {
            return;
        }

        ApplyModel(themeData);
        ApplyMaterial(themeData);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void ApplyModel(ThemeData themeData)
    {
        if (themeData.RingModelData == null)
        {
            return;
        }

        GameObject modelPrefab = themeData.RingModelData.GetModel(layer);

        if (modelPrefab == null)
        {
            Debug.LogWarning("Ring: Model prefab bulunamadı. Layer: " + layer);
            return;
        }

        if (currentModel != null)
        {
            DestroyImmediateSafe(currentModel);
            currentModel = null;
        }

        currentModel = Instantiate(modelPrefab, modelParent);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one;

        currentRenderers = currentModel.GetComponentsInChildren<Renderer>(true);
    }

    private void ApplyMaterial(ThemeData themeData)
    {
        RingColorData colorData = themeData.GetColorData(colorType);

        if (colorData == null || colorData.Material == null)
        {
            Debug.LogWarning("Ring: Material bulunamadı. ColorType: " + colorType);
            return;
        }

        if (currentRenderers == null || currentRenderers.Length == 0)
        {
            currentRenderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < currentRenderers.Length; i++)
        {
            Renderer targetRenderer = currentRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.sharedMaterial = colorData.Material;
        }
    }

    private void DestroyImmediateSafe(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}