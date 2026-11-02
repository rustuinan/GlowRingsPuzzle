using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class MenuContentLayout : MonoBehaviour
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private LayoutElement hero;
    [SerializeField] private RectTransform visual;
    public void Configure(ScrollRect value, LayoutElement heroElement, RectTransform heroVisual) { scroll = value; hero = heroElement; visual = heroVisual; Apply(); }
    private void LateUpdate() { Apply(); }
    public void Apply()
    {
        if (scroll == null || hero == null) return;
        float height = Mathf.Max(420f, scroll.viewport.rect.height - 778f);
        if (Mathf.Abs(hero.preferredHeight - height) > 0.5f) hero.preferredHeight = height;
        if (visual != null) visual.sizeDelta = Vector2.one * Mathf.Min(780f, height - 24f);
    }
}
