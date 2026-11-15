using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DailySpinUI : MonoBehaviour
{
    private static readonly Color White = new Color32(244, 243, 255, 255);
    private static readonly Color Muted = new Color32(169, 166, 198, 255);
    private static readonly Color Cyan = new Color32(139, 224, 247, 255);
    private static readonly Color Gold = new Color32(246, 209, 133, 255);
    private static readonly Color Surface = new Color32(31, 26, 60, 255);
    private const float PanelWidth = 1080f;
    private const float ContentTop = 666f;
    private const float WheelCenterY = 15f;
    private DailySpinConfig config;
    private TMP_FontAsset font;
    private FontStyles fontStyle = FontStyles.Bold;
    private RectTransform window;
    private RectTransform content;
    private RectTransform rotor;
    private DailySpinWheelGraphic wheel;
    private DailySpinWheelGraphic wheelFrame;
    private RectTransform pointer;
    private RectTransform balancePanel;
    private RectTransform resultPanel;
    private DailySpinRewardVFX rewardVfx;
    private TMP_Text resultHeading;
    private bool walletAnimating;
    private TMP_Text wallet;
    private TMP_Text result;
    private TMP_Text nextSpin;
    private TMP_Text spinCaption;
    private Button spinButton;
    private Button closeButton;
    private AudioSource sound;
    private float refreshAt;
    public bool IsSpinning { get; private set; }
    public int DisplayedReward { get; private set; }

    public static DailySpinUI Open(Transform parent, TMP_FontAsset font)
    {
        var config = Resources.Load<DailySpinConfig>("DailySpinConfig");
        if (config == null || !config.IsValid()) { Debug.LogError("Daily Spin configuration is missing or invalid."); return null; }
        var root = Rect("Daily Spin Window", parent, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.48f);
        var ui = root.gameObject.AddComponent<DailySpinUI>(); ui.config = config; ui.font = font;
        if (GameLanguage.IsTurkish)
        {
            var turkishFont = Resources.Load<TMP_FontAsset>("DailyReward Turkish SDF");
            if (turkishFont != null)
            {
                ui.font = turkishFont;
                ui.fontStyle = FontStyles.Normal;
            }
        }
        ui.Build(); ui.Present(); return ui;
    }
    private void OnEnable() { CosmeticsSave.Changed += RefreshWallet; }
    private void OnDisable()
    {
        CosmeticsSave.Changed -= RefreshWallet; StopAllCoroutines(); IsSpinning = false; walletAnimating = false;
        if (rewardVfx != null) rewardVfx.StopEffect();
        if (wheelFrame != null) wheelFrame.SetSpinning(false);
        if (sound != null) sound.Stop();
    }
    public void Reopen() { gameObject.SetActive(true); transform.SetAsLastSibling(); Present(); }
    public void Close()
    { if (IsSpinning) return; Click(); gameObject.SetActive(false); }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + 0.5f; if (!IsSpinning) RefreshAvailability(); }
        if (sound != null) sound.mute = AudioManager.Instance != null && AudioManager.Instance.SoundMuted;
        if (pointer != null) pointer.localRotation = Quaternion.Euler(0, 0, IsSpinning ? Mathf.Sin(Time.unscaledTime * 19) * 6 : 0);
    }
    private void Build()
    {
        var frame = Rect("Daily Spin Reference Frame", transform, Vector2.zero, new Vector2(1080, 1920));
        frame.gameObject.AddComponent<ReferenceFrameFit>();
        window = Box("Daily Spin Modal", frame, 0, 0, PanelWidth, 0, new Color32(20, 17, 45, 255), new Color32(131, 111, 181, 255), 48);
        window.pivot = new Vector2(0.5f, 1f);
        window.localScale = Vector3.one * 0.84f;
        window.anchoredPosition = new Vector2(0, ContentTop * window.localScale.y);
        content = Rect("Daily Spin Content", window, Vector2.zero, Vector2.zero);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.anchoredPosition = new Vector2(0, -ContentTop);
        Label(content, "DAILY SPIN", 0, 584, 750, 90, 60, White);
        closeButton = ButtonAt(content, "X", 440, 584, 106, 106, Surface, Close);
        var balance = Box("Coin Balance", content, 0, 488, 410, 88, new Color32(53, 42, 49, 255), Gold, 25);
        balancePanel = balance;
        if (config.CoinIcon != null)
        {
            var coin = Rect("Balance Coin Icon", balance, new Vector2(-140, 0), new Vector2(46, 46));
            var coinImage = coin.gameObject.AddComponent<Image>(); coinImage.sprite = config.CoinIcon; coinImage.raycastTarget = false;
        }
        wallet = Label(balance, "", 28, 0, 300, 66, 32, Gold);
        sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 0; sound.volume = 0.55f;
        var frameArt = Rect("Wheel Frame", content, new Vector2(0, WheelCenterY), new Vector2(800, 800));
        wheelFrame = frameArt.gameObject.AddComponent<DailySpinWheelGraphic>(); wheelFrame.ConfigureFrame();
        pointer = Rect("Winning Pointer", content, new Vector2(0, WheelCenterY + 376), new Vector2(70, 80));
        pointer.gameObject.AddComponent<DailySpinWheelGraphic>().ConfigurePointer();
        var hub = Box("Wheel Hub", content, 0, WheelCenterY, 168, 168, new Color32(38, 23, 76, 255), Gold, 84);
        if (config.CoinIcon != null)
        {
            var icon = Rect("Coin", hub, Vector2.zero, new Vector2(102, 102));
            var graphic = icon.gameObject.AddComponent<Image>(); graphic.sprite = config.CoinIcon; graphic.raycastTarget = false;
        }
        else Label(hub, "COINS", 0, 0, 130, 50, 27, Gold);
        resultPanel = Box("Spin Result", content, 0, -489, 910, 150, Surface, new Color32(76, 67, 118, 255), 30);
        resultHeading = Label(resultPanel, "", 0, 37, 850, 40, 28, Muted);
        result = Label(resultPanel, "", 0, -23, 850, 78, 46, Gold);
        spinButton = ButtonAt(content, "SPIN", 0, -476, 910, 130, new Color32(75, 43, 150, 255), Spin);
        spinCaption = spinButton.GetComponentInChildren<TMP_Text>();
        spinButton.GetComponent<RoundedPanelGraphic>().SetStyle(new Color32(98, 46, 186, 255), Gold, 28);
        nextSpin = Label(content, "", 0, 0, 940, 48, 27, Muted);
        var overlay = Rect("Reward Celebration", content, Vector2.zero, new Vector2(1080, 1920));
        rewardVfx = overlay.gameObject.AddComponent<DailySpinRewardVFX>(); overlay.gameObject.SetActive(false);
    }
    private void BuildWheel(int[] rewards)
    {
        if (rotor != null) { rotor.gameObject.SetActive(false); Destroy(rotor.gameObject); }
        rotor = Rect("Prize Wheel", content, new Vector2(0, WheelCenterY), new Vector2(760, 760));
        // Keep the pointer and hub above the spinning artwork.
        rotor.SetSiblingIndex(wheelFrame.transform.GetSiblingIndex() + 1);
        wheel = rotor.gameObject.AddComponent<DailySpinWheelGraphic>(); wheel.Configure(rewards.Length);
        for (int i = 0; i < rewards.Length; i++)
        {
            float angle = (90 - i * 360f / rewards.Length) * Mathf.Deg2Rad;
            var label = Rect("Prize " + i, rotor, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 247, new Vector2(175, 90));
            label.localRotation = Quaternion.Euler(0, 0, -i * 360f / rewards.Length);
            var amount = Label(label, rewards[i].ToString("N0", GameLanguage.Culture), 0, 0, 171, 78, 51, White);
            amount.outlineColor = new Color32(33, 19, 79, 255); amount.outlineWidth = 0.17f;
        }
    }
    private void Present()
    {
        if (rewardVfx != null) rewardVfx.StopEffect(); walletAnimating = false;
        wheelFrame.SetSpinning(false); wheelFrame.ResetCelebration();
        resultPanel.GetComponent<RoundedPanelGraphic>().SetStyle(Surface, new Color32(76, 67, 118, 255), 30);
        resultHeading.color = Muted;
        var state = DailySpinSave.Read();
        bool ready = DailySpinSave.CanSpin(DateTime.UtcNow);
        int[] rewards = (state.Pending || !ready) && state.Rewards != null && state.Rewards.Length >= 3 ? state.Rewards : config.Rewards;
        BuildWheel(rewards); RefreshWallet(); DisplayedReward = 0;
        if (state.Pending) { StartCoroutine(Animate(state)); return; }
        rotor.localRotation = Quaternion.Euler(0, 0, state.StopAngle);
        bool showLastReward = !ready && state.LastReward > 0;
        resultPanel.gameObject.SetActive(showLastReward);
        if (showLastReward)
        {
            DisplayedReward = state.LastReward; result.text = "+" + state.LastReward.ToString("N0", GameLanguage.Culture) + " " + GameLanguage.Text("COINS");
            resultHeading.text = GameLanguage.Text("LAST REWARD"); SetResultSize(55);
            wheel.Highlight(DailySpinSave.SegmentAtPointer(state.StopAngle, rewards.Length));
        }
        RefreshAvailability();
    }
    public void Spin()
    {
        if (IsSpinning || !DailySpinSave.TryBegin(config, DateTime.UtcNow, out var state)) return;
        rewardVfx.StopEffect(); walletAnimating = false; RefreshWallet();
        wheelFrame.ResetCelebration();
        resultPanel.GetComponent<RoundedPanelGraphic>().SetStyle(Surface, new Color32(76, 67, 118, 255), 30);
        float angle = rotor != null ? rotor.localEulerAngles.z : 0;
        Click(); BuildWheel(state.Rewards); rotor.localRotation = Quaternion.Euler(0, 0, angle);
        StartCoroutine(Animate(state));
    }
    private IEnumerator Animate(DailySpinSave.State state)
    {
        IsSpinning = true; closeButton.interactable = false; spinButton.interactable = false;
        wheelFrame.SetSpinning(true);
        resultPanel.gameObject.SetActive(false);
        spinCaption.text = GameLanguage.Text("SPINNING...");
        nextSpin.text = "";
        FitActionArea();
        if (config.SpinSound != null)
        { sound.clip = config.SpinSound; sound.loop = true; sound.mute = AudioManager.Instance != null && AudioManager.Instance.SoundMuted; sound.Play(); }
        float start = rotor.localEulerAngles.z;
        float distance = 6 * 360 + Mathf.Repeat(state.StopAngle - start, 360);
        float elapsed = 0, duration = Mathf.Max(0.1f, config.SpinDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1 - Mathf.Pow(1 - t, 4);
            rotor.localRotation = Quaternion.Euler(0, 0, start + distance * eased);
            yield return null;
        }
        rotor.localRotation = Quaternion.Euler(0, 0, state.StopAngle);
        int landed = DailySpinSave.SegmentAtPointer(rotor.localEulerAngles.z, state.Rewards.Length);
        wheel.Highlight(landed);
        wheelFrame.SetSpinning(false);
        int before = CosmeticsSave.Coins;
        int amount = DailySpinSave.Complete(); DisplayedReward = amount;
        sound.Stop(); sound.loop = false;
        resultPanel.gameObject.SetActive(true);
        if (amount > 0)
        {
            result.text = "+" + amount.ToString("N0", GameLanguage.Culture) + " " + GameLanguage.Text("COINS");
            resultHeading.text = GameLanguage.Text("YOU WON!"); resultHeading.color = Gold; SetResultSize(64);
            resultPanel.GetComponent<RoundedPanelGraphic>().SetStyle(new Color32(56, 37, 70, 255), Gold, 30);
            wheelFrame.Celebrate();
            int after = CosmeticsSave.Coins;
            walletAnimating = true;
            rewardVfx.Play(config.CoinIcon, rotor, balancePanel, resultPanel, amount * 397 + Time.frameCount,
                progress => { if (wallet != null) wallet.text = Mathf.RoundToInt(Mathf.Lerp(before, after, progress)).ToString("N0", GameLanguage.Culture) + " " + GameLanguage.Text("COINS"); },
                () => { walletAnimating = false; RefreshWallet(); });
            if (config.RewardSound != null && !(AudioManager.Instance != null && AudioManager.Instance.SoundMuted)) sound.PlayOneShot(config.RewardSound);
        }
        else
        {
            resultHeading.text = GameLanguage.Text("REWARD PENDING");
            resultHeading.color = Muted;
            result.text = GameLanguage.Text("Reopen to finish collecting your reward");
            SetResultSize(34);
        }
        IsSpinning = false; closeButton.interactable = true; RefreshWallet(); RefreshAvailability();
    }
    private void RefreshWallet() { if (wallet != null && !walletAnimating) wallet.text = CosmeticsSave.Coins.ToString("N0", GameLanguage.Culture) + " " + GameLanguage.Text("COINS"); }
    private void SetResultSize(float size) { result.fontSize = size; result.fontSizeMin = size * 0.8f; result.fontSizeMax = size; }
    private void RefreshAvailability()
    {
        if (spinButton == null) return;
        bool ready = DailySpinSave.CanSpin(DateTime.UtcNow);
        spinButton.interactable = ready; closeButton.interactable = true;
        spinCaption.text = GameLanguage.Text(ready ? "SPIN" : "SPIN USED");
        var remaining = ready ? TimeSpan.Zero : DailySpinSave.Remaining(DateTime.UtcNow);
        nextSpin.text = remaining > TimeSpan.Zero
            ? GameLanguage.Choose("Next free spin in", "Sonraki ücretsiz çevirme") +
                $"  {(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
            : "";
        FitActionArea();
    }
    private void FitActionArea()
    {
        var actionRect = (RectTransform)spinButton.transform;
        float wheelBottom = WheelCenterY - 400f;
        float visibleBottom = resultPanel.gameObject.activeSelf
            ? resultPanel.anchoredPosition.y - resultPanel.rect.height * 0.5f
            : wheelBottom;
        float actionY = visibleBottom - 26f - actionRect.rect.height * 0.5f;
        actionRect.anchoredPosition = new Vector2(0, actionY);
        float bottom = actionY - actionRect.rect.height * 0.5f;
        bool showTimer = !string.IsNullOrEmpty(nextSpin.text);
        nextSpin.gameObject.SetActive(showTimer);
        if (showTimer)
        {
            var timerRect = nextSpin.rectTransform;
            timerRect.anchoredPosition = new Vector2(0, bottom - 10f - timerRect.rect.height * 0.5f);
            bottom -= 10f + timerRect.rect.height;
        }
        window.sizeDelta = new Vector2(PanelWidth, ContentTop - bottom + 34f);
    }
    private static void Click() { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickForced(); }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color fill, Color border, float radius)
    {
        var rect = Rect(name, parent, new Vector2(x, y), new Vector2(width, height));
        var panel = rect.gameObject.AddComponent<RoundedPanelGraphic>(); panel.SetStyle(fill, border, radius); panel.raycastTarget = false; return rect;
    }
    private TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, float size, Color color)
    {
        var rect = Rect(text, parent, new Vector2(x, y), new Vector2(width, height));
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = GameLanguage.Text(text);
        label.fontSize = size; label.fontStyle = fontStyle; label.alignment = TextAlignmentOptions.Center;
        label.color = color; label.raycastTarget = false; label.enableAutoSizing = true; label.fontSizeMin = size * 0.8f; label.fontSizeMax = size; return label;
    }
    private Button ButtonAt(Transform parent, string text, float x, float y, float width, float height, Color fill, UnityEngine.Events.UnityAction action)
    {
        var rect = Box(text + " Button", parent, x, y, width, height, fill, Cyan, 28);
        var graphic = rect.GetComponent<RoundedPanelGraphic>(); graphic.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic; button.onClick.AddListener(action);
        rect.gameObject.AddComponent<MenuButtonFeedback>(); Label(rect, text, 0, 0, width - 25, height - 10, height > 100 ? 43 : 35, White); return button;
    }
}
