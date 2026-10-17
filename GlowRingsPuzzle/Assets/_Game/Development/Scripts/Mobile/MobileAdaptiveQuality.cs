using UnityEngine;
using UnityEngine.Rendering.Universal;

#if UNITY_ADAPTIVE_PERFORMANCE
using UnityEngine.AdaptivePerformance;
#endif

public class MobileAdaptiveQuality : MonoBehaviour
{
    [Header("Quality Index")]
    public int performantIndex = 0;
    public int balancedIndex = 1;
    public int highFidelityIndex = 2;

    [Header("FPS")]
    public int targetFps = 60;
    public float checkInterval = 3f;

    [Header("FPS Thresholds")]
    public float downgradeFps = 45f;
    public float upgradeFps = 58f;

    [Header("Quality Stability")]
    [Min(3f)] public float downgradeHoldDuration = 9f;
    [Min(3f)] public float upgradeHoldDuration = 15f;

    private float timer;
    private int frames;
    private float lowFpsTimer;
    private float highFpsTimer;
    private UniversalAdditionalCameraData mainCameraData;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFps;

        CacheMainCameraData();

        int ram = SystemInfo.systemMemorySize;
        int vram = SystemInfo.graphicsMemorySize;

        if (ram <= 3000 || vram <= 1024)
            SetQuality(performantIndex);
        else if (ram <= 5000 || vram <= 2048)
            SetQuality(balancedIndex);
        else
            SetQuality(highFidelityIndex);
    }

    private void Start()
    {
        ApplyCameraAntialiasing(QualitySettings.GetQualityLevel());
    }

    private void OnEnable()
    {
#if UNITY_ADAPTIVE_PERFORMANCE
        if (Holder.Instance != null && Holder.Instance.Active)
        {
            Holder.Instance.ThermalStatus.ThermalEvent += OnThermalEvent;
            Holder.Instance.PerformanceStatus.PerformanceBottleneckChangeEvent += OnBottleneckChanged;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_ADAPTIVE_PERFORMANCE
        if (Holder.Instance != null && Holder.Instance.Active)
        {
            Holder.Instance.ThermalStatus.ThermalEvent -= OnThermalEvent;
            Holder.Instance.PerformanceStatus.PerformanceBottleneckChangeEvent -= OnBottleneckChanged;
        }
#endif
    }

    private void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;

        if (timer >= checkInterval)
        {
            float sampleDuration = timer;
            float fps = frames / sampleDuration;

            if (fps < downgradeFps)
            {
                highFpsTimer = 0f;
                lowFpsTimer += sampleDuration;

                if (lowFpsTimer >= downgradeHoldDuration)
                {
                    Downgrade();
                    lowFpsTimer = 0f;
                }
            }
            else if (fps > upgradeFps)
            {
                lowFpsTimer = 0f;
                highFpsTimer += sampleDuration;

                if (highFpsTimer >= upgradeHoldDuration)
                {
                    Upgrade();
                    highFpsTimer = 0f;
                }
            }
            else
            {
                ResetStabilityTimers();
            }

            frames = 0;
            timer = 0f;
        }
    }

#if UNITY_ADAPTIVE_PERFORMANCE
    private void OnThermalEvent(ThermalMetrics metrics)
    {
        if (metrics.WarningLevel == WarningLevel.ThrottlingImminent ||
            metrics.WarningLevel == WarningLevel.Throttling)
        {
            Application.targetFrameRate = 30;
            ResetStabilityTimers();
            Downgrade();
        }
    }

    private void OnBottleneckChanged(PerformanceBottleneckChangeEventArgs args)
    {
        if (args.PerformanceBottleneck == PerformanceBottleneck.CPU ||
            args.PerformanceBottleneck == PerformanceBottleneck.GPU)
        {
            lowFpsTimer = Mathf.Max(lowFpsTimer, downgradeHoldDuration - checkInterval);
        }
    }
#endif

    private void SetQuality(int index)
    {
        index = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(index, true);
        ApplyCameraAntialiasing(index);
    }

    private void Downgrade()
    {
        int current = QualitySettings.GetQualityLevel();

        if (current > performantIndex)
            SetQuality(current - 1);
    }

    private void Upgrade()
    {
        int current = QualitySettings.GetQualityLevel();

        if (current < highFidelityIndex)
            SetQuality(current + 1);
    }

    private void CacheMainCameraData()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            mainCameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        }
    }

    private void ApplyCameraAntialiasing(int qualityIndex)
    {
        if (mainCameraData == null)
        {
            CacheMainCameraData();
        }

        if (mainCameraData == null)
        {
            return;
        }

        mainCameraData.antialiasing = qualityIndex == performantIndex
            ? AntialiasingMode.FastApproximateAntialiasing
            : AntialiasingMode.None;
    }

    private void ResetStabilityTimers()
    {
        lowFpsTimer = 0f;
        highFpsTimer = 0f;
    }
}
