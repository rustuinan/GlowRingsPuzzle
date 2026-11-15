using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameplaySettingsUI : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle soundToggle;
    private Toggle vibrationToggle;
    private bool leaving;

    public bool IsOpen => panel.activeInHierarchy;

    public void Configure(GameManager manager, GameObject dialog, Button open, Toggle music, Toggle sound)
    {
        game = manager;
        panel = dialog;
        openButton = open;
        musicToggle = music;
        soundToggle = sound;
    }

    private void Update()
    {
        openButton.interactable = !leaving && !game.IsGameOver && !game.IsResolving &&
            !game.IsPieceInteracting && !game.IsBoosterOfferOpen;
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open()
    {
        if (!openButton.interactable) return;
        game.CancelBombTargeting();
        if (vibrationToggle == null) AddVibrationRow();
        panel.SetActive(true);
        RefreshAudio();
        VibrationSettingsRow.Refresh(vibrationToggle);
    }

    private void AddVibrationRow()
    {
        var soundRow = (RectTransform)soundToggle.transform;
        var card = (RectTransform)soundRow.parent;
        card.sizeDelta += Vector2.up * 132;
        ((RectTransform)card.Find("Title")).anchoredPosition += Vector2.up * 62;
        ((RectTransform)musicToggle.transform).anchoredPosition += Vector2.up * 62;
        soundRow.anchoredPosition += Vector2.up * 62;
        ((RectTransform)card.Find("Close")).anchoredPosition -= Vector2.up * 62;
        ((RectTransform)card.Find("MainMenu")).anchoredPosition -= Vector2.up * 62;
        vibrationToggle = VibrationSettingsRow.Create(soundRow, soundRow.Find("Label").GetComponent<TMP_Text>(), -77);
    }

    public void Close()
    {
        if (!leaving) panel.SetActive(false);
    }

    public void SetMusic(bool enabled)
    {
        AudioManager.Instance?.SetMusicMuted(!enabled);
        RefreshAudio();
    }

    public void SetSound(bool enabled)
    {
        AudioManager.Instance?.SetSoundMuted(!enabled);
        RefreshAudio();
    }

    public void ReturnToMenu()
    {
        if (leaving) return;
        leaving = true;
        SceneManager.LoadSceneAsync("Assets/_Game/Development/Scene/MainMenu.unity");
    }

    private void RefreshAudio()
    {
        var audio = AudioManager.Instance;
        RefreshToggle(musicToggle, audio != null && !audio.MusicMuted);
        RefreshToggle(soundToggle, audio != null && !audio.SoundMuted);
    }

    private static void RefreshToggle(Toggle toggle, bool enabled)
    {
        toggle.SetIsOnWithoutNotify(enabled);
        var track = (RoundedPanelGraphic)toggle.targetGraphic;
        track.SetStyle(enabled ? new Color32(71, 65, 169, 255) : new Color32(48, 43, 79, 255),
            enabled ? new Color32(135, 240, 255, 255) : new Color32(107, 101, 147, 255), 28);
        var thumb = (RectTransform)track.transform.GetChild(0);
        thumb.anchoredPosition = new Vector2(enabled ? 32 : -32, 0);
    }
}
