using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DailyRewardUI : MonoBehaviour
{
    private static readonly Color White = new Color32(244, 243, 255, 255);
    private static readonly Color Muted = new Color32(169, 166, 198, 255);
    private static readonly Color Gold = new Color32(246, 209, 133, 255);
    private static readonly Color Cyan = new Color32(139, 224, 247, 255);
    private static readonly Color Mint = new Color32(136, 223, 202, 255);
    private static readonly Color Surface = new Color32(31, 26, 60, 255);
    private static readonly Color Violet = new Color32(156, 123, 226, 255);
    private static readonly Color CardBorder = new Color32(93, 75, 143, 255);
    private DailyRewardConfig config;
    private TMP_FontAsset font;
    private TMP_Text message, caption, footer;
    private RectTransform window, balance;
    private Button claimButton;
    private DailySpinRewardVFX vfx;
    private readonly TMP_Text[] dayLabels = new TMP_Text[7];
    private readonly TMP_Text[] dayAmounts = new TMP_Text[7];
    private readonly RoundedPanelGraphic[] cards = new RoundedPanelGraphic[7];
    private readonly RoundedPanelGraphic[] dayBadges = new RoundedPanelGraphic[7];
    private readonly Image[] statusIcons = new Image[7];
    private readonly RectTransform[] coins = new RectTransform[7];
    private bool showingWin;
    private int activeIndex;
    private float nextRefresh;
    private long shownDate;
    public int DisplayedReward { get; private set; }
    public bool ClaimAvailable => claimButton != null && claimButton.interactable;

    public static DailyRewardUI Open(Transform parent, TMP_FontAsset font)
    {
        var config = Resources.Load<DailyRewardConfig>("DailyRewardConfig");
        if (config == null || !config.IsValid) { Debug.LogError("Daily Reward configuration is missing or invalid."); return null; }
        var root = Rect("Daily Reward Window", parent, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.48f);
        var ui = root.gameObject.AddComponent<DailyRewardUI>(); ui.config = config; ui.font = config.UIFont != null ? config.UIFont : font;
        ui.Build(); ui.Present(); return ui;
    }
    private void OnDisable()
    {
        if (vfx != null) vfx.StopEffect();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        if (DateTime.Today.Ticks != shownDate) Present();
        if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + 1; RefreshTimer(); }
    }
    public void Reopen() { gameObject.SetActive(true); transform.SetAsLastSibling(); Present(); }
    public void Close() { Click(); gameObject.SetActive(false); }
    private void Build()
    {
        var frame = Rect("Daily Reward Reference Frame", transform, Vector2.zero, new Vector2(1080, 1920));
        frame.gameObject.AddComponent<ReferenceFrameFit>();
        window = Box("Daily Reward Modal", frame, 0, 0, 1040, 1320, new Color32(20, 17, 45, 255), new Color32(131, 111, 181, 255), 44);
        window.localScale = Vector3.one * 0.86f;
        BuildRibbonTails();
        var header = Box("Reward Header", window, 0, 550, 856, 112, new Color32(81, 43, 155, 255), Violet, 28);
        Label(header, GameLanguage.Choose("DAILY REWARDS", "GÜNLÜK ÖDÜLLER"), 0, 0, 786, 82, 56, Gold);
        for (int i = 0; i < 7; i++)
        {
            bool weekly = i == 6;
            var card = Box("Reward Day Card " + i, window, weekly ? 0 : (i % 3 - 1) * 320,
                weekly ? -150 : 310 - i / 3 * 230, weekly ? 940 : 300, weekly ? 190 : 210, Surface, CardBorder, 26);
            cards[i] = card.GetComponent<RoundedPanelGraphic>();
            float badgeWidth = weekly ? 170 : 204;
            var badge = Box("Day Badge", card, 0, 62, badgeWidth, 49, Surface, Violet, 18);
            dayBadges[i] = badge.GetComponent<RoundedPanelGraphic>();
            dayLabels[i] = Label(badge, "", 0, 0, badgeWidth - 16, 45, 34, White);
            if (weekly && config.GiftIcon != null)
                SpriteAt(card, "Weekly Gift", config.GiftIcon, new Vector2(-373, 0), new Vector2(100, 100));
            coins[i] = Icon(card, "Day Coin", 0, 7, 52);
            dayAmounts[i] = Label(card, "", 0, -51, weekly ? 260 : 208, 59, weekly ? 54 : 46, Gold);
            statusIcons[i] = SpriteAt(card, "Reward Status", null, new Vector2(weekly ? 421 : 108, weekly ? 0 : -70),
                Vector2.one * (weekly ? 44 : 37));
        }
        message = Label(window, "", 0, -282, 940, 54, 27, Gold);
        claimButton = ButtonAt(window, "COIN AL", 0, -375, 940, 110, new Color32(98, 46, 186, 255), Claim);
        caption = claimButton.GetComponentInChildren<TMP_Text>();
        ButtonAt(window, GameLanguage.Choose("BACK", "GERİ"), 0, -485, 940, 80, Surface, Close);
        footer = Label(window, "", 0, -577, 950, 65, 24, Muted);
        var menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
        balance = menu != null ? menu.transform.Find("Safe Area/Header/Wallet/Coin Surface") as RectTransform : null;
        if (balance == null) balance = header;
        var overlay = Rect("Daily Reward Celebration", window, Vector2.zero, new Vector2(1040, 1320));
        vfx = overlay.gameObject.AddComponent<DailySpinRewardVFX>(); overlay.gameObject.SetActive(false);
    }
    private void BuildRibbonTails()
    {
        if (config.Ribbon == null) return;
        for (int side = -1; side <= 1; side += 2)
        {
            var tail = SpriteAt(window, "Ribbon Tail", config.Ribbon, new Vector2(side * 422, 533), new Vector2(158, 88));
            tail.preserveAspect = false;
            tail.color = new Color32(117, 106, 255, 255);
            tail.rectTransform.localScale = new Vector3(-side, 1, 1);
        }
    }
    private void Present()
    {
        vfx.StopEffect(); showingWin = false; shownDate = DateTime.Today.Ticks;
        DailyRewardSave.Recover(); RefreshContent();
    }
    private void RefreshContent()
    {
        bool ready = DailyRewardSave.CanClaim(DateTime.Now);
        var state = DailyRewardSave.Read();
        int nextDay = DailyRewardSave.NextDay;
        DisplayedReward = ready ? config.CoinsForDay(nextDay) : state.LastReward;
        int activeDay = ready ? nextDay : Math.Max(1, state.ClaimedDays);
        int firstDay = (activeDay - 1) / 7 * 7 + 1;
        activeIndex = activeDay - firstDay;
        claimButton.interactable = ready;
        caption.text = ready ? GameLanguage.Format("CLAIM {0:N0} COINS", "{0:N0} COIN AL", DisplayedReward) : GameLanguage.Choose("REWARD CLAIMED", "BUGÜNKÜ ÖDÜL ALINDI");
        for (int i = 0; i < dayLabels.Length; i++)
        {
            int day = (int)Math.Min(int.MaxValue, (long)firstDay + i);
            bool current = i == activeIndex, collected = day <= state.ClaimedDays;
            bool available = current && ready;
            bool weekly = i == dayLabels.Length - 1;
            dayLabels[i].text = current ? GameLanguage.Choose("TODAY", "BUGÜN") : GameLanguage.Format("DAY {0}", "{0}. GÜN", day);
            dayAmounts[i].text = config.CoinsForDay(day).ToString("N0", GameLanguage.Culture);
            dayLabels[i].color = available ? Cyan : collected ? Mint : White;
            var border = available ? Cyan : collected ? new Color32(68, 127, 132, 255) : weekly ? Gold : CardBorder;
            var fill = available ? new Color32(66, 39, 120, 255) : weekly ? new Color32(57, 33, 98, 255) : Surface;
            cards[i].SetStyle(fill, border, 26);
            dayBadges[i].SetStyle(new Color32(56, 34, 100, 255), available || collected || weekly ? border : Violet, 18);
            dayAmounts[i].color = collected ? Muted : Gold;
            statusIcons[i].sprite = collected ? config.ClaimedIcon : config.LockedIcon;
            statusIcons[i].color = collected ? Mint : Muted;
            statusIcons[i].enabled = !available && statusIcons[i].sprite != null;
        }
        message.text = showingWin ? GameLanguage.Format("+{0:N0} COINS!", "+{0:N0} COIN KAZANDIN!", DisplayedReward) : "";
        RefreshTimer();
    }
    private void RefreshTimer()
    {
        if (footer == null) return;
        if (DailyRewardSave.CanClaim(DateTime.Now)) { footer.text = ""; return; }
        var now = DateTime.Now;
        long nextDate = Math.Max(DateTime.Today.Ticks, DailyRewardSave.Read().LastClaimDate) + TimeSpan.TicksPerDay;
        var remaining = TimeSpan.FromTicks(Math.Max(0, nextDate - now.Ticks));
        footer.text = GameLanguage.Choose("NEXT REWARD IN  ", "YENİ ÖDÜLE  ") + ((int)remaining.TotalHours).ToString("00") + ":" + remaining.Minutes.ToString("00") + ":" + remaining.Seconds.ToString("00");
    }
    public void Claim()
    {
        if (!claimButton.interactable) return;
        claimButton.interactable = false; Click();
        int won = DailyRewardSave.Claim(config, DateTime.Now);
        if (won <= 0) { RefreshContent(); return; }
        showingWin = true; DisplayedReward = won;
        RefreshContent();
        vfx.Play(config.CoinIcon, coins[activeIndex], balance, (RectTransform)cards[activeIndex].transform, won * 397 + Time.frameCount, null, null);
        if (config.RewardSound != null && !(AudioManager.Instance != null && AudioManager.Instance.SoundMuted))
        {
            var audio = GetComponent<AudioSource>();
            if (audio == null) audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.PlayOneShot(config.RewardSound, 0.55f);
        }
    }
    private static void Click() { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickForced(); }
    private RectTransform Icon(Transform parent, string name, float x, float y, float size)
    {
        var graphic = SpriteAt(parent, name, config.CoinIcon, new Vector2(x, y), Vector2.one * size);
        if (config.CoinIcon == null) graphic.color = Gold;
        return graphic.rectTransform;
    }
    private static Image SpriteAt(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color fill, Color border, float radius)
    {
        var rect = Rect(name, parent, new Vector2(x, y), new Vector2(width, height));
        rect.gameObject.AddComponent<RoundedPanelGraphic>().SetStyle(fill, border, radius); return rect;
    }
    private TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, float size, Color color)
    {
        var rect = Rect(text, parent, new Vector2(x, y), new Vector2(width, height));
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text;
        label.fontSize = size; label.fontStyle = FontStyles.Normal; label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.color = color; label.raycastTarget = false; label.enableAutoSizing = true;
        label.fontSizeMin = size * 0.8f; label.fontSizeMax = size; return label;
    }
    private Button ButtonAt(Transform parent, string text, float x, float y, float width, float height, Color fill, UnityEngine.Events.UnityAction action)
    {
        var rect = Box(text + " Button", parent, x, y, width, height, fill, Gold, 28);
        var graphic = rect.GetComponent<RoundedPanelGraphic>(); graphic.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic; button.onClick.AddListener(action);
        rect.gameObject.AddComponent<MenuButtonFeedback>(); Label(rect, text, 0, 0, width - 25, height - 10, height > 100 ? 40 : 35, White); return button;
    }
}
