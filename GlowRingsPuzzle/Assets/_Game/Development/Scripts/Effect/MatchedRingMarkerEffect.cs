using System.Collections;
using UnityEngine;

public class MatchedRingMarkerEffect : MonoBehaviour
{
    [SerializeField] private LineRenderer glowRing;
    [SerializeField] private LineRenderer coreRing;

    private MatchEffectSettings settings;
    private Ring targetRing;
    private Coroutine playRoutine;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public void Initialize(MatchEffectSettings effectSettings, Ring ring)
    {
        settings = effectSettings;
        targetRing = ring;

        if (glowRing == null || coreRing == null || targetRing == null || settings == null)
        {
            Destroy(gameObject);
            return;
        }

        SetupLine(glowRing);
        SetupLine(coreRing);

        if (settings.additiveLineMaterial != null)
        {
            glowRing.sharedMaterial = settings.additiveLineMaterial;
            coreRing.sharedMaterial = settings.additiveLineMaterial;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private void SetupLine(LineRenderer line)
    {
        line.useWorldSpace = true;
        line.loop = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 12;
        line.numCornerVertices = 12;
        line.positionCount = Mathf.Max(8, settings.ringMarkerSegments);
    }

    private IEnumerator PlayRoutine()
    {
        float duration = settings.GetSafeDuration();
        float elapsed = 0f;

        Color ringColor = GetRingColor(targetRing);
        Color glowBaseColor = BoostColor(Color.Lerp(ringColor, Color.white, 0.18f), 1.08f);
        Color coreBaseColor = BoostColor(Color.Lerp(ringColor, Color.white, 0.60f), 1.18f);

        while (elapsed < duration)
        {
            if (targetRing == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float t = elapsed / duration;
            float pulse = Mathf.Sin(t * Mathf.PI);
            float fade = 1f - Mathf.SmoothStep(0.78f, 1f, t);
            float alphaPulse = Mathf.Clamp01((0.32f + pulse * 0.55f) * fade);

            float radiusMultiplier = Mathf.Lerp(settings.ringMarkerStartRadiusMultiplier, settings.ringMarkerPeakRadiusMultiplier, pulse);
            float glowWidth = Mathf.Lerp(settings.ringMarkerStartWidth, settings.ringMarkerPeakWidth, pulse);
            float coreWidth = glowWidth * 0.42f;

            float radius = GetRingRadius(targetRing) * radiusMultiplier;
            Vector3 center = GetRingCenter(targetRing);

            Color glowColor = glowBaseColor;
            glowColor.a = 0.55f * alphaPulse;

            Color coreColor = coreBaseColor;
            coreColor.a = 0.82f * alphaPulse;

            UpdateCircle(glowRing, center, radius, glowWidth, glowColor);
            UpdateCircle(coreRing, center, radius * 0.985f, coreWidth, coreColor);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private Color BoostColor(Color color, float multiplier)
    {
        return new Color(
            color.r * multiplier,
            color.g * multiplier,
            color.b * multiplier,
            color.a
        );
    }

    private Color GetRingColor(Ring ring)
    {
        Renderer renderer = ring.GetComponentInChildren<Renderer>();

        if (renderer == null || renderer.sharedMaterial == null)
        {
            return settings.ringMarkerColor;
        }

        Material material = renderer.sharedMaterial;

        if (material.HasProperty(BaseColorId))
        {
            return material.GetColor(BaseColorId);
        }

        if (material.HasProperty(ColorId))
        {
            return material.GetColor(ColorId);
        }

        return settings.ringMarkerColor;
    }

    private Vector3 GetRingCenter(Ring ring)
    {
        Vector3 center = ring.transform.position;
        center += Vector3.up * settings.ringMarkerVerticalOffset;

        Camera cam = Camera.main;
        if (cam != null)
        {
            center += -cam.transform.forward * 0.012f;
        }

        return center;
    }

    private float GetRingRadius(Ring ring)
    {
        Renderer renderer = ring.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return Mathf.Max(0.06f, radius);
        }

        return 0.18f;
    }

    private void UpdateCircle(LineRenderer line, Vector3 center, float radius, float width, Color color)
    {
        if (line == null)
        {
            return;
        }

        int segments = Mathf.Max(8, settings.ringMarkerSegments);
        line.positionCount = segments;
        line.startWidth = width;
        line.endWidth = width;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(Color.Lerp(color, Color.white, 0.35f), 0.5f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(color.a * 0.75f, 0f),
                new GradientAlphaKey(color.a, 0.5f),
                new GradientAlphaKey(color.a * 0.75f, 1f)
            }
        );

        line.colorGradient = gradient;

        for (int i = 0; i < segments; i++)
        {
            float normalized = (float)i / segments;
            float angle = normalized * Mathf.PI * 2f;

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            line.SetPosition(i, center + point);
        }
    }
}