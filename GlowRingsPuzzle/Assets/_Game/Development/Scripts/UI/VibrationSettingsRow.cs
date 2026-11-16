using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class VibrationSettingsRow
{
    public static Toggle Create(RectTransform soundRow, TMP_Text soundLabel, float y)
    {
        float width = soundRow.sizeDelta.x;
        var row = Rect("Vibration", soundRow.parent, new Vector2(0, y), soundRow.sizeDelta);
        var background = row.gameObject.AddComponent<RoundedPanelGraphic>();
        var soundBackground = soundRow.GetComponent<RoundedPanelGraphic>();
        background.SetStyle(soundBackground.color, soundBackground.BorderColor, soundBackground.CornerRadius);

        float iconX = ((RectTransform)soundRow.Find("Icon")).anchoredPosition.x;
        var phone = Graphic("Phone", row, new Vector2(iconX, 0), new Vector2(25, 39),
            new Color32(62, 51, 131, 255), Color.white, 6);
        foreach (int side in new[] { -1, 1 })
            Graphic("Vibration Mark", phone.transform, new Vector2(side * 22, 0), new Vector2(5, 22),
                Color.white, Color.white, 2.5f);

        var soundRect = soundLabel.rectTransform;
        float left = soundRect.anchoredPosition.x - soundRect.rect.width * .5f;
        float labelWidth = width * .5f - 200 - left;
        var label = Rect("Label", row, new Vector2(left + labelWidth * .5f, 0), new Vector2(labelWidth, 72))
            .gameObject.AddComponent<TextMeshProUGUI>();
        label.font = GameLanguage.UiFont(soundLabel.font);
        label.fontSize = soundLabel.fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24;
        label.fontSizeMax = soundLabel.fontSize;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = GameLanguage.Text("VIBRATION");
        label.raycastTarget = false;

        var track = Graphic("Track", row, new Vector2(width * .5f - 112, 0), new Vector2(126, 58),
            Color.clear, Color.clear, 28);
        Graphic("Thumb", track.transform, Vector2.zero, new Vector2(43, 43), Color.white,
            new Color32(218, 219, 255, 255), 21);
        var toggle = row.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.transition = Selectable.Transition.None;
        toggle.onValueChanged.AddListener(enabled =>
        {
            GameplayHaptics.Enabled = enabled;
            Refresh(toggle);
            if (enabled) GameplayHaptics.Play();
        });
        Refresh(toggle);
        return toggle;
    }

    public static void Refresh(Toggle toggle)
    {
        bool enabled = GameplayHaptics.Enabled;
        toggle.SetIsOnWithoutNotify(enabled);
        var track = (RoundedPanelGraphic)toggle.targetGraphic;
        track.SetStyle(enabled ? new Color32(71, 65, 169, 255) : new Color32(48, 43, 79, 255),
            enabled ? new Color32(135, 240, 255, 255) : new Color32(107, 101, 147, 255), 28);
        ((RectTransform)track.transform.GetChild(0)).anchoredPosition = new Vector2(enabled ? 32 : -32, 0);
    }

    private static RoundedPanelGraphic Graphic(string name, Transform parent, Vector2 position, Vector2 size,
        Color fill, Color border, float radius)
    {
        var graphic = Rect(name, parent, position, size).gameObject.AddComponent<RoundedPanelGraphic>();
        graphic.SetStyle(fill, border, radius);
        graphic.raycastTarget = false;
        return graphic;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
