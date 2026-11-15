using UnityEngine;

internal static class GameplayHaptics
{
    private const string PreferenceKey = "GlowRings.Vibration";

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(PreferenceKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void Play()
    {
        if (!Enabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            activity?.Call("runOnUiThread", new AndroidJavaRunnable(PlayAndroid));
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void PlayAndroid()
    {
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var window = activity?.Call<AndroidJavaObject>("getWindow"))
        using (var view = window?.Call<AndroidJavaObject>("getDecorView"))
        using (var feedback = new AndroidJavaClass("android.view.HapticFeedbackConstants"))
        {
            view?.Call<bool>("performHapticFeedback", feedback.GetStatic<int>("CONTEXT_CLICK"));
        }
    }
#endif
}
