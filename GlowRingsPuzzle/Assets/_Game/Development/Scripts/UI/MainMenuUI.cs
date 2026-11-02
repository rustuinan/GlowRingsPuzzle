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
    [SerializeField] private string gameScenePath = "Assets/_Game/Development/Scene/Game.unity";
    private bool loading;

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
        playButton.onClick.AddListener(Play); closeButton.onClick.AddListener(ClosePage);
        musicButton.onClick.AddListener(ToggleMusic);
        soundButton.onClick.AddListener(ToggleSound);
        highScore.text = SaveManager.LoadHighScore().ToString("N0"); modal.SetActive(false);
    }
    private void OnDisable()
    {
        if (playButton == null || modal == null) return;
        playButton.onClick.RemoveListener(Play); closeButton.onClick.RemoveListener(ClosePage);
        musicButton.onClick.RemoveListener(ToggleMusic);
        soundButton.onClick.RemoveListener(ToggleSound);
    }
    private void Update()
    {
        if (modal == null) return;
        if (Input.GetKeyDown(KeyCode.Escape) && modal.activeSelf) ClosePage();
        if (settingsRows.activeInHierarchy) RefreshAudio();
    }
    private void Click() { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickForced(); }
    public void ClosePage() { Click(); modal.SetActive(false); }
    private void ToggleMusic() { Click(); if (AudioManager.Instance != null) AudioManager.Instance.ToggleMusic(); RefreshAudio(); }
    private void ToggleSound() { Click(); if (AudioManager.Instance != null) AudioManager.Instance.ToggleSound(); RefreshAudio(); }
    private void RefreshAudio()
    {
        musicValue.text = AudioManager.Instance != null && AudioManager.Instance.MusicMuted ? "OFF" : "ON";
        soundValue.text = AudioManager.Instance != null && AudioManager.Instance.SoundMuted ? "OFF" : "ON";
    }
    public void ShowPage(MenuPage page)
    {
        if (loading) return;
        Click(); modal.SetActive(true); settingsRows.SetActive(page == MenuPage.Settings);
        modalBody.gameObject.SetActive(page != MenuPage.Settings);
        switch (page)
        {
            case MenuPage.Settings: modalTitle.text = "SETTINGS"; RefreshAudio(); break;
            case MenuPage.Statistics:
                modalTitle.text = "YOUR RECORD";
                modalBody.text = "PERSONAL BEST\n<size=80><color=#8CEAFF>" + SaveManager.LoadHighScore().ToString("N0") +
                    "</color></size>\n\nKeep matching. Your next best is waiting."; break;
            case MenuPage.PuzzleMode:
                modalTitle.text = "PUZZLE MODE";
                modalBody.text = "Solve handcrafted ring challenges.\n\n<color=#F3CE8C>COMING SOON</color>"; break;
            default:
                string title; string description;
                switch (page)
                {
                    case MenuPage.Shop: title = "SHOP"; description = "Coins, boosters and more ways to glow."; break;
                    case MenuPage.DailyReward: title = "DAILY REWARD"; description = "Come back for something bright."; break;
                    case MenuPage.DailySpin: title = "DAILY SPIN"; description = "A daily chance to win new rewards."; break;
                    default: title = "COINS"; description = "New rewards are on their way."; break;
                }
                modalTitle.text = title; modalBody.text = description + "\n\n<color=#F3CE8C>COMING SOON</color>"; break;
        }
    }
    public void Play()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(gameScenePath))
        { Debug.LogError("Main menu: Game scene is missing from Build Settings.", this); return; }
        Click(); loading = true; playButton.interactable = false;
        SceneManager.LoadSceneAsync(gameScenePath, LoadSceneMode.Single);
    }
}
