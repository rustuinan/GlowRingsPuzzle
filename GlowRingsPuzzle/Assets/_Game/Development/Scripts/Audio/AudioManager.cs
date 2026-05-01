using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MusicMutedKey = "MusicMuted";
    private const string SoundMutedKey = "SoundMuted";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Playlist")]
    [SerializeField] private AudioClip[] backgroundMusicPlaylist;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool shufflePlaylist = true;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.55f;

    [SerializeField] private float musicFadeDuration = 0.35f;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip[] placeClips;
    [SerializeField] private AudioClip matchClip;
    [SerializeField] private AudioClip trashClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip buttonClickClip;

    [Header("SFX Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float placeVolume = 0.65f;

    [Range(0f, 1f)]
    [SerializeField] private float matchVolume = 0.85f;

    [Range(0f, 1f)]
    [SerializeField] private float trashVolume = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float gameOverVolume = 0.90f;

    [Range(0f, 1f)]
    [SerializeField] private float buttonVolume = 0.55f;

    private bool musicMuted;
    private bool soundMuted;
    private bool musicPausedByMute;
    private int currentMusicIndex;
    private Coroutine musicRoutine;
    private Coroutine fadeRoutine;

    public bool MusicMuted
    {
        get { return musicMuted; }
    }

    public bool SoundMuted
    {
        get { return soundMuted; }
    }

    public event System.Action<bool> MusicMuteChanged;
    public event System.Action<bool> SoundMuteChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        LoadSettings();
        FindOrCreateAudioSources();
        ConfigureAudioSources();
        ApplyMuteStates();
    }

    private void Start()
    {
        if (playMusicOnStart && !musicMuted)
        {
            StartMusicIfNeeded();
        }

        NotifyCurrentStates();
    }

    private void FindOrCreateAudioSources()
    {
        if (musicSource == null)
        {
            Transform musicTransform = transform.Find("MusicSource");

            if (musicTransform != null)
            {
                musicSource = musicTransform.GetComponent<AudioSource>();
            }
        }

        if (sfxSource == null)
        {
            Transform sfxTransform = transform.Find("SFXSource");

            if (sfxTransform != null)
            {
                sfxSource = sfxTransform.GetComponent<AudioSource>();
            }
        }

        if (musicSource == null)
        {
            GameObject musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(transform);
            musicSource = musicObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFXSource");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
        }
    }

    private void ConfigureAudioSources()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicMuted ? 0f : musicVolume;
            musicSource.mute = false;
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
            sfxSource.mute = false;
        }
    }

    private void LoadSettings()
    {
        musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        soundMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(MusicMutedKey, musicMuted ? 1 : 0);
        PlayerPrefs.SetInt(SoundMutedKey, soundMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyMuteStates()
    {
        if (musicSource != null)
        {
            musicSource.mute = false;
            musicSource.volume = musicMuted ? 0f : musicVolume;

            if (musicMuted && musicSource.isPlaying)
            {
                musicSource.Pause();
                musicPausedByMute = true;
            }
        }

        if (sfxSource != null)
        {
            sfxSource.mute = false;
            sfxSource.volume = 1f;
        }
    }

    private void NotifyCurrentStates()
    {
        if (MusicMuteChanged != null)
        {
            MusicMuteChanged.Invoke(musicMuted);
        }

        if (SoundMuteChanged != null)
        {
            SoundMuteChanged.Invoke(soundMuted);
        }
    }

    public void ToggleMusic()
    {
        SetMusicMuted(!musicMuted);
    }

    public void ToggleSound()
    {
        SetSoundMuted(!soundMuted);
    }

    public void SetMusicMuted(bool muted)
    {
        if (musicMuted == muted)
        {
            NotifyCurrentStates();
            return;
        }

        musicMuted = muted;
        SaveSettings();

        if (musicSource == null)
        {
            FindOrCreateAudioSources();
            ConfigureAudioSources();
        }

        if (musicMuted)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
                musicPausedByMute = true;
            }

            FadeMusicTo(0f, musicFadeDuration);
        }
        else
        {
            StartMusicIfNeeded();

            if (musicSource != null && musicPausedByMute)
            {
                musicSource.UnPause();
                musicPausedByMute = false;
            }

            FadeMusicTo(musicVolume, musicFadeDuration);
        }

        if (MusicMuteChanged != null)
        {
            MusicMuteChanged.Invoke(musicMuted);
        }
    }

    public void SetSoundMuted(bool muted)
    {
        if (soundMuted == muted)
        {
            NotifyCurrentStates();
            return;
        }

        soundMuted = muted;
        SaveSettings();

        if (SoundMuteChanged != null)
        {
            SoundMuteChanged.Invoke(soundMuted);
        }
    }

    private void StartMusicIfNeeded()
    {
        if (musicMuted)
        {
            return;
        }

        if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Length == 0)
        {
            Debug.LogWarning("AudioManager: Background Music Playlist boş.");
            return;
        }

        if (musicSource == null)
        {
            FindOrCreateAudioSources();
            ConfigureAudioSources();
        }

        if (musicSource != null && musicSource.clip != null)
        {
            if (musicPausedByMute)
            {
                musicSource.UnPause();
                musicPausedByMute = false;
                return;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
                return;
            }
        }

        if (musicRoutine == null)
        {
            musicRoutine = StartCoroutine(MusicPlaylistRoutine());
        }
    }

    public void PlayPlace()
    {
        PlayRandomSFX(placeClips, placeVolume, false);
    }

    public void PlayMatch()
    {
        PlaySFX(matchClip, matchVolume, false);
    }

    public void PlayTrash()
    {
        PlaySFX(trashClip, trashVolume, false);
    }

    public void PlayGameOver()
    {
        PlaySFX(gameOverClip, gameOverVolume, false);
    }

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickClip, buttonVolume, false);
    }

    public void PlayButtonClickForced()
    {
        PlaySFX(buttonClickClip, buttonVolume, true);
    }

    public void PlaySFX(AudioClip clip, float volume)
    {
        PlaySFX(clip, volume, false);
    }

    private void PlaySFX(AudioClip clip, float volume, bool ignoreMute)
    {
        if (soundMuted && !ignoreMute)
        {
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("AudioManager: SFX clip atanmadı.");
            return;
        }

        if (sfxSource == null)
        {
            FindOrCreateAudioSources();
            ConfigureAudioSources();
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager: SFXSource bulunamadı.");
            return;
        }

        sfxSource.mute = false;
        sfxSource.volume = 1f;
        sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayRandomSFX(AudioClip[] clips, float volume, bool ignoreMute)
    {
        if (soundMuted && !ignoreMute)
        {
            return;
        }

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("AudioManager: Place Clips listesi boş.");
            return;
        }

        AudioClip selectedClip = null;
        int safety = 0;

        while (selectedClip == null && safety < 12)
        {
            int randomIndex = Random.Range(0, clips.Length);
            selectedClip = clips[randomIndex];
            safety++;
        }

        if (selectedClip == null)
        {
            Debug.LogWarning("AudioManager: Place Clips içinde geçerli clip yok.");
            return;
        }

        PlaySFX(selectedClip, volume, ignoreMute);
    }

    private IEnumerator MusicPlaylistRoutine()
    {
        while (true)
        {
            if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Length == 0)
            {
                yield return null;
                continue;
            }

            if (musicMuted)
            {
                yield return null;
                continue;
            }

            AudioClip nextClip = GetNextMusicClip();

            if (nextClip == null)
            {
                yield return null;
                continue;
            }

            musicSource.clip = nextClip;
            musicSource.volume = musicVolume;
            musicSource.mute = false;
            musicSource.Play();
            musicPausedByMute = false;

            while (musicSource != null && musicSource.clip == nextClip)
            {
                if (musicMuted)
                {
                    yield return null;
                    continue;
                }

                if (!musicSource.isPlaying)
                {
                    break;
                }

                yield return null;
            }

            yield return null;
        }
    }

    private AudioClip GetNextMusicClip()
    {
        if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Length == 0)
        {
            return null;
        }

        if (shufflePlaylist)
        {
            if (backgroundMusicPlaylist.Length == 1)
            {
                currentMusicIndex = 0;
                return backgroundMusicPlaylist[0];
            }

            int nextIndex = Random.Range(0, backgroundMusicPlaylist.Length);

            if (nextIndex == currentMusicIndex)
            {
                nextIndex++;

                if (nextIndex >= backgroundMusicPlaylist.Length)
                {
                    nextIndex = 0;
                }
            }

            currentMusicIndex = nextIndex;
            return backgroundMusicPlaylist[currentMusicIndex];
        }

        AudioClip clip = backgroundMusicPlaylist[currentMusicIndex];

        currentMusicIndex++;

        if (currentMusicIndex >= backgroundMusicPlaylist.Length)
        {
            currentMusicIndex = 0;
        }

        return clip;
    }

    private void FadeMusicTo(float targetVolume, float duration)
    {
        if (musicSource == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeMusicRoutine(targetVolume, duration));
    }

    private IEnumerator FadeMusicRoutine(float targetVolume, float duration)
    {
        if (musicSource == null)
        {
            yield break;
        }

        float startVolume = musicSource.volume;
        float timer = 0f;

        if (duration <= 0f)
        {
            musicSource.volume = targetVolume;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    [ContextMenu("Test Place SFX")]
    public void TestPlaceSFX()
    {
        PlayPlace();
    }

    [ContextMenu("Test Match SFX")]
    public void TestMatchSFX()
    {
        PlayMatch();
    }

    [ContextMenu("Test Button SFX")]
    public void TestButtonSFX()
    {
        PlayButtonClickForced();
    }

    [ContextMenu("Reset Audio Settings")]
    public void ResetAudioSettings()
    {
        musicMuted = false;
        soundMuted = false;
        musicPausedByMute = false;

        PlayerPrefs.DeleteKey(MusicMutedKey);
        PlayerPrefs.DeleteKey(SoundMutedKey);
        PlayerPrefs.Save();

        ApplyMuteStates();

        if (playMusicOnStart)
        {
            StartMusicIfNeeded();
        }

        NotifyCurrentStates();
    }
}