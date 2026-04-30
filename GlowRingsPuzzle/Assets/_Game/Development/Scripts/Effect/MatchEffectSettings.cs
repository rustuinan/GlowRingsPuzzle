using UnityEngine;

[CreateAssetMenu(fileName = "MatchEffectSettings", menuName = "Data/Effect/Match Effect Settings")]
public class MatchEffectSettings : ScriptableObject
{
    [Header("General")]
    [Min(0.05f)]
    public float previewDuration = 0.52f;

    [Min(0f)]
    public float spawnStagger = 0.01f;

    [Header("Placement")]
    [Min(0f)]
    public float verticalOffset = 0.19f;

    [Min(0f)]
    public float towardCameraOffset = 0.07f;

    [Header("Materials")]
    public Material additiveLineMaterial;

    [Header("Beam Shape")]
    [Min(0f)]
    public float beamExtraLength = 0.10f;

    [Header("Beam Colors")]
    [ColorUsage(true, true)]
    public Color beamCoreColor = new Color(2.25f, 2.05f, 1.30f, 1f);

    [ColorUsage(true, true)]
    public Color beamGlowColor = new Color(1.85f, 0.95f, 0.18f, 1f);

    [ColorUsage(true, true)]
    public Color flareColor = new Color(2.35f, 1.55f, 0.45f, 1f);

    [Header("Line Match Beam Widths")]
    [Min(0.001f)]
    public float beamStartWidth = 0.22f;

    [Min(0.001f)]
    public float beamPeakWidth = 0.36f;

    [Min(0.001f)]
    public float beamEndWidth = 0.10f;

    [Header("Line Match Core Widths")]
    [Min(0.001f)]
    public float beamCoreStartWidth = 0.075f;

    [Min(0.001f)]
    public float beamCorePeakWidth = 0.135f;

    [Min(0.001f)]
    public float beamCoreEndWidth = 0.045f;

    [Header("Line Match Glow Widths")]
    [Min(0.001f)]
    public float beamGlowStartWidth = 0.22f;

    [Min(0.001f)]
    public float beamGlowPeakWidth = 0.36f;

    [Min(0.001f)]
    public float beamGlowEndWidth = 0.10f;

    [Header("Line Match Flares")]
    [Min(0.01f)]
    public float endpointFlareStartSize = 0.28f;

    [Min(0.01f)]
    public float endpointFlarePeakSize = 0.48f;

    [Min(0.01f)]
    public float middleFlareStartSize = 0.34f;

    [Min(0.01f)]
    public float middleFlarePeakSize = 0.68f;

    [Min(0.01f)]
    public float sweepFlareSize = 0.32f;

    [Header("Cell Match Mesh Effect")]
    [Min(0.01f)]
    public float cellBeamStartLength = 0.40f;

    [Min(0.01f)]
    public float cellBeamPeakLength = 1.04f;

    [Min(0.001f)]
    public float cellBeamStartWidth = 0.16f;

    [Min(0.001f)]
    public float cellBeamPeakWidth = 0.34f;

    [Min(0.001f)]
    public float cellBeamEndWidth = 0.09f;

    [Min(0.01f)]
    public float cellCenterFlareStartSize = 0.34f;

    [Min(0.01f)]
    public float cellCenterFlarePeakSize = 0.78f;

    [Min(0.01f)]
    public float cellOuterFlareStartSize = 0.62f;

    [Min(0.01f)]
    public float cellOuterFlarePeakSize = 1.08f;

    [Header("Old Cell LineRenderer Compatibility")]
    [ColorUsage(true, true)]
    public Color cellCoreColor = new Color(2.25f, 2.05f, 1.30f, 1f);

    [ColorUsage(true, true)]
    public Color cellGlowColor = new Color(1.85f, 0.95f, 0.18f, 1f);

    [ColorUsage(true, true)]
    public Color cellFlashColor = new Color(2.35f, 1.55f, 0.45f, 1f);

    [Min(0.01f)]
    public float outerRingStartRadius = 0.24f;

    [Min(0.01f)]
    public float outerRingPeakRadius = 0.58f;

    [Min(0.01f)]
    public float innerRingStartRadius = 0.11f;

    [Min(0.01f)]
    public float innerRingPeakRadius = 0.32f;

    [Min(0.001f)]
    public float cellCoreStartWidth = 0.045f;

    [Min(0.001f)]
    public float cellCorePeakWidth = 0.12f;

    [Min(0.001f)]
    public float cellCoreEndWidth = 0.024f;

    [Min(0.001f)]
    public float cellGlowStartWidth = 0.12f;

    [Min(0.001f)]
    public float cellGlowPeakWidth = 0.28f;

    [Min(0.001f)]
    public float cellGlowEndWidth = 0.040f;

    [Min(0.01f)]
    public float centerFlashStartSize = 0.18f;

    [Min(0.01f)]
    public float centerFlashPeakSize = 0.56f;

    [Min(0.01f)]
    public float haloStartSize = 0.32f;

    [Min(0.01f)]
    public float haloPeakSize = 0.96f;

    [Min(8)]
    public int cellSegments = 56;

    [Header("Matched Ring Marker")]
    [ColorUsage(true, true)]
    public Color ringMarkerColor = new Color(1.35f, 0.72f, 1.30f, 1f);

    [ColorUsage(true, true)]
    public Color ringMarkerCoreColor = new Color(1.80f, 1.10f, 1.90f, 1f);

    [Min(0.01f)]
    public float ringMarkerStartRadiusMultiplier = 1.01f;

    [Min(0.01f)]
    public float ringMarkerPeakRadiusMultiplier = 1.09f;

    [Min(0.001f)]
    public float ringMarkerStartWidth = 0.020f;

    [Min(0.001f)]
    public float ringMarkerPeakWidth = 0.040f;

    [Min(8)]
    public int ringMarkerSegments = 48;

    [Min(0f)]
    public float ringMarkerVerticalOffset = 0.032f;

    [Header("Matched Ring Smooth Glow Feedback")]
    [Min(0.05f)]
    public float ringFeedbackDuration = 0.50f;

    [Min(1f)]
    public float ringPeakScale = 1.07f;

    [Min(0f)]
    public float ringLiftAmount = 0.016f;

    [Min(1f)]
    public float ringEmissionPeak = 1.80f;

    [Min(1f)]
    public float ringRimPeak = 1.42f;

    [Range(0f, 1f)]
    public float ringWhiteFlashAmount = 0.10f;

    [Header("Cell Support Aura")]
    [ColorUsage(true, true)]
    public Color cellAuraColor = new Color(1.45f, 0.58f, 1.25f, 1f);

    [Min(0.01f)]
    public float cellAuraStartRadius = 0.28f;

    [Min(0.01f)]
    public float cellAuraPeakRadius = 0.60f;

    [Min(0.001f)]
    public float cellAuraStartWidth = 0.03f;

    [Min(0.001f)]
    public float cellAuraPeakWidth = 0.08f;

    [Min(8)]
    public int cellAuraSegments = 48;

    public float GetSafeDuration()
    {
        return Mathf.Max(0.05f, previewDuration);
    }

    public bool IsValid()
    {
        if (previewDuration <= 0f)
        {
            return false;
        }

        if (cellSegments < 8)
        {
            return false;
        }

        if (ringMarkerSegments < 8)
        {
            return false;
        }

        if (cellAuraSegments < 8)
        {
            return false;
        }

        return true;
    }
}