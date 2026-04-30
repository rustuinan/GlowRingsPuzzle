using DG.Tweening;
using UnityEngine;

public class RingMatchFeedback : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer[] targetRenderers;

    private Vector3 initialScale;
    private Vector3 initialLocalPosition;
    private Sequence activeSequence;
    private MaterialPropertyBlock[] propertyBlocks;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");
    private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        KillTween();
        RestoreDefaults();
    }

    private void Initialize()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        initialScale = visualRoot.localScale;
        initialLocalPosition = visualRoot.localPosition;

        propertyBlocks = new MaterialPropertyBlock[targetRenderers.Length];

        for (int i = 0; i < propertyBlocks.Length; i++)
        {
            propertyBlocks[i] = new MaterialPropertyBlock();
        }
    }

    public void PlayFeedback(MatchEffectSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        if (propertyBlocks == null || propertyBlocks.Length == 0)
        {
            Initialize();
        }

        KillTween();
        RestoreDefaults();

        float duration = Mathf.Max(0.05f, settings.ringFeedbackDuration);
        float riseDuration = duration * 0.38f;
        float holdDuration = duration * 0.18f;
        float settleDuration = duration * 0.44f;

        Vector3 peakScale = initialScale * settings.ringPeakScale;
        Vector3 liftedPosition = initialLocalPosition + Vector3.up * settings.ringLiftAmount;

        activeSequence = DOTween.Sequence();
        activeSequence.SetUpdate(false);

        activeSequence.Append(
            visualRoot.DOScale(peakScale, riseDuration)
                .SetEase(Ease.OutCubic)
        );

        activeSequence.Join(
            visualRoot.DOLocalMove(liftedPosition, riseDuration)
                .SetEase(Ease.OutCubic)
        );

        activeSequence.Join(
            DOTween.To(
                () => 0f,
                value => ApplyGlow(settings, value),
                1f,
                riseDuration
            ).SetEase(Ease.OutCubic)
        );

        activeSequence.AppendInterval(holdDuration);

        activeSequence.Append(
            visualRoot.DOScale(initialScale, settleDuration)
                .SetEase(Ease.InOutSine)
        );

        activeSequence.Join(
            visualRoot.DOLocalMove(initialLocalPosition, settleDuration)
                .SetEase(Ease.InOutSine)
        );

        activeSequence.Join(
            DOTween.To(
                () => 1f,
                value => ApplyGlow(settings, value),
                0f,
                settleDuration
            ).SetEase(Ease.InOutSine)
        );

        activeSequence.OnComplete(() =>
        {
            RestoreDefaults();
            activeSequence = null;
        });
    }

    private void ApplyGlow(MatchEffectSettings settings, float amount)
    {
        amount = Mathf.Clamp01(amount);

        float emissionValue = Mathf.Lerp(1f, settings.ringEmissionPeak, amount);
        float rimValue = Mathf.Lerp(1f, settings.ringRimPeak, amount);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer currentRenderer = targetRenderers[i];

            if (currentRenderer == null)
            {
                continue;
            }

            Material sharedMaterial = currentRenderer.sharedMaterial;

            if (sharedMaterial == null)
            {
                continue;
            }

            MaterialPropertyBlock block = propertyBlocks[i];
            currentRenderer.GetPropertyBlock(block);

            if (sharedMaterial.HasProperty(EmissionIntensityId))
            {
                block.SetFloat(EmissionIntensityId, emissionValue);
            }

            if (sharedMaterial.HasProperty(RimIntensityId))
            {
                block.SetFloat(RimIntensityId, rimValue);
            }

            if (sharedMaterial.HasProperty(BaseColorId))
            {
                Color baseColor = sharedMaterial.GetColor(BaseColorId);
                Color boostedColor = Color.Lerp(baseColor, Color.white, settings.ringWhiteFlashAmount);
                block.SetColor(BaseColorId, Color.Lerp(baseColor, boostedColor, amount));
            }
            else if (sharedMaterial.HasProperty(ColorId))
            {
                Color baseColor = sharedMaterial.GetColor(ColorId);
                Color boostedColor = Color.Lerp(baseColor, Color.white, settings.ringWhiteFlashAmount);
                block.SetColor(ColorId, Color.Lerp(baseColor, boostedColor, amount));
            }

            currentRenderer.SetPropertyBlock(block);
        }
    }

    private void RestoreDefaults()
    {
        if (visualRoot != null)
        {
            visualRoot.localScale = initialScale;
            visualRoot.localPosition = initialLocalPosition;
        }

        if (targetRenderers == null || propertyBlocks == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer currentRenderer = targetRenderers[i];

            if (currentRenderer == null)
            {
                continue;
            }

            Material sharedMaterial = currentRenderer.sharedMaterial;

            if (sharedMaterial == null)
            {
                continue;
            }

            MaterialPropertyBlock block = propertyBlocks[i];
            block.Clear();

            if (sharedMaterial.HasProperty(BaseColorId))
            {
                block.SetColor(BaseColorId, sharedMaterial.GetColor(BaseColorId));
            }

            if (sharedMaterial.HasProperty(ColorId))
            {
                block.SetColor(ColorId, sharedMaterial.GetColor(ColorId));
            }

            if (sharedMaterial.HasProperty(EmissionIntensityId))
            {
                block.SetFloat(EmissionIntensityId, 1f);
            }

            if (sharedMaterial.HasProperty(RimIntensityId))
            {
                block.SetFloat(RimIntensityId, 1f);
            }

            currentRenderer.SetPropertyBlock(block);
        }
    }

    private void KillTween()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (visualRoot != null)
        {
            visualRoot.DOKill();
        }
    }
}