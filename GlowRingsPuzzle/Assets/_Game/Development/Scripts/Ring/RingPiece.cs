using System.Collections.Generic;
using UnityEngine;

public class RingPiece : MonoBehaviour
{
    [Header("Ring References")]
    [SerializeField] private Ring outerRing;
    [SerializeField] private Ring middleRing;
    [SerializeField] private Ring innerRing;

    private readonly List<Ring> activeRings = new List<Ring>();
    private Vector3 startPosition;
    private bool placed;

    public IReadOnlyList<Ring> Rings => activeRings;
    public bool IsPlaced => placed;

    public bool Initialize(RingPieceData pieceData, ThemeData themeData)
    {
        if (themeData == null)
            return false;

        RingColorData colorData = themeData.GetRandomColor();
        if (colorData == null)
            return false;

        return Initialize(pieceData, themeData, colorData.ColorType);
    }

    public bool Initialize(RingPieceData pieceData, ThemeData themeData, RingColorType forcedColor)
    {
        activeRings.Clear();
        placed = false;

        if (pieceData == null || !pieceData.HasAnyLayer)
        {
            Debug.LogError("RingPiece: Geçersiz RingPieceData.");
            return false;
        }

        if (themeData == null || !themeData.IsValid())
        {
            Debug.LogError("RingPiece: Geçersiz ThemeData.");
            return false;
        }

        SetupRing(outerRing, RingLayer.Outer, pieceData.HasOuter, forcedColor, themeData);
        SetupRing(middleRing, RingLayer.Middle, pieceData.HasMiddle, forcedColor, themeData);
        SetupRing(innerRing, RingLayer.Inner, pieceData.HasInner, forcedColor, themeData);

        return activeRings.Count > 0;
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

    private void SetupRing(Ring ring, RingLayer layer, bool isActive, RingColorType colorType, ThemeData themeData)
    {
        if (ring == null)
        {
            if (isActive)
                Debug.LogError("Piece prefab üzerinde " + layer + " Ring referansı eksik.");

            return;
        }

        ring.gameObject.SetActive(isActive);

        if (!isActive)
            return;

        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = Vector3.one;

        ring.Initialize(layer, colorType, themeData);
        activeRings.Add(ring);
    }
}