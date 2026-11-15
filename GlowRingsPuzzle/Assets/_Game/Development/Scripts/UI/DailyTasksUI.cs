using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DailyTasksUI : MonoBehaviour
{
    private static readonly Color Ink = new Color32(20, 17, 45, 255);
    private static readonly Color Violet = new Color32(131, 111, 181, 255);
    private static readonly Color Muted = new Color32(190, 181, 218, 255);
    private static readonly Color Cyan = new Color32(139, 224, 247, 255);
    private static readonly Color Gold = new Color32(246, 209, 133, 255);
    private static readonly Color Mint = new Color32(136, 223, 202, 255);
    private static readonly Color CardBorder = new Color32(93, 75, 143, 255);
    private static readonly string[] Titles = { "Place 15 pieces", "Clear 18 rings in matches", "Earn 600 points" };
    private static readonly Color[] CardFills = { new Color32(28, 42, 69, 255), new Color32(44, 30, 76, 255), new Color32(50, 33, 64, 255) };
    private readonly TMP_Text[] progress = new TMP_Text[3];
    private readonly TMP_Text[] statuses = new TMP_Text[3];
    private readonly TMP_Text[] actions = new TMP_Text[3];
    private readonly Image[] fills = new Image[3];
    private readonly RoundedPanelGraphic[] cards = new RoundedPanelGraphic[3];
    private readonly Button[] buttons = new Button[3];
    private TMP_FontAsset font;
    private TMP_Text timer, feedback;
    private float nextRefresh;
    private long displayedDay;

    public static DailyTasksUI Open(Transform parent, TMP_FontAsset font)
    {
        var root = Rect("Daily Tasks Window", parent, 0, 0, 0, 0);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .48f);
        var ui = root.gameObject.AddComponent<DailyTasksUI>();
        var rewardArt = Resources.Load<DailyRewardConfig>("DailyRewardConfig");
        ui.font = rewardArt != null && rewardArt.UIFont != null ? rewardArt.UIFont : font;
        ui.Build(rewardArt != null ? rewardArt.CoinIcon : null, Resources.Load<DailyTasksArt>("DailyTasksArt"));
        ui.Reopen();
        return ui;
    }

    public void Reopen()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        feedback.text = "";
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1f;
        Refresh();
    }

    private void Build(Sprite coinIcon, DailyTasksArt art)
    {
        var frame = Rect("Daily Tasks Reference Frame", transform, 0, 0, 1080, 1920);
        frame.gameObject.AddComponent<ReferenceFrameFit>();
        var panel = Box("Daily Tasks Panel", frame, 0, 0, 1010, 1260, Ink, Violet, 44);
        Label(panel, "DAILY TASKS", -60, 541, 800, 88, 56, Gold, TextAlignmentOptions.MidlineLeft);
        MakeButton("Close Daily Tasks", panel, 398, 541, 126, 108, Close, out var close);
        close.text = "X";

        for (int i = 0; i < Titles.Length; i++)
        {
            int index = i;
            var task = (DailyTasksSave.Task)i;
            var card = Box(Titles[i], panel, 0, 344 - 310 * i, 922, 292, CardFills[i], CardBorder, 28);
            cards[i] = card.GetComponent<RoundedPanelGraphic>();
            bool isPlacementTask = task == DailyTasksSave.Task.PlacePieces;
            bool isRingTask = task == DailyTasksSave.Task.ClearRings;
            Sprite icon = art == null ? null : isRingTask ? art.Ring : art.Trophy;
            bool hasIcon = isPlacementTask || icon != null;
            if (hasIcon)
            {
                var iconTile = Box("Task Icon Tile", card, -364, 88, 112, 76, Ink, CardBorder, 18);
                if (isPlacementTask)
                    Rect("Nested Blue Rings", iconTile, 0, 0, 68, 68).gameObject.AddComponent<DailyTaskRingIcon>().raycastTarget = false;
                else
                    AddIcon("Task Icon", iconTile, icon, 0, 57, isRingTask ? new Color32(195, 156, 255, 255) : Color.white);
            }
            float titleWidth = hasIcon ? 678 : 840;
            Label(card, Titles[i], 420 - titleWidth * 0.5f, 88, titleWidth, 74, 40, Color.white, TextAlignmentOptions.MidlineLeft);

            var reward = MakeButton("Claim " + i, card, 250, -84, 340, 88, () => Claim(index), out actions[i]);
            buttons[i] = reward;
            var rewardRect = (RectTransform)reward.transform;
            if (coinIcon != null)
                AddIcon("Reward Coin", rewardRect, coinIcon, -119, 45, Color.white);
            actions[i].rectTransform.anchoredPosition = new Vector2(coinIcon != null ? 29 : 0, 0);
            actions[i].rectTransform.sizeDelta = new Vector2(coinIcon != null ? 244 : 308, 74);

            var bar = Box("Progress Track", card, 0, -14, 840, 24, Ink, CardBorder, 12);
            var fill = Rect("Progress Fill", bar, 0, 0, 0, 0);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(4, 4);
            fills[i] = fill.gameObject.AddComponent<Image>();
            fills[i].color = Cyan;
            fills[i].raycastTarget = false;
            progress[i] = Label(card, "", 0, 25, 840, 42, 33, Cyan, TextAlignmentOptions.MidlineLeft);
            statuses[i] = Label(card, "", -215, -84, 410, 54, 30, Muted, TextAlignmentOptions.MidlineLeft);
        }

        timer = Label(panel, "", 0, -484, 868, 54, 30, Muted);
        feedback = Label(panel, "", 0, -548, 868, 54, 32, Mint);
    }

    private void Refresh()
    {
        var now = DateTime.Now;
        var state = DailyTasksSave.Read(now);
        if (displayedDay != state.Day)
        {
            displayedDay = state.Day;
            feedback.text = "";
        }
        var remaining = now.Date.AddDays(1) - now;
        timer.text = GameLanguage.Choose("RESETS IN", "YENİLENMEYE") + "  " + remaining.ToString(@"hh\:mm\:ss");
        for (int i = 0; i < buttons.Length; i++)
        {
            var task = (DailyTasksSave.Task)i;
            int goal = DailyTasksSave.Goal(task);
            bool claimed = DailyTasksSave.WasClaimed(state, task);
            int value = state.Progress[i];
            bool ready = value >= goal && !claimed;
            progress[i].text = value.ToString("N0", GameLanguage.Culture) + " / " + goal.ToString("N0", GameLanguage.Culture);
            var accent = claimed ? Mint : ready ? Gold : Cyan;
            progress[i].color = accent;
            fills[i].color = accent;
            var fill = fills[i].rectTransform;
            float fillWidth = ((RectTransform)fill.parent).rect.width - 8f;
            fill.offsetMax = new Vector2(-4f - fillWidth * (1f - Mathf.Clamp01((float)value / goal)), -4f);
            fills[i].enabled = value > 0;
            cards[i].SetStyle(CardFills[i], ready ? Gold : CardBorder, 28);
            var button = buttons[i];
            button.interactable = ready;
            ((RoundedPanelGraphic)button.targetGraphic).SetStyle(ready ? Gold : new Color32(62, 43, 64, 255), Gold, 22);
            string amount = DailyTasksSave.Reward(task).ToString("N0", GameLanguage.Culture);
            actions[i].text = ready ? GameLanguage.Text("CLAIM") + "  " + amount : amount + " " + GameLanguage.Text("COINS");
            actions[i].color = ready ? Ink : Gold;
            statuses[i].text = ready ? GameLanguage.Choose("READY", "HAZIR") : GameLanguage.Text(claimed ? "CLAIMED" : "IN PROGRESS");
            statuses[i].color = claimed || ready ? accent : Muted;
        }
    }

    private void Claim(int index)
    {
        int coins = DailyTasksSave.Claim((DailyTasksSave.Task)index, DateTime.Now);
        if (coins > 0)
        {
            AudioManager.Instance?.PlayButtonClickForced();
            feedback.text = GameLanguage.Format("+{0:N0} COINS ADDED!", "+{0:N0} COIN EKLENDİ", coins);
        }
        Refresh();
    }

    private void Close()
    {
        AudioManager.Instance?.PlayButtonClickForced();
        gameObject.SetActive(false);
    }

    private Button MakeButton(string name, Transform parent, float x, float y, float width, float height,
        UnityEngine.Events.UnityAction action, out TMP_Text caption)
    {
        var rect = Box(name, parent, x, y, width, height, new Color32(56, 34, 100, 255), Violet, 22);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<RoundedPanelGraphic>();
        button.targetGraphic.raycastTarget = true;
        var colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(action);
        caption = Label(rect, "", 0, 0, width - 20, height - 12, 34, Color.white);
        return button;
    }

    private static void AddIcon(string name, Transform parent, Sprite sprite, float x, float size, Color tint)
    {
        var icon = Rect(name, parent, x, 0, size, size).gameObject.AddComponent<Image>();
        icon.sprite = sprite;
        icon.color = tint;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private TMP_Text Label(Transform parent, string text, float x, float y, float width, float height,
        float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var label = Rect("Label", parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = GameLanguage.Text(text);
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * .8f;
        label.fontSizeMax = size;
        return label;
    }

    private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height,
        Color fill, Color border, float radius)
    {
        var rect = Rect(name, parent, x, y, width, height);
        var graphic = rect.gameObject.AddComponent<RoundedPanelGraphic>();
        graphic.SetStyle(fill, border, radius);
        graphic.raycastTarget = false;
        return rect;
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
}
