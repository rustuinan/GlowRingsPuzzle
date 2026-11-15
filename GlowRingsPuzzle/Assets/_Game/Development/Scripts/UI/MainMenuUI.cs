using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject modal;
    [SerializeField] private TMP_Text modalTitle;
    [SerializeField] private TMP_Text modalBody;
    [SerializeField] private TMP_Text highScore;
    [SerializeField] private GameObject settingsRows;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text soundValue;
    private Toggle musicToggle;
    private Toggle soundToggle;
    private Toggle vibrationToggle;
    private LanguageSettingsUI languageSelector;
    private GameObject puzzleLevels;
    private static bool reopenSettings;
    [SerializeField] private string gameScenePath = "Assets/_Game/Development/Scene/Game.unity";
    private bool loading;
    private CosmeticsStoreUI store;
    private TMP_Text walletValue;
    private DailySpinUI dailySpin;
    private DailyRewardUI dailyReward;
    private DailyTasksUI dailyTasks;
    private PlayerProgressUI playerProgress;
    private TMP_Text rewardAvailability;
    private TMP_Text spinAvailability;
    private float nextSpinRefresh;

    public void Configure(Button play, GameObject dialog, TMP_Text title, TMP_Text body, TMP_Text record,
        GameObject settings, Button close, Button music, Button sound, TMP_Text musicLabel, TMP_Text soundLabel)
    {
        playButton = play; modal = dialog; modalTitle = title; modalBody = body; highScore = record;
        settingsRows = settings; closeButton = close;
        musicButton = music; soundButton = sound; musicValue = musicLabel; soundValue = soundLabel;
    }
    private void OnEnable()
    {
        if (playButton == null || modal == null) return;
        CosmeticsSave.Changed += RefreshWallet;
        CosmeticsSave.Changed += RefreshTheme;
        RefreshWallet();
        RefreshDailySpin();
        RefreshDailyReward();
        playButton.onClick.AddListener(Play); closeButton.onClick.AddListener(ClosePage);
        if (musicToggle == null) musicToggle = CreateAudioToggle(musicButton, musicValue);
        if (soundToggle == null) soundToggle = CreateAudioToggle(soundButton, soundValue);
        musicToggle.onValueChanged.AddListener(SetMusic);
        soundToggle.onValueChanged.AddListener(SetSound);
        highScore.text = SaveManager.LoadHighScore().ToString("N0", GameLanguage.Culture); modal.SetActive(false);
    }
    private void OnDisable()
    {
        if (playButton == null || modal == null) return;
        CosmeticsSave.Changed -= RefreshWallet;
        CosmeticsSave.Changed -= RefreshTheme;
        playButton.onClick.RemoveListener(Play); closeButton.onClick.RemoveListener(ClosePage);
        if (musicToggle != null) musicToggle.onValueChanged.RemoveListener(SetMusic);
        if (soundToggle != null) soundToggle.onValueChanged.RemoveListener(SetSound);
    }
    private void Update()
    {
        if (modal == null) return;
        if (Input.GetKeyDown(KeyCode.Escape) && modal.activeSelf) ClosePage();
        if (settingsRows.activeInHierarchy) RefreshAudio();
        if (Time.unscaledTime >= nextSpinRefresh) { nextSpinRefresh = Time.unscaledTime + 1; RefreshDailySpin(); RefreshDailyReward(); }
    }
    private void Click() { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickForced(); }
    private void Start()
    {
        RefreshTheme(); DailyRewardSave.Recover(); RefreshWallet(); RefreshDailyReward();
        if (reopenSettings)
        {
            reopenSettings = false;
            ShowPage(MenuPage.Settings);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLanguageNavigation() { reopenSettings = false; }

    private void SelectLanguage(string code)
    {
        if (loading) return;
        Click();
        bool changed = GameLanguage.SetPreference(code);
        if (!changed)
        {
            languageSelector.Refresh();
            return;
        }

        loading = true;
        reopenSettings = true;
        SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
    }
    private void RefreshTheme()
    {
        var theme = CosmeticsSave.MenuTheme;
        if (theme != null) ThemeScenePalette.Apply(gameObject.scene, theme);
    }
    private void RefreshWallet()
    {
        if (walletValue == null)
        {
            Transform value = transform.Find("Safe Area/Header/Wallet/Coin Surface/CoinsValue");
            if (value != null) walletValue = value.GetComponent<TMP_Text>();
        }
        if (walletValue != null) walletValue.text = CosmeticsSave.Coins.ToString("N0", GameLanguage.Culture);
    }
    private void RefreshDailySpin()
    {
        if (spinAvailability == null)
        {
            Transform value = transform.Find("Safe Area/Menu Scroll/Viewport/Content/Activities/Daily Spin/Availability");
            if (value != null) spinAvailability = value.GetComponent<TMP_Text>();
        }
        if (spinAvailability == null) return;
        var state = DailySpinSave.Read();
        var remaining = DailySpinSave.Remaining(System.DateTime.UtcNow);
        spinAvailability.text = state.Pending ? GameLanguage.Text("FINISH") : DailySpinSave.CanSpin(System.DateTime.UtcNow) ? GameLanguage.Text("FREE") :
            remaining.TotalHours >= 1 ? ((int)remaining.TotalHours + 1) + "h" : (remaining.Minutes + 1) + "m";
    }
    public void ClosePage()
    {
        if (dailySpin != null && dailySpin.IsSpinning) return;
        Click(); modal.SetActive(false);
        if (store != null) store.gameObject.SetActive(false);
        if (dailyReward != null) dailyReward.gameObject.SetActive(false);
        if (dailySpin != null) dailySpin.gameObject.SetActive(false);
        if (dailyTasks != null) dailyTasks.gameObject.SetActive(false);
        if (playerProgress != null) playerProgress.gameObject.SetActive(false);
    }
    private void RefreshDailyReward()
    {
        if (rewardAvailability == null)
        {
            Transform value = transform.Find("Safe Area/Menu Scroll/Viewport/Content/Activities/Daily Reward/Availability");
            if (value != null) rewardAvailability = value.GetComponent<TMP_Text>();
        }
        if (rewardAvailability != null) rewardAvailability.text = DailyRewardSave.CanClaim(System.DateTime.Now) ? GameLanguage.Text("CLAIM") : GameLanguage.Text("DONE");
    }
    private void SetMusic(bool enabled) { Click(); AudioManager.Instance?.SetMusicMuted(!enabled); RefreshAudio(); }
    private void SetSound(bool enabled) { Click(); AudioManager.Instance?.SetSoundMuted(!enabled); RefreshAudio(); }

    private static Toggle CreateAudioToggle(Button row, TMP_Text value)
    {
        row.enabled = false;
        value.gameObject.SetActive(false);
        ((RectTransform)row.transform.Find("Icon")).anchoredPosition = new Vector2(-274, 0);
        row.transform.Find("Title").GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;
        var track = SwitchGraphic("Track", row.transform, new Vector2(278, 0), new Vector2(144, 66));
        var thumb = SwitchGraphic("Thumb", track.transform, Vector2.zero, new Vector2(48, 48));
        thumb.SetStyle(Color.white, new Color32(218, 219, 255, 255), 24);
        var hitArea = new GameObject(row.name + " Toggle", typeof(RectTransform), typeof(Image));
        hitArea.layer = row.gameObject.layer;
        var hitRect = (RectTransform)hitArea.transform;
        hitRect.SetParent(row.transform, false);
        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.offsetMin = hitRect.offsetMax = Vector2.zero;
        hitArea.GetComponent<Image>().color = Color.clear;
        var toggle = hitArea.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.transition = Selectable.Transition.None;
        return toggle;
    }

    private static RoundedPanelGraphic SwitchGraphic(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var graphic = rect.gameObject.AddComponent<RoundedPanelGraphic>();
        graphic.raycastTarget = false;
        return graphic;
    }

    private void RefreshAudio()
    {
        var audio = AudioManager.Instance;
        RefreshSwitch(musicToggle, audio == null || !audio.MusicMuted);
        RefreshSwitch(soundToggle, audio == null || !audio.SoundMuted);
    }

    private static void RefreshSwitch(Toggle toggle, bool enabled)
    {
        var track = (RoundedPanelGraphic)toggle.targetGraphic;
        var thumb = (RectTransform)track.transform.GetChild(0);
        var position = new Vector2(enabled ? 36 : -36, 0);
        toggle.SetIsOnWithoutNotify(enabled);
        if (thumb.anchoredPosition == position) return;
        track.SetStyle(enabled ? new Color32(71, 65, 169, 255) : new Color32(48, 43, 79, 255),
            enabled ? new Color32(135, 240, 255, 255) : new Color32(107, 101, 147, 255), 33);
        thumb.anchoredPosition = position;
    }
    public void ShowPage(MenuPage page)
    {
        if (loading || (dailySpin != null && dailySpin.IsSpinning)) return;
        if (puzzleLevels != null) puzzleLevels.SetActive(page == MenuPage.PuzzleMode);
        if (playerProgress != null) playerProgress.gameObject.SetActive(false);
        if (dailyTasks != null) dailyTasks.gameObject.SetActive(false);
        if (store != null && page != MenuPage.Shop && page != MenuPage.Wallet) store.gameObject.SetActive(false);
        if (dailyReward != null) dailyReward.gameObject.SetActive(false);
        if (page == MenuPage.DailyTasks)
        {
            Click(); modal.SetActive(false);
            if (dailySpin != null) dailySpin.gameObject.SetActive(false);
            if (dailyTasks == null) dailyTasks = DailyTasksUI.Open(modal.transform.parent, modalTitle.font);
            else dailyTasks.Reopen();
            return;
        }
        if (page == MenuPage.DailyReward)
        {
            Click(); modal.SetActive(false);
            if (store != null) store.gameObject.SetActive(false);
            if (dailySpin != null) dailySpin.gameObject.SetActive(false);
            if (dailyReward == null) dailyReward = DailyRewardUI.Open(modal.transform.parent, modalTitle.font);
            else dailyReward.Reopen();
            return;
        }
        if (page == MenuPage.DailySpin)
        {
            Click(); modal.SetActive(false);
            if (store != null) store.gameObject.SetActive(false);
            if (dailySpin == null) dailySpin = DailySpinUI.Open(modal.transform.parent, modalTitle.font);
            else dailySpin.Reopen();
            return;
        }
        if (dailySpin != null) dailySpin.gameObject.SetActive(false);
        if (page == MenuPage.Statistics)
        {
            Click(); modal.SetActive(false);
            if (playerProgress == null) playerProgress = PlayerProgressUI.Open(modal.transform.parent, modalTitle.font);
            else playerProgress.Reopen();
            return;
        }
        if (page == MenuPage.Shop || page == MenuPage.Wallet)
        {
            Click(); modal.SetActive(false);
            if (store == null) store = CosmeticsStoreUI.Open(modal.transform.parent, modalTitle.font);
            else store.Reopen();
            return;
        }
        Click(); modal.SetActive(true); settingsRows.SetActive(page == MenuPage.Settings);
        modalBody.gameObject.SetActive(page != MenuPage.Settings);
        switch (page)
        {
            case MenuPage.Settings:
                modalTitle.text = GameLanguage.Text("SETTINGS");
                if (vibrationToggle == null)
                {
                    var soundRow = (RectTransform)soundButton.transform;
                    ((RectTransform)musicButton.transform).anchoredPosition += Vector2.up * 52;
                    soundRow.anchoredPosition += Vector2.up * 52;
                    vibrationToggle = VibrationSettingsRow.Create(soundRow, soundButton.transform.Find("Title").GetComponent<TMP_Text>(), -153);
                }
                VibrationSettingsRow.Refresh(vibrationToggle);
                if (languageSelector == null)
                    languageSelector = LanguageSettingsUI.Create(settingsRows.transform, modalTitle.font, SelectLanguage);
                else languageSelector.Refresh();
                RefreshAudio();
                break;
            case MenuPage.PuzzleMode:
                modalTitle.text = GameLanguage.Text("PUZZLE MODE");
                modalBody.gameObject.SetActive(false);
                if (puzzleLevels == null)
                    puzzleLevels = MovePuzzleUI.CreateMenu(modalTitle.transform.parent, modalTitle.font, PlayPuzzle);
                break;
            default:
                string title; string description;
                switch (page)
                {
                    case MenuPage.Shop: title = "SHOP"; description = "Coins, boosters and more ways to glow."; break;
                    default: title = "COINS"; description = "New rewards are on their way."; break;
                }
                modalTitle.text = title; modalBody.text = description; break;
        }
    }
    public void Play() { StartGameMode(-1); }

    private void PlayPuzzle(int level) { StartGameMode(level); }

    private void StartGameMode(int puzzleLevel)
    {
        if (loading || (dailySpin != null && dailySpin.IsSpinning)) return;
        if (!Application.CanStreamedLevelBeLoaded(gameScenePath))
        { Debug.LogError("Main menu: Game scene is missing from Build Settings.", this); return; }
        Click(); loading = true; playButton.interactable = false;
        MovePuzzle.Queue(puzzleLevel);
        SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single);
    }
}
