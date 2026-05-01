using UnityEngine;
using UnityEngine.UI;

public enum AudioToggleType
{
    Music,
    Sound
}

public class AudioToggleButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;

    [Header("Toggle Type")]
    [SerializeField] private AudioToggleType toggleType = AudioToggleType.Sound;

    [Header("Sprites")]
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    private void Awake()
    {
        FindMissingReferences();

        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnEnable()
    {
        SubscribeEvents();
        RefreshVisual();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    private void FindMissingReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    private void SubscribeEvents()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.MusicMuteChanged -= OnAudioStateChanged;
        AudioManager.Instance.SoundMuteChanged -= OnAudioStateChanged;

        AudioManager.Instance.MusicMuteChanged += OnAudioStateChanged;
        AudioManager.Instance.SoundMuteChanged += OnAudioStateChanged;
    }

    private void UnsubscribeEvents()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.MusicMuteChanged -= OnAudioStateChanged;
        AudioManager.Instance.SoundMuteChanged -= OnAudioStateChanged;
    }

    private void OnButtonClicked()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlayButtonClickForced();

        if (toggleType == AudioToggleType.Music)
        {
            AudioManager.Instance.ToggleMusic();
        }
        else
        {
            AudioManager.Instance.ToggleSound();
        }

        RefreshVisual();
    }

    private void OnAudioStateChanged(bool muted)
    {
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (AudioManager.Instance == null || targetImage == null)
        {
            return;
        }

        bool muted;

        if (toggleType == AudioToggleType.Music)
        {
            muted = AudioManager.Instance.MusicMuted;
        }
        else
        {
            muted = AudioManager.Instance.SoundMuted;
        }

        if (muted)
        {
            if (offSprite != null)
            {
                targetImage.sprite = offSprite;
            }
        }
        else
        {
            if (onSprite != null)
            {
                targetImage.sprite = onSprite;
            }
        }
    }
}