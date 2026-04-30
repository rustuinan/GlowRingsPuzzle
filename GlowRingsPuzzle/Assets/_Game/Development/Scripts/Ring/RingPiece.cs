using System.Collections.Generic;
using UnityEngine;

public class RingPiece : MonoBehaviour
{
    [Header("Ring References")]
    [SerializeField] private Ring outerRing;
    [SerializeField] private Ring middleRing;
    [SerializeField] private Ring innerRing;

    private readonly List<Ring> activeRings = new List<Ring>();

    private RingPieceData currentPieceData;
    private Vector3 startPosition;
    private bool placed;

    public IReadOnlyList<Ring> Rings
    {
        get { return activeRings; }
    }

    public bool IsPlaced
    {
        get { return placed; }
    }

    public RingPieceData CurrentPieceData
    {
        get { return currentPieceData; }
    }

    public bool Initialize(RingPieceData pieceData, ThemeData themeData)
    {
        if (themeData == null)
        {
            Debug.LogError("RingPiece: ThemeData null.");
            return false;
        }

        RingColorData colorData = themeData.GetRandomColor();

        if (colorData == null)
        {
            Debug.LogError("RingPiece: ThemeData içinde uygun renk bulunamadı.");
            return false;
        }

        return Initialize(pieceData, themeData, colorData.ColorType);
    }

    public bool Initialize(RingPieceData pieceData, ThemeData themeData, RingColorType forcedColor)
    {
        activeRings.Clear();
        placed = false;
        currentPieceData = pieceData;

        if (pieceData == null || !pieceData.HasAnyLayer)
        {
            Debug.LogError("RingPiece: Geçersiz RingPieceData.");
            DisableAllRings();
            return false;
        }

        if (themeData == null || !themeData.IsValid())
        {
            Debug.LogError("RingPiece: Geçersiz ThemeData.");
            DisableAllRings();
            return false;
        }

        SetupRing(outerRing, RingLayer.Outer, pieceData.HasOuter, forcedColor, themeData);
        SetupRing(middleRing, RingLayer.Middle, pieceData.HasMiddle, forcedColor, themeData);
        SetupRing(innerRing, RingLayer.Inner, pieceData.HasInner, forcedColor, themeData);

        bool hasActiveRing = activeRings.Count > 0;

        if (!hasActiveRing)
        {
            Debug.LogError("RingPiece: Aktif ring oluşmadı.");
        }

        return hasActiveRing;
    }

    public bool Initialize(RingPieceData pieceData, RingColorType forcedColor)
    {
        if (ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null)
        {
            Debug.LogError("RingPiece: ThemeManager veya CurrentTheme bulunamadı.");
            return false;
        }

        return Initialize(pieceData, ThemeManager.Instance.CurrentTheme, forcedColor);
    }

    public void SetStartPosition(Vector3 position)
    {
        startPosition = position;
        transform.position = position;
    }

    public void ReturnToStart()
    {
        transform.position = startPosition;
    }

    public void MarkPlaced()
    {
        placed = true;
    }

    public bool HasLayer(RingLayer layer)
    {
        for (int i = 0; i < activeRings.Count; i++)
        {
            Ring ring = activeRings[i];

            if (ring == null)
            {
                continue;
            }

            if (ring.Layer == layer)
            {
                return true;
            }
        }

        return false;
    }

    public Ring GetRing(RingLayer layer)
    {
        if (layer == RingLayer.Outer)
        {
            return outerRing != null && outerRing.gameObject.activeSelf ? outerRing : null;
        }

        if (layer == RingLayer.Middle)
        {
            return middleRing != null && middleRing.gameObject.activeSelf ? middleRing : null;
        }

        if (layer == RingLayer.Inner)
        {
            return innerRing != null && innerRing.gameObject.activeSelf ? innerRing : null;
        }

        return null;
    }

    private void SetupRing(Ring ring, RingLayer layer, bool isActive, RingColorType colorType, ThemeData themeData)
    {
        if (ring == null)
        {
            if (isActive)
            {
                Debug.LogError("RingPiece: Piece prefab üzerinde " + layer + " Ring referansı eksik.");
            }

            return;
        }

        ring.gameObject.SetActive(isActive);

        if (!isActive)
        {
            return;
        }

        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = Vector3.one;

        ring.Initialize(layer, colorType, themeData);

        if (!activeRings.Contains(ring))
        {
            activeRings.Add(ring);
        }
    }

    private void DisableAllRings()
    {
        activeRings.Clear();

        if (outerRing != null)
        {
            outerRing.gameObject.SetActive(false);
        }

        if (middleRing != null)
        {
            middleRing.gameObject.SetActive(false);
        }

        if (innerRing != null)
        {
            innerRing.gameObject.SetActive(false);
        }
    }
}