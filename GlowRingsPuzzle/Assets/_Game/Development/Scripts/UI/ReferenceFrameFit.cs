using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ReferenceFrameFit : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    private readonly Vector3[] corners = new Vector3[4];
    public void Configure(Camera camera) { gameplayCamera = camera; Apply(); }
    public void Apply()
    {
        RectTransform rect = (RectTransform)transform;
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(1080, 1920);
        rect.anchoredPosition = Vector2.zero;
        float scale = Mathf.Min(parent.rect.width / 1080f, parent.rect.height / 1920f);
        rect.localScale = Vector3.one * Mathf.Max(0.001f, scale);
        if (gameplayCamera != null && Screen.width > 0 && Screen.height > 0)
        {
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            gameplayCamera.rect = new Rect(min.x / Screen.width, min.y / Screen.height,
                (max.x - min.x) / Screen.width, (max.y - min.y) / Screen.height);
        }
    }
    private void OnEnable() { Apply(); }
    private void LateUpdate() { Apply(); }
}
