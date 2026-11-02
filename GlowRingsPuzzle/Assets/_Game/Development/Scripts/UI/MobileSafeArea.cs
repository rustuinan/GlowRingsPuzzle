using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class MobileSafeArea : MonoBehaviour
{
    public Rect? PreviewSafeArea { get; set; }
    public void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        ApplyRect(PreviewSafeArea ?? Screen.safeArea, new Vector2(Screen.width, Screen.height));
    }
    public void ApplyRect(Rect safe, Vector2 screen)
    {
        RectTransform rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
        rect.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private void OnEnable() { Apply(); }
    private void LateUpdate() { Apply(); }
}
