using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerProgressUI : MonoBehaviour
{
    private static readonly Color Ink = new Color32(20, 17, 45, 255);
    private static readonly Color Surface = new Color32(31, 26, 60, 255);
    private static readonly Color Violet = new Color32(131, 111, 181, 255);
    private static readonly Color Cyan = new Color32(139, 224, 247, 255);
    private static readonly Color Gold = new Color32(246, 209, 133, 255);
    private static readonly Color Muted = new Color32(199, 191, 224, 255);
    private static readonly Color Mint = new Color32(136, 223, 202, 255);
    private static readonly Color Border = new Color32(93, 75, 143, 255);

    private readonly struct Achievement
    {
        public readonly string Title, GoalText;
        public readonly int Goal;
        public readonly long Value;
        public Achievement(string title, string goalText, int goal, long value)
        { Title = title; GoalText = goalText; Goal = goal; Value = value; }
    }

    private TMP_FontAsset font;
    private PlayerProgressArt art;
    private RectTransform panel, statsPage, achievementsPage;
    private TMP_Text title;
    private Button statsTab, achievementsTab;

    public static PlayerProgressUI Open(Transform parent, TMP_FontAsset font)
    {
        var root = Rect("Player Progress Window", parent, 0, 0, 0, 0);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .48f);
        var ui = root.gameObject.AddComponent<PlayerProgressUI>();
        var rewardArt = Resources.Load<DailyRewardConfig>("DailyRewardConfig");
        ui.font = rewardArt != null && rewardArt.UIFont != null ? rewardArt.UIFont : font;
        ui.art = Resources.Load<PlayerProgressArt>("PlayerProgressArt");
        ui.Build();
        ui.Reopen();
        return ui;
    }

    public void Reopen()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Populate();
        Show(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Build()
    {
        var frame = Rect("Reference Frame", transform, 0, 0, 1080, 1920);
        frame.gameObject.AddComponent<ReferenceFrameFit>();
        panel = Box("Player Progress Panel", frame, 0, 0, 1010, 1560, Ink, Violet, 44);
        title = Label(panel, "", -70, 684, 780, 92, 60, Gold, TextAlignmentOptions.MidlineLeft);
        MakeButton("X", panel, 398, 684, 126, 108, Close);
        statsTab = MakeButton("STATISTICS", panel, -235, 552, 450, 112, () => Show(false), art != null ? art.Statistics : null);
        achievementsTab = MakeButton(GameLanguage.Choose("BADGES", "ROZETLER"), panel, 235, 552, 450, 112,
            () => Show(true), art != null ? art.Trophy : null);
    }

    private void Populate()
    {
        if (statsPage != null) { statsPage.gameObject.SetActive(false); Destroy(statsPage.gameObject); }
        if (achievementsPage != null) { achievementsPage.gameObject.SetActive(false); Destroy(achievementsPage.gameObject); }
        statsPage = Rect("Statistics Page", panel, 0, 0, 0, 0);
        achievementsPage = Rect("Achievements Page", panel, 0, 0, 0, 0);
        var stats = PlayerProgressSave.Read();
        int best = SaveManager.LoadHighScore();
        PopulateStatistics(stats, best);
        PopulateBadges(Achievements(stats, best));
    }

    private void PopulateStatistics(PlayerProgressSave.Stats stats, int best)
    {
        Label(statsPage, "LIFETIME STATISTICS", 0, 420, 890, 58, 34, Muted);
        string[] names = { "HIGH SCORE", "RUNS PLAYED", "PIECES PLACED", "MATCHING MOVES", "RINGS CLEARED",
            "TOTAL POINTS", "BEST COMBO", "ALL CLEARS", "MULTI-MATCH MOVES", "MATCH RATE",
            "BOMBS USED", "PIECE CHANGES" };
        long[] values = { best, stats.Runs, stats.Pieces, stats.MatchingMoves, stats.Rings, stats.Points, stats.BestCombo,
            stats.AllClears, stats.MultiMatches, 0, stats.Bombs, stats.Changes };
        for (int i = 0; i < names.Length; i++)
        {
            var card = Box(names[i], statsPage, i % 2 == 0 ? -235 : 235, 292 - i / 2 * 178, 450, 156, Surface, Border);
            Label(card, names[i], 0, 37, 400, 46, 31, Color.white);
            string value = names[i] == "MATCH RATE" ? (stats.Pieces > 0 ? (stats.MatchingMoves / (double)stats.Pieces).ToString("P0", GameLanguage.Culture) : "0%") :
                (names[i] == "BEST COMBO" && values[i] > 0 ? "x" : "") + values[i].ToString("N0", GameLanguage.Culture);
            Label(card, value, 0, -26, 400, 66, 51, i == 0 ? Gold : Cyan);
        }
    }

    private void PopulateBadges(Achievement[] entries)
    {
        int unlockedCount = 0;
        foreach (var item in entries)
            if (item.Value >= item.Goal) unlockedCount++;

        var trophy = art != null ? art.Trophy : null;
        var summary = Box("Badges Earned Summary", achievementsPage, 0, 397, 920, 154, new Color32(55, 39, 65, 255), Gold, 28);
        AddIcon("Trophy", summary, trophy, -344, 0, 112);
        float summaryWidth = trophy != null ? 636 : 836;
        float summaryX = trophy != null ? 78 : 0;
        Label(summary, GameLanguage.Format("BADGES EARNED · {0} / {1}", "KAZANILAN ROZET · {0} / {1}", unlockedCount, entries.Length),
            summaryX, 29, summaryWidth, 55, 35, Gold);
        ProgressBar(summary, summaryX, -31, summaryWidth, (float)unlockedCount / entries.Length, Gold);

        const float viewportTop = 298;
        const float viewportBottom = -733;
        const float cardHeight = 260;
        const float cardGap = 16;
        float viewportHeight = viewportTop - viewportBottom;
        var viewport = Rect("Badges Scroll", achievementsPage, 0, (viewportTop + viewportBottom) * 0.5f, 932, viewportHeight);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 64;
        scroll.viewport = viewport;
        var content = Rect("Badge Cards", viewport, 0, 0, 0, Mathf.Max(viewportHeight, entries.Length * (cardHeight + cardGap) - cardGap));
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        scroll.content = content;
        for (int i = 0; i < entries.Length; i++)
        {
            var item = entries[i];
            bool unlocked = item.Value >= item.Goal;
            float y = -cardHeight * 0.5f - i * (cardHeight + cardGap);
            var card = Box(item.Title, content, 0, y, 920, cardHeight, Surface, unlocked ? Gold : Border, 28);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0, y);
            var icon = art != null && art.Badges != null && i < art.Badges.Length ? art.Badges[i] : trophy;
            if (icon != null)
            {
                var tile = Box("Badge Artwork", card, -321, 0, 214, 208, new Color32(49, 35, 87, 255), Border, 23);
                AddIcon("Badge Icon", tile, icon, 0, 0, 158);
            }
            float left = icon != null ? -188 : -426;
            float width = 426 - left;
            Label(card, item.Title, left + width * 0.5f, 79, width, 50, 42, Color.white, TextAlignmentOptions.MidlineLeft);
            Label(card, item.GoalText, left + width * 0.5f, 19, width, 68, 34, Muted, TextAlignmentOptions.MidlineLeft)
                .enableWordWrapping = true;
            float barWidth = width - 176;
            ProgressBar(card, left + barWidth * 0.5f, -28, barWidth, (float)System.Math.Min(item.Value, item.Goal) / item.Goal, unlocked ? Mint : Cyan);
            string count = System.Math.Min(item.Value, item.Goal).ToString("N0", GameLanguage.Culture) + " / " + item.Goal.ToString("N0", GameLanguage.Culture);
            Label(card, count, 347, -28, 158, 41, 28, unlocked ? Mint : Cyan, TextAlignmentOptions.MidlineRight);
            var status = Box("Badge Status", card, left + 135, -84, 270, 46,
                unlocked ? new Color32(30, 66, 71, 255) : new Color32(38, 45, 79, 255), unlocked ? Mint : Border, 15);
            Label(status, unlocked ? "UNLOCKED" : "IN PROGRESS", 0, 0, 246, 40, 27, unlocked ? Mint : Muted);
        }
    }

    private static Achievement[] Achievements(PlayerProgressSave.Stats stats, int best) => new[]
    {
        new Achievement("FIRST MOVE", "Place a piece", 1, stats.Pieces),
        new Achievement("GETTING STARTED", "Play 10 runs", 10, stats.Runs),
        new Achievement("RING COLLECTOR", "Clear 100 rings in matches", 100, stats.Rings),
        new Achievement("CLEAN SWEEP", "Clear the entire board in a match", 1, stats.AllClears),
        new Achievement("DOUBLE TAKE", "Trigger 5 multi-match moves", 5, stats.MultiMatches),
        new Achievement("COMBO MASTER", "Reach a x5 combo", 5, stats.BestCombo),
        new Achievement("HIGH FLYER", "Reach a score of 2,000", 2000, best),
        new Achievement("RING VETERAN", "Place 250 pieces", 250, stats.Pieces)
    };

    private void Show(bool achievements)
    {
        statsPage.gameObject.SetActive(!achievements);
        achievementsPage.gameObject.SetActive(achievements);
        title.text = achievements ? GameLanguage.Choose("BADGES", "ROZETLER") : GameLanguage.Text("STATISTICS");
        SetTabSelected(statsTab, !achievements);
        SetTabSelected(achievementsTab, achievements);
    }

    private static void SetTabSelected(Button tab, bool selected)
    {
        tab.interactable = !selected;
        ((RoundedPanelGraphic)tab.targetGraphic).SetStyle(selected ? new Color32(81, 43, 155, 255) : Surface,
            selected ? Cyan : Violet, 25);
    }

    private void Close()
    {
        AudioManager.Instance?.PlayButtonClickForced();
        gameObject.SetActive(false);
    }

    private Button MakeButton(string caption, Transform parent, float x, float y, float width, float height,
        UnityEngine.Events.UnityAction action, Sprite icon = null)
    {
        var rect = Box(caption, parent, x, y, width, height, new Color32(56, 34, 100, 255), Violet);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<RoundedPanelGraphic>();
        button.targetGraphic.raycastTarget = true;
        var colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(action);
        rect.gameObject.AddComponent<MenuButtonFeedback>();
        AddIcon("Tab Icon", rect, icon, -153, 0, 66);
        Label(rect, caption, icon != null ? 38 : 0, 0, width - (icon != null ? 118 : 24), height - 12, 36, Color.white);
        return button;
    }

    private static void AddIcon(string name, Transform parent, Sprite sprite, float x, float y, float size)
    {
        if (sprite == null) return;
        var icon = Rect(name, parent, x, y, size, size).gameObject.AddComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private static void ProgressBar(Transform parent, float x, float y, float width, float progress, Color color)
    {
        var track = Box("Progress Track", parent, x, y, width, 22, Ink, Border, 11);
        float fillWidth = (width - 8f) * Mathf.Clamp01(progress);
        if (fillWidth <= 0f) return;
        var fill = Rect("Progress Fill", track, (fillWidth - width) * 0.5f + 4f, 0, fillWidth, 14)
            .gameObject.AddComponent<Image>();
        fill.color = color;
        fill.raycastTarget = false;
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
        Color color, Color border, float radius = 25)
    {
        var rect = Rect(name, parent, x, y, width, height);
        var graphic = rect.gameObject.AddComponent<RoundedPanelGraphic>();
        graphic.SetStyle(color, border, radius);
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
