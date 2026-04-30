using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchEffectManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MatchEffectSettings settings;
    [SerializeField] private MatchLineEffect lineEffectPrefab;
    [SerializeField] private MatchedRingMarkerEffect ringMarkerEffectPrefab;
    [SerializeField] private Transform effectRoot;

    [Header("Detection")]
    [SerializeField] private float sameCellDistanceThreshold = 0.12f;

    public float PreviewDuration
    {
        get
        {
            if (settings == null)
            {
                return 0f;
            }

            return settings.GetSafeDuration();
        }
    }

    private void Awake()
    {
        if (effectRoot == null)
        {
            effectRoot = transform;
        }
    }

    public IEnumerator PlayMatches(List<MatchData> matches)
    {
        if (!CanPlay(matches))
        {
            yield break;
        }

        HashSet<Ring> uniqueMatchedRings = CollectMatchedRings(matches);

        PlayRingFeedback(uniqueMatchedRings);
        SpawnRingMarkers(uniqueMatchedRings);

        float stagger = Mathf.Max(0f, settings.spawnStagger);

        for (int i = 0; i < matches.Count; i++)
        {
            MatchData match = matches[i];
            if (match == null)
            {
                continue;
            }

            if (!IsCellMatch(match))
            {
                SpawnLineEffect(match);
            }

            if (stagger > 0f && i < matches.Count - 1)
            {
                yield return new WaitForSeconds(stagger);
            }
        }

        yield return new WaitForSeconds(settings.GetSafeDuration());
    }

    private bool CanPlay(List<MatchData> matches)
    {
        if (matches == null || matches.Count == 0)
        {
            return false;
        }

        if (settings == null || !settings.IsValid())
        {
            Debug.LogWarning("MatchEffectManager: MatchEffectSettings atanmadı veya geçersiz.");
            return false;
        }

        if (ringMarkerEffectPrefab == null)
        {
            Debug.LogWarning("MatchEffectManager: Ring Marker Effect Prefab atanmadı.");
            return false;
        }

        return true;
    }

    private HashSet<Ring> CollectMatchedRings(List<MatchData> matches)
    {
        HashSet<Ring> uniqueRings = new HashSet<Ring>();

        for (int i = 0; i < matches.Count; i++)
        {
            MatchData match = matches[i];
            if (match == null)
            {
                continue;
            }

            if (match.RingA != null) uniqueRings.Add(match.RingA);
            if (match.RingB != null) uniqueRings.Add(match.RingB);
            if (match.RingC != null) uniqueRings.Add(match.RingC);
        }

        return uniqueRings;
    }

    private void PlayRingFeedback(HashSet<Ring> uniqueRings)
    {
        foreach (Ring ring in uniqueRings)
        {
            if (ring == null)
            {
                continue;
            }

            RingMatchFeedback feedback = ring.GetComponent<RingMatchFeedback>();
            if (feedback == null)
            {
                feedback = ring.gameObject.AddComponent<RingMatchFeedback>();
            }

            feedback.PlayFeedback(settings);
        }
    }

    private void SpawnRingMarkers(HashSet<Ring> uniqueRings)
    {
        foreach (Ring ring in uniqueRings)
        {
            if (ring == null)
            {
                continue;
            }

            MatchedRingMarkerEffect marker = Instantiate(ringMarkerEffectPrefab, effectRoot);
            marker.Initialize(settings, ring);
        }
    }

    private void SpawnLineEffect(MatchData match)
    {
        if (lineEffectPrefab == null)
        {
            return;
        }

        if (match.RingA == null || match.RingB == null || match.RingC == null)
        {
            return;
        }

        Vector3 posA = GetEffectPosition(match.RingA);
        Vector3 posB = GetEffectPosition(match.RingB);
        Vector3 posC = GetEffectPosition(match.RingC);

        MatchLineEffect effect = Instantiate(lineEffectPrefab, effectRoot);
        effect.Initialize(settings, posA, posB, posC);
    }

    private Vector3 GetEffectPosition(Ring ring)
    {
        Vector3 worldPosition = ring.transform.position;
        worldPosition += Vector3.up * settings.verticalOffset;

        Camera cam = Camera.main;
        if (cam != null)
        {
            worldPosition += -cam.transform.forward * settings.towardCameraOffset;
        }

        return worldPosition;
    }

    private bool IsCellMatch(MatchData match)
    {
        if (match == null || match.RingA == null || match.RingB == null || match.RingC == null)
        {
            return false;
        }

        Transform parentA = match.RingA.transform.parent;
        Transform parentB = match.RingB.transform.parent;
        Transform parentC = match.RingC.transform.parent;

        if (parentA != null && parentA == parentB && parentA == parentC)
        {
            return true;
        }

        float ab = Vector3.Distance(match.RingA.transform.position, match.RingB.transform.position);
        float ac = Vector3.Distance(match.RingA.transform.position, match.RingC.transform.position);
        float bc = Vector3.Distance(match.RingB.transform.position, match.RingC.transform.position);

        return ab <= sameCellDistanceThreshold &&
               ac <= sameCellDistanceThreshold &&
               bc <= sameCellDistanceThreshold;
    }
}