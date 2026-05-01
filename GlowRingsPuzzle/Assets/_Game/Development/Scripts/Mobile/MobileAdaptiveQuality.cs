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

    private float timer;
    private int frames;
    private float stableTimer;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFps;

        int ram = SystemInfo.systemMemorySize;
        int vram = SystemInfo.graphicsMemorySize;

        if (ram <= 3000 || vram <= 1024)
            SetQuality(performantIndex);
        else if (ram <= 5000 || vram <= 2048)
            SetQuality(balancedIndex);
        else
            SetQuality(highFidelityIndex);
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
            float fps = frames / timer;

            if (fps < downgradeFps)
            {
                stableTimer = 0f;
                Downgrade();
            }
            else if (fps > upgradeFps)
            {
                stableTimer += checkInterval;

                if (stableTimer >= 15f)
                {
                    Upgrade();
                    stableTimer = 0f;
                }
            }
            else
            {
                stableTimer = 0f;
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
            Downgrade();
        }
    }

    private void OnBottleneckChanged(PerformanceBottleneckChangeEventArgs args)
    {
        if (args.PerformanceBottleneck == PerformanceBottleneck.CPU ||
            args.PerformanceBottleneck == PerformanceBottleneck.GPU)
        {
            Downgrade();
        }
    }
#endif

    private void SetQuality(int index)
    {
        index = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(index, true);
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
}