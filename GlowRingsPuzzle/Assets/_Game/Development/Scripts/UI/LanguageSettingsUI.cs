using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LanguageSettingsUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private static readonly Color Cyan = new Color32(141, 237, 255, 255);
    private static readonly Color Violet = new Color32(137, 116, 204, 255);

    private Action<string> select;
    private TMP_Text value;
    private TMP_FontAsset menuFont;
    private TMP_FontAsset latinFont;
    private TMP_FontAsset chineseFont;
    private TMP_FontAsset cyrillicFont;

    public static LanguageSettingsUI Create(Transform parent, TMP_FontAsset font, Action<string> select)
    {
        var heading = Label(parent, "Language Heading", font, new Vector2(-195, -264), new Vector2(390, 42), 29);
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        heading.text = GameLanguage.Text("LANGUAGE");

        var row = Rect("Language Selector", parent, new Vector2(0, -349), new Vector2(780, 112));
        row.gameObject.AddComponent<RoundedPanelGraphic>().SetStyle(new Color32(39, 29, 91, 255), Violet, 26);
        var selector = row.gameObject.AddComponent<LanguageSettingsUI>();
        selector.select = select;
        selector.menuFont = font;
        selector.latinFont = Resources.Load<TMP_FontAsset>("Shop Readable Turkish SDF");
        selector.chineseFont = Resources.Load<TMP_FontAsset>("Ring Match Chinese SDF");
        selector.cyrillicFont = Resources.Load<TMP_FontAsset>("Ring Match Cyrillic SDF");
        selector.value = Label(row, "Selected Language", font, Vector2.zero, new Vector2(510, 72), 39);
        selector.value.color = Cyan;
        selector.AddArrow("Previous Language", -1);
        selector.AddArrow("Next Language", 1);
        selector.Refresh();
        return selector;
    }

    public void Refresh()
    {
        var option = GameLanguage.Preference;
        var font = option.Code == "zh" ? chineseFont : option.Code == "ru" ? cyrillicFont :
            option.Code == GameLanguage.Automatic ? menuFont : latinFont;
        value.font = font != null ? font : menuFont;
        value.text = GameLanguage.PreferenceName;
    }

    private void Step(int direction)
    {
        var options = GameLanguage.Options;
        string selected = GameLanguage.Preference.Code;
        for (int index = 0; index < options.Count; index++)
        {
            if (options[index].Code != selected) continue;
            select(options[(index + direction + options.Count) % options.Count].Code);
            return;
        }
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        var row = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(row, eventData.pressPosition, eventData.pressEventCamera, out var start) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(row, eventData.position, eventData.pressEventCamera, out var end)) return;
        Vector2 movement = end - start;
        if (Mathf.Abs(movement.x) >= 70f && Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            Step(movement.x < 0f ? 1 : -1);
    }

    private void AddArrow(string name, int direction)
    {
        var hit = Rect(name, transform, new Vector2(direction * 328, 0), new Vector2(108, 88));
        var background = hit.gameObject.AddComponent<RoundedPanelGraphic>();
        background.SetStyle(new Color32(55, 42, 113, 255), Violet, 19);
        var button = hit.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => Step(direction));

        for (int side = -1; side <= 1; side += 2)
        {
            var stroke = Rect("Chevron", hit, new Vector2(0, side * 7), new Vector2(20, 6));
            stroke.localRotation = Quaternion.Euler(0, 0, -direction * side * 45);
            var graphic = stroke.gameObject.AddComponent<Image>();
            graphic.color = Cyan;
            graphic.raycastTarget = false;
        }
    }

    private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = GameLanguage.UiFont(font);
        label.fontSize = fontSize;
        label.fontSizeMin = fontSize * .7f;
        label.fontSizeMax = fontSize;
        label.enableAutoSizing = true;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
}
