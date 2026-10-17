using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

/// <summary>
/// Owns the Google Mobile Ads lifecycle and keeps one interstitial and one rewarded ad ready.
/// The object is created before the first scene and survives scene changes.
/// </summary>
[DisallowMultipleComponent]
public sealed class AdMobManager : MonoBehaviour
{
    private const string InterstitialProductionId = "ca-app-pub-1200547097832665/9800211152";
    private const string RewardedProductionId = "ca-app-pub-1200547097832665/7616900942";

    private const string AndroidInterstitialTestId = "ca-app-pub-3940256099942544/1033173712";
    private const string AndroidRewardedTestId = "ca-app-pub-3940256099942544/5224354917";
    private const string IosInterstitialTestId = "ca-app-pub-3940256099942544/4411468910";
    private const string IosRewardedTestId = "ca-app-pub-3940256099942544/1712485313";

    private const float RetryDelaySeconds = 30f;

    public static AdMobManager Instance { get; private set; }

    public event Action<bool> RewardedAvailabilityChanged;

    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private Coroutine interstitialRetryRoutine;
    private Coroutine rewardedRetryRoutine;
    private bool initialized;
    private bool loadingInterstitial;
    private bool loadingRewarded;
    private bool fullScreenAdShowing;
    private Action pendingRewardCallback;
    private Action pendingRewardCancelledCallback;
    private bool pendingRewardEarned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(AdMobManager));
        managerObject.AddComponent<AdMobManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAds();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyInterstitial();
        DestroyRewarded();
    }

    public bool ShowInterstitialAfterLevelStart()
    {
        if (!initialized || fullScreenAdShowing || interstitialAd == null || !interstitialAd.CanShowAd())
        {
            if (initialized)
            {
                LoadInterstitial();
            }

            return false;
        }

        fullScreenAdShowing = true;
        interstitialAd.Show();
        return true;
    }

    public bool ShowRewardedExtraLife(Action onRewardEarned, Action onRewardNotEarned)
    {
        if (onRewardEarned == null || !initialized || fullScreenAdShowing || !IsRewardedReady)
        {
            if (initialized)
            {
                LoadRewarded();
            }

            NotifyRewardedAvailability();
            return false;
        }

        fullScreenAdShowing = true;
        pendingRewardCallback = onRewardEarned;
        pendingRewardCancelledCallback = onRewardNotEarned;
        pendingRewardEarned = false;
        NotifyRewardedAvailability();

        rewardedAd.Show(reward =>
        {
            pendingRewardEarned = true;
        });

        return true;
    }

    private void InitializeAds()
    {
        MobileAds.Initialize(initializationStatus =>
        {
            RunOnUnityThread(() =>
            {
                if (this == null)
                {
                    return;
                }

                if (initializationStatus == null)
                {
                    Debug.LogError("AdMob: SDK başlatılamadı.");
                    ScheduleInterstitialRetry();
                    ScheduleRewardedRetry();
                    return;
                }

                initialized = true;
                LoadInterstitial();
                LoadRewarded();
            });
        });
    }

    private void LoadInterstitial()
    {
        if (!initialized || loadingInterstitial || fullScreenAdShowing ||
            (interstitialAd != null && interstitialAd.CanShowAd()))
        {
            return;
        }

        loadingInterstitial = true;
        DestroyInterstitial();

        InterstitialAd.Load(GetInterstitialAdUnitId(), new AdRequest(), (ad, error) =>
        {
            RunOnUnityThread(() =>
            {
                if (this == null)
                {
                    ad?.Destroy();
                    return;
                }

                loadingInterstitial = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning("AdMob: Geçiş reklamı yüklenemedi: " + error);
                    ScheduleInterstitialRetry();
                    return;
                }

                interstitialAd = ad;
                RegisterInterstitialEvents(ad);
            });
        });
    }

    private void RegisterInterstitialEvents(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () => RunOnUnityThread(() =>
        {
            fullScreenAdShowing = false;
            DestroyInterstitial();
            LoadInterstitial();
        });

        ad.OnAdFullScreenContentFailed += error => RunOnUnityThread(() =>
        {
            Debug.LogWarning("AdMob: Geçiş reklamı açılamadı: " + error);
            fullScreenAdShowing = false;
            DestroyInterstitial();
            ScheduleInterstitialRetry();
        });
    }

    private void LoadRewarded()
    {
        if (!initialized || loadingRewarded || fullScreenAdShowing || IsRewardedReady)
        {
            return;
        }

        loadingRewarded = true;
        DestroyRewarded();
        NotifyRewardedAvailability();

        RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (ad, error) =>
        {
            RunOnUnityThread(() =>
            {
                if (this == null)
                {
                    ad?.Destroy();
                    return;
                }

                loadingRewarded = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning("AdMob: Ödüllü reklam yüklenemedi: " + error);
                    NotifyRewardedAvailability();
                    ScheduleRewardedRetry();
                    return;
                }

                rewardedAd = ad;
                RegisterRewardedEvents(ad);
                NotifyRewardedAvailability();
            });
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () => RunOnUnityThread(() =>
        {
            fullScreenAdShowing = false;

            Action completionCallback = pendingRewardEarned
                ? pendingRewardCallback
                : pendingRewardCancelledCallback;
            pendingRewardCallback = null;
            pendingRewardCancelledCallback = null;
            pendingRewardEarned = false;

            DestroyRewarded();
            completionCallback?.Invoke();
            LoadRewarded();
        });

        ad.OnAdFullScreenContentFailed += error => RunOnUnityThread(() =>
        {
            Debug.LogWarning("AdMob: Ödüllü reklam açılamadı: " + error);
            fullScreenAdShowing = false;
            Action cancelledCallback = pendingRewardCancelledCallback;
            pendingRewardCallback = null;
            pendingRewardCancelledCallback = null;
            pendingRewardEarned = false;
            DestroyRewarded();
            NotifyRewardedAvailability();
            cancelledCallback?.Invoke();
            ScheduleRewardedRetry();
        });
    }

    private void DestroyInterstitial()
    {
        if (interstitialAd == null)
        {
            return;
        }

        interstitialAd.Destroy();
        interstitialAd = null;
    }

    private void DestroyRewarded()
    {
        if (rewardedAd == null)
        {
            return;
        }

        rewardedAd.Destroy();
        rewardedAd = null;
    }

    private void ScheduleInterstitialRetry()
    {
        if (interstitialRetryRoutine == null && isActiveAndEnabled)
        {
            interstitialRetryRoutine = StartCoroutine(RetryInterstitialRoutine());
        }
    }

    private IEnumerator RetryInterstitialRoutine()
    {
        yield return new WaitForSecondsRealtime(RetryDelaySeconds);
        interstitialRetryRoutine = null;
        LoadInterstitial();
    }

    private void ScheduleRewardedRetry()
    {
        if (rewardedRetryRoutine == null && isActiveAndEnabled)
        {
            rewardedRetryRoutine = StartCoroutine(RetryRewardedRoutine());
        }
    }

    private IEnumerator RetryRewardedRoutine()
    {
        yield return new WaitForSecondsRealtime(RetryDelaySeconds);
        rewardedRetryRoutine = null;
        LoadRewarded();
    }

    private void NotifyRewardedAvailability()
    {
        RewardedAvailabilityChanged?.Invoke(IsRewardedReady && !fullScreenAdShowing);
    }

    private static void RunOnUnityThread(Action action)
    {
        if (action != null)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(action);
        }
    }

    private static string GetInterstitialAdUnitId()
    {
        if (Debug.isDebugBuild)
        {
#if UNITY_IOS
            return IosInterstitialTestId;
#else
            return AndroidInterstitialTestId;
#endif
        }

        return InterstitialProductionId;
    }

    private static string GetRewardedAdUnitId()
    {
        if (Debug.isDebugBuild)
        {
#if UNITY_IOS
            return IosRewardedTestId;
#else
            return AndroidRewardedTestId;
#endif
        }

        return RewardedProductionId;
    }
}
